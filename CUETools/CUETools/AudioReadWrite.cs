using System;
using System.IO;
using FLACDotNet;
using WavPackDotNet;

namespace JDP {
	public interface IAudioSource {
		int Read(byte[] buff, int sampleCount);
		long Length { get; }
		long Position { get; set; }
		long Remaining { get; }
		void Close();
		int BitsPerSample { get; }
		int ChannelCount { get; }
		int SampleRate { get; }
	}

	public interface IAudioDest {
		void Write(byte[] buff, int sampleCount);
		void Close();
		long FinalSampleCount { set; }
	}

	public static class AudioReadWrite {
		public static IAudioSource GetAudioSource(string path) {
			switch (Path.GetExtension(path).ToLower()) {
				case ".wav":
					return new WAVReader(path);
				case ".flac":
					return new FLACReader(path);
				case ".wv":
					return new WavPackReader(path);
				default:
					throw new Exception("Unsupported audio type.");
			}
		}

		public static IAudioDest GetAudioDest(string path, int bitsPerSample, int channelCount, int sampleRate, long finalSampleCount) {
			IAudioDest dest;
			switch (Path.GetExtension(path).ToLower()) {
				case ".wav":
					dest = new WAVWriter(path, bitsPerSample, channelCount, sampleRate); break;
				case ".flac":
					dest = new FLACWriter(path, bitsPerSample, channelCount, sampleRate); break;
				case ".wv":
					dest = new WavPackWriter(path, bitsPerSample, channelCount, sampleRate); break;
				default:
					throw new Exception("Unsupported audio type.");
			}
			dest.FinalSampleCount = finalSampleCount;
			return dest;
		}
	}

	public class WAVReader : IAudioSource {
		FileStream _fs;
		BinaryReader _br;
		long _dataOffset, _dataLen;
		long _samplePos, _sampleLen;
		int _bitsPerSample, _channelCount, _sampleRate, _blockAlign;
		bool _largeFile;

		public WAVReader(string path) {
			_fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			_br = new BinaryReader(_fs);

			ParseHeaders();

			_sampleLen = _dataLen / (long)_blockAlign;
			Position = 0;
		}

		public void Close() {
			_br.Close();

			_br = null;
			_fs = null;
		}

		private void ParseHeaders() {
			const long maxFileSize = 0x7FFFFFFEL;
			const uint fccRIFF = 0x46464952;
			const uint fccWAVE = 0x45564157;
			const uint fccFormat = 0x20746D66;
			const uint fccData = 0x61746164;

			uint lenRIFF;
			long fileEnd;
			bool foundFormat, foundData;

			if (_br.ReadUInt32() != fccRIFF) {
				throw new Exception("Not a valid RIFF file.");
			}

			lenRIFF = _br.ReadUInt32();
			fileEnd = (long)lenRIFF + 8;

			if (_br.ReadUInt32() != fccWAVE) {
				throw new Exception("Not a valid WAVE file.");
			}

			_largeFile = false;
			foundFormat = false;
			foundData = false;

			while (_fs.Position < fileEnd) {
				uint ckID, ckSize, ckSizePadded;
				long ckEnd;

				ckID = _br.ReadUInt32();
				ckSize = _br.ReadUInt32();
				ckSizePadded = (ckSize + 1U) & ~1U;
				ckEnd = _fs.Position + (long)ckSizePadded;

				if (ckID == fccFormat) {
					foundFormat = true;

					if (_br.ReadUInt16() != 1) {
						throw new Exception("WAVE must be PCM format.");
					}
					_channelCount = _br.ReadInt16();
					_sampleRate = _br.ReadInt32();
					_br.ReadInt32();
					_blockAlign = _br.ReadInt16();
					_bitsPerSample = _br.ReadInt16();
				}
				else if (ckID == fccData) {
					foundData = true;

					_dataOffset = _fs.Position;
					if (_fs.Length <= maxFileSize) {
						_dataLen = (long)ckSize;
					}
					else {
						_largeFile = true;
						_dataLen = _fs.Length - _dataOffset;
					}
				}

				if ((foundFormat & foundData) || _largeFile) {
					break;
				}

				_fs.Seek(ckEnd, SeekOrigin.Begin);
			}

			if ((foundFormat & foundData) == false) {
				throw new Exception("Format or data chunk not found.");
			}

			if (_channelCount <= 0) {
				throw new Exception("Channel count is invalid.");
			}
			if (_sampleRate <= 0) {
				throw new Exception("Sample rate is invalid.");
			}
			if (_blockAlign != (_channelCount * ((_bitsPerSample + 7) / 8))) {
				throw new Exception("Block align is invalid.");
			}
			if ((_bitsPerSample <= 0) || (_bitsPerSample > 32)) {
				throw new Exception("Bits per sample is invalid.");
			}
		}

		public long Position {
			get {
				return _samplePos;
			}
			set {
				long seekPos;

				if (value < 0) {
					_samplePos = 0;
				}
				else if (value > _sampleLen) {
					_samplePos = _sampleLen;
				}
				else {
					_samplePos = value;
				}

				seekPos = _dataOffset + (_samplePos * (long)_blockAlign);
				_fs.Seek(seekPos, SeekOrigin.Begin);
			}
		}

		public long Length {
			get {
				return _sampleLen;
			}
		}

		public long Remaining {
			get {
				return _sampleLen - _samplePos;
			}
		}

		public int ChannelCount {
			get {
				return _channelCount;
			}
		}

		public int SampleRate {
			get {
				return _sampleRate;
			}
		}

		public int BitsPerSample {
			get {
				return _bitsPerSample;
			}
		}

		public int BlockAlign {
			get {
				return _blockAlign;
			}
		}

		public int Read(byte[] buff, int sampleCount) {
			int byteCount;

			if (sampleCount < 0) {
				sampleCount = 0;
			}
			else if (sampleCount > Remaining) {
				sampleCount = (int)Remaining;
			}
			byteCount = sampleCount * _blockAlign;

			if (sampleCount != 0) {
				if (_fs.Read(buff, 0, byteCount) != byteCount) {
					throw new Exception("Incomplete file read.");
				}
				_samplePos += sampleCount;
			}

			return sampleCount;
		}
	}

	public class WAVWriter : IAudioDest {
		FileStream _fs;
		BinaryWriter _bw;
		int _bitsPerSample, _channelCount, _sampleRate, _blockAlign;
		long _sampleLen;

		public WAVWriter(string path, int bitsPerSample, int channelCount, int sampleRate) {
			_bitsPerSample = bitsPerSample;
			_channelCount = channelCount;
			_sampleRate = sampleRate;
			_blockAlign = _channelCount * ((_bitsPerSample + 7) / 8);

			_fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
			_bw = new BinaryWriter(_fs);

			WriteHeaders();
		}

		private void WriteHeaders() {
			const uint fccRIFF = 0x46464952;
			const uint fccWAVE = 0x45564157;
			const uint fccFormat = 0x20746D66;
			const uint fccData = 0x61746164;

			_bw.Write(fccRIFF);
			_bw.Write((uint)0);
			_bw.Write(fccWAVE);

			_bw.Write(fccFormat);
			_bw.Write((uint)16);
			_bw.Write((ushort)1);
			_bw.Write((ushort)_channelCount);
			_bw.Write((uint)_sampleRate);
			_bw.Write((uint)(_sampleRate * _blockAlign));
			_bw.Write((ushort)_blockAlign);
			_bw.Write((ushort)_bitsPerSample);

			_bw.Write(fccData);
			_bw.Write((uint)0);
		}

		public void Close() {
			const long maxFileSize = 0x7FFFFFFEL;
			long dataLen, dataLenPadded;

			dataLen = _sampleLen * _blockAlign;

			if ((dataLen & 1) == 1) {
				_bw.Write((byte)0);
			}

			if ((dataLen + 44) > maxFileSize) {
				dataLen = ((maxFileSize - 44) / _blockAlign) * _blockAlign;
			}

			dataLenPadded = ((dataLen & 1) == 1) ? (dataLen + 1) : dataLen;

			_bw.Seek(4, SeekOrigin.Begin);
			_bw.Write((uint)(dataLenPadded + 36));

			_bw.Seek(40, SeekOrigin.Begin);
			_bw.Write((uint)dataLen);

			_bw.Close();

			_bw = null;
			_fs = null;
		}

		public long Position {
			get {
				return _sampleLen;
			}
		}

		public long FinalSampleCount {
			set {
			}
		}

		public void Write(byte[] buff, int sampleCount) {
			if (sampleCount < 0) {
				sampleCount = 0;
			}

			if (sampleCount != 0) {
				_fs.Write(buff, 0, sampleCount * _blockAlign);
				_sampleLen += sampleCount;
			}
		}
	}

	class FLACReader : IAudioSource {
		FLACDotNet.FLACReader _flacReader;
		int[,] _sampleBuffer;
		int _bufferOffset, _bufferLength;

		public FLACReader(string path) {
			_flacReader = new FLACDotNet.FLACReader(path);
			_bufferOffset = 0;
			_bufferLength = 0;
		}

		public void Close() {
			_flacReader.Close();
		}

		public long Length {
			get {
				return _flacReader.Length;
			}
		}

		public long Remaining {
			get {
				return _flacReader.Remaining + SamplesInBuffer;
			}
		}

		public long Position {
			get {
				return _flacReader.Position - SamplesInBuffer;
			}
			set {
				_flacReader.Position = value;
				_bufferOffset = 0;
				_bufferLength = 0;
			}
		}

		private int SamplesInBuffer {
			get {
				return _bufferLength - _bufferOffset;
			}
		}

		public int BitsPerSample {
			get {
				return _flacReader.BitsPerSample;
			}
		}

		public int ChannelCount {
			get {
				return _flacReader.ChannelCount;
			}
		}

		public int SampleRate {
			get {
				return _flacReader.SampleRate;
			}
		}

		private unsafe void FLACSamplesToBytes_16(int[,] inSamples, int inSampleOffset,
			byte[] outSamples, int outByteOffset, int sampleCount, int channelCount)
		{
			int loopCount = sampleCount * channelCount;

			if ((inSamples.GetLength(0) - inSampleOffset < sampleCount) ||
				(outSamples.Length - outByteOffset < loopCount * 2))
			{
				throw new IndexOutOfRangeException();
			}

			fixed (int* pInSamplesFixed = &inSamples[inSampleOffset, 0]) {
				fixed (byte* pOutSamplesFixed = &outSamples[outByteOffset]) {
					int* pInSamples = pInSamplesFixed;
					short* pOutSamples = (short*)pOutSamplesFixed;

					for (int i = 0; i < loopCount; i++) {
						*(pOutSamples++) = (short)*(pInSamples++);
					}
				}
			}
		}

		public int Read(byte[] buff, int sampleCount) {
			if (_flacReader.BitsPerSample != 16) {
				throw new Exception("Reading is only supported for 16 bit sample depth.");
			}
			int chanCount = _flacReader.ChannelCount;
			int samplesNeeded, copyCount, buffOffset;

			buffOffset = 0;
			samplesNeeded = sampleCount;

			while (samplesNeeded != 0) {
				if (SamplesInBuffer == 0) {
					_bufferOffset = 0;
					_bufferLength = _flacReader.Read(out _sampleBuffer);
				}

				copyCount = Math.Min(samplesNeeded, SamplesInBuffer);

				FLACSamplesToBytes_16(_sampleBuffer, _bufferOffset, buff, buffOffset,
					copyCount, chanCount);

				samplesNeeded -= copyCount;
				buffOffset += copyCount * chanCount * 2;
				_bufferOffset += copyCount;
			}

			return sampleCount;
		}
	}

	class FLACWriter : IAudioDest {
		FLACDotNet.FLACWriter _flacWriter;
		int[,] _sampleBuffer;
		int _bitsPerSample;
		int _channelCount;
		int _sampleRate;

		public FLACWriter(string path, int bitsPerSample, int channelCount, int sampleRate) {
			if (bitsPerSample != 16) {
				throw new Exception("Bits per sample must be 16.");
			}
			_bitsPerSample = bitsPerSample;
			_channelCount = channelCount;
			_sampleRate = sampleRate;
			_flacWriter = new FLACDotNet.FLACWriter(path, bitsPerSample, channelCount, sampleRate);
		}

		public long FinalSampleCount {
			get {
				return _flacWriter.FinalSampleCount;
			}
			set {
				_flacWriter.FinalSampleCount = value;
			}
		}

		public int CompressionLevel {
			get {
				return _flacWriter.CompressionLevel;
			}
			set {
				_flacWriter.CompressionLevel = value;
			}
		}

		public bool Verify {
			get {
				return _flacWriter.Verify;
			}
			set {
				_flacWriter.Verify = value;
			}
		}

		public void Close() {
			_flacWriter.Close();
		}

		private unsafe void BytesToFLACSamples_16(byte[] inSamples, int inByteOffset,
			int[,] outSamples, int outSampleOffset, int sampleCount, int channelCount)
		{
			int loopCount = sampleCount * channelCount;

			if ((inSamples.Length - inByteOffset < loopCount * 2) ||
				(outSamples.GetLength(0) - outSampleOffset < sampleCount))
			{
				throw new IndexOutOfRangeException();
			}

			fixed (byte* pInSamplesFixed = &inSamples[inByteOffset]) {
				fixed (int* pOutSamplesFixed = &outSamples[outSampleOffset, 0]) {
					short* pInSamples = (short*)pInSamplesFixed;
					int* pOutSamples = pOutSamplesFixed;

					for (int i = 0; i < loopCount; i++) {
						*(pOutSamples++) = (int)*(pInSamples++);
					}
				}
			}
		}

		public void Write(byte[] buff, int sampleCount) {
			if ((_sampleBuffer == null) || (_sampleBuffer.GetLength(0) < sampleCount)) {
				_sampleBuffer = new int[sampleCount, _channelCount];
			}
			BytesToFLACSamples_16(buff, 0, _sampleBuffer, 0, sampleCount, _channelCount);
			_flacWriter.Write(_sampleBuffer, sampleCount);
		}
	}

	class WavPackReader : IAudioSource {
		WavPackDotNet.WavPackReader _wavPackReader;

		public WavPackReader(string path) {
			_wavPackReader = new WavPackDotNet.WavPackReader(path);
		}

		public void Close() {
			_wavPackReader.Close();
		}

		public long Length {
			get {
				return _wavPackReader.Length;
			}
		}

		public long Remaining {
			get {
				return _wavPackReader.Remaining;
			}
		}

		public long Position {
			get {
				return _wavPackReader.Position;
			}
			set {
				_wavPackReader.Position = (int)value;
			}
		}

		public int BitsPerSample {
			get {
				return _wavPackReader.BitsPerSample;
			}
		}

		public int ChannelCount {
			get {
				return _wavPackReader.ChannelCount;
			}
		}

		public int SampleRate {
			get {
				return _wavPackReader.SampleRate;
			}
		}

		private unsafe void WavPackSamplesToBytes_16(int[,] inSamples, int inSampleOffset,
			byte[] outSamples, int outByteOffset, int sampleCount, int channelCount)
		{
			int loopCount = sampleCount * channelCount;

			if ((inSamples.GetLength(0) - inSampleOffset < sampleCount) ||
				(outSamples.Length - outByteOffset < loopCount * 2))
			{
				throw new IndexOutOfRangeException();
			}

			fixed (int* pInSamplesFixed = &inSamples[inSampleOffset, 0]) {
				fixed (byte* pOutSamplesFixed = &outSamples[outByteOffset]) {
					int* pInSamples = pInSamplesFixed;
					short* pOutSamples = (short*)pOutSamplesFixed;

					for (int i = 0; i < loopCount; i++) {
						*(pOutSamples++) = (short)*(pInSamples++);
					}
				}
			}
		}

		public int Read(byte[] buff, int sampleCount) {
			if (_wavPackReader.BitsPerSample != 16) {
				throw new Exception("Reading is only supported for 16 bit sample depth.");
			}
			int chanCount = _wavPackReader.ChannelCount;
			int[,] sampleBuffer;

			sampleBuffer = new int[sampleCount * 2, chanCount];
			_wavPackReader.Read(sampleBuffer, sampleCount);
			WavPackSamplesToBytes_16(sampleBuffer, 0, buff, 0, sampleCount, chanCount);

			return sampleCount;
		}
	}

	class WavPackWriter : IAudioDest {
		WavPackDotNet.WavPackWriter _wavPackWriter;
		int[,] _sampleBuffer;
		int _bitsPerSample;
		int _channelCount;
		int _sampleRate;

		public WavPackWriter(string path, int bitsPerSample, int channelCount, int sampleRate) {
			if (bitsPerSample != 16) {
				throw new Exception("Bits per sample must be 16.");
			}
			_bitsPerSample = bitsPerSample;
			_channelCount = channelCount;
			_sampleRate = sampleRate;
			_wavPackWriter = new WavPackDotNet.WavPackWriter(path, bitsPerSample, channelCount, sampleRate);
		}

		public long FinalSampleCount {
			get {
				return _wavPackWriter.FinalSampleCount;
			}
			set {
				_wavPackWriter.FinalSampleCount = (int)value;
			}
		}

		public int CompressionMode {
			get {
				return _wavPackWriter.CompressionMode;
			}
			set {
				_wavPackWriter.CompressionMode = value;
			}
		}

		public int ExtraMode {
			get {
				return _wavPackWriter.ExtraMode;
			}
			set {
				_wavPackWriter.ExtraMode = value;
			}
		}

		public void Close() {
			_wavPackWriter.Close();
		}

		private unsafe void BytesToWavPackSamples_16(byte[] inSamples, int inByteOffset,
			int[,] outSamples, int outSampleOffset, int sampleCount, int channelCount)
		{
			int loopCount = sampleCount * channelCount;

			if ((inSamples.Length - inByteOffset < loopCount * 2) ||
				(outSamples.GetLength(0) - outSampleOffset < sampleCount))
			{
				throw new IndexOutOfRangeException();
			}

			fixed (byte* pInSamplesFixed = &inSamples[inByteOffset]) {
				fixed (int* pOutSamplesFixed = &outSamples[outSampleOffset, 0]) {
					short* pInSamples = (short*)pInSamplesFixed;
					int* pOutSamples = pOutSamplesFixed;

					for (int i = 0; i < loopCount; i++) {
						*(pOutSamples++) = (int)*(pInSamples++);
					}
				}
			}
		}

		public void Write(byte[] buff, int sampleCount) {
			if ((_sampleBuffer == null) || (_sampleBuffer.GetLength(0) < sampleCount)) {
				_sampleBuffer = new int[sampleCount, _channelCount];
			}
			BytesToWavPackSamples_16(buff, 0, _sampleBuffer, 0, sampleCount, _channelCount);
			_wavPackWriter.Write(_sampleBuffer, sampleCount);
		}
	}
}