// --------------------------------------------------------------------------------
// Copyright (c) 2004 J.D. Purcell
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// --------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace JDP {
	public class AVIReader {
		private static readonly uint ckIDLIST = AVIHelper.FourCC("LIST");
		private static readonly uint ckIDRIFF = AVIHelper.FourCC("RIFF");

		private FileStream _fs;
		private long _fileOffset;
		private long _fileLength;
		private List<AVIStream> _streamList;
		private int _videoStreamID;
		private long _moviOffset;
		private bool _isOpenDML;
		private bool _foundIndex;
		private long _firstVideoChunkOffset;
		private long _nextChunkOffset;

		public AVIReader(string path) {
			_fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536);
			_fileOffset = 0;
			_fileLength = _fs.Length;
			_streamList = new List<AVIStream>();
			_videoStreamID = -1;
			_moviOffset = -1;
			_firstVideoChunkOffset = -1;
			_isOpenDML = false;
			_foundIndex = false;

			ReadHeaders();
		}

		public void Close() {
			_fs.Close();
		}

		public int StreamCount {
			get {
				return _streamList.Count;
			}
		}

		public AVIStreamType GetStreamType(int streamID) {
			return _streamList[streamID].Type;
		}

		public string GetStreamTwoCC(int streamID) {
			return AVIHelper.StreamTwoCC(_streamList[streamID].FourCC, false);
		}

		public AVISTREAMHEADER GetStreamHeader(int streamID) {
			return _streamList[streamID].Header;
		}

		public BITMAPINFOHEADER GetVideoStreamFormat(int streamID) {
			return _streamList[streamID].VideoFormat;
		}

		public WAVEFORMATEX GetAudioStreamFormat(int streamID) {
			return _streamList[streamID].AudioFormat;
		}

		public byte[] GetStreamFormatExtra(int streamID) {
			return _streamList[streamID].FormatExtra;
		}

		public byte[] GetStreamName(int streamID) {
			return _streamList[streamID].STRNData;
		}

		public int VideoStreamID {
			get {
				return _videoStreamID;
			}
		}

		public double Progress {
			get {
				return (double)_fileOffset / (double)_fileLength;
			}
		}

		public bool ReadChunk(out int streamID, out byte[] buff, out bool isKeyFrame, out int duration) {
			StreamChunkInfo chunkInfo;
			bool foundChunk;

			buff = null;
			isKeyFrame = false;
			duration = 0;

			if (_foundIndex) {
				foundChunk = NextChunkByIndex(out streamID, out chunkInfo);
			}
			else {
				foundChunk = NextChunkByMOVI(out streamID, out chunkInfo);
			}
			if (!foundChunk) {
				return false;
			}

			Seek(chunkInfo.Offset);
			buff = ReadChunkData(chunkInfo.Size);
			if (buff.Length < chunkInfo.Size) {
				return false;
			}

			isKeyFrame = chunkInfo.IsKeyFrame;

			if (_streamList[streamID].Type == AVIStreamType.Audio) {
				uint blockAlign = _streamList[streamID].AudioFormat.nBlockAlign;
				duration = (int)((chunkInfo.Size + (blockAlign - 1)) / blockAlign);
			}
			else {
				duration = 1;
			}

			return true;
		}

		public void SeekToStart() {
			foreach (AVIStream stream in _streamList) {
				stream.ChunkIndex = 0;
			}
			_nextChunkOffset = _moviOffset + 12;
		}

		private void ReadHeaders() {
			uint chunkID, dataSize, listType;
			long chunkOffset, firstRIFFEnd, moviEnd;
			byte[] data;
			bool inMOVI;

			moviEnd = -1;

			chunkOffset = ReadChunkHeader(out chunkID, out dataSize, out listType);
			if ((chunkOffset == -1) || (chunkID != AVIHelper.FourCC("RIFF")) ||
				(listType != AVIHelper.FourCC("AVI ")))
			{
				throw new Exception("File isn't an AVI.");
			}
			firstRIFFEnd = _fileOffset + dataSize;

			while (_fileOffset < firstRIFFEnd) {
				chunkOffset = ReadChunkHeader(out chunkID, out dataSize, out listType);
				if (chunkOffset == -1) {
					break;
				}

				if (chunkID == ckIDLIST) {
					if (listType == AVIHelper.FourCC("movi")) {
						if (_videoStreamID == -1) {
							throw new Exception("Video stream not found.");
						}
						_moviOffset = chunkOffset;
						moviEnd = _fileOffset + dataSize;
					}
					continue;
				}

				inMOVI = (_moviOffset != -1) && (chunkOffset >= _moviOffset) && (chunkOffset < moviEnd);

				if (!inMOVI) {
					data = ReadChunkData(dataSize);
					if (data.Length < dataSize) {
						break;
					}
				}
				else {
					SkipChunkData(dataSize);
					data = null;
				}

				if (chunkID == AVIHelper.FourCC("strh")) {
					AVISTREAMHEADER strH = StructHelper<AVISTREAMHEADER>.FromBytes(data, 0, false);
					AVIStream s;

					if (strH.fccType == AVIHelper.FourCC("vids")) {
						s = new AVIStream(AVIStreamType.Video);
						if (_videoStreamID == -1) {
							_videoStreamID = _streamList.Count;
						}
					}
					else if (strH.fccType == AVIHelper.FourCC("auds")) {
						s = new AVIStream(AVIStreamType.Audio);
					}
					else {
						s = new AVIStream(AVIStreamType.Other);
					}

					s.Header = strH;
					_streamList.Add(s);
				}
				if (chunkID == AVIHelper.FourCC("strf")) {
					AVIStream stream = _streamList[_streamList.Count - 1];
					int fmtSize = 0;
					int fmtExtraSize;

					if (stream.Type == AVIStreamType.Video) {
						stream.VideoFormat = StructHelper<BITMAPINFOHEADER>.FromBytes(data, 0, false);
						fmtSize = StructHelper<BITMAPINFOHEADER>.SizeOf;
					}
					else if (stream.Type == AVIStreamType.Audio) {
						stream.AudioFormat = StructHelper<WAVEFORMATEX>.FromBytes(data, 0, false);
						fmtSize = StructHelper<WAVEFORMATEX>.SizeOf;
					}
					else {
						fmtSize = 0;
					}

					fmtExtraSize = data.Length - fmtSize;
					if (fmtExtraSize > 0) {
						stream.FormatExtra = new byte[fmtExtraSize];
						Buffer.BlockCopy(data, fmtSize, stream.FormatExtra, 0, fmtExtraSize);
					}
				}
				if (chunkID == AVIHelper.FourCC("strn")) {
					_streamList[_streamList.Count - 1].STRNData = data;
				}
				if (inMOVI && (AVIHelper.StreamID(chunkID, false) == _videoStreamID)) {
					_firstVideoChunkOffset = chunkOffset;
					Seek(moviEnd);
				}
				if (chunkID == AVIHelper.FourCC("indx")) {
					_isOpenDML = true;
					_foundIndex = ParseOpenDMLIndex(data);
				}
				if (chunkID == AVIHelper.FourCC("idx1")) {
					if (!_isOpenDML) {
						ParseOldIndex(data);
						_foundIndex = true;
					}
				}
			}
			if (_moviOffset == -1) {
				throw new Exception("\"movi\" list not found.");
			}

			SeekToStart();
		}

		private bool NextChunkByIndex(out int streamID, out StreamChunkInfo chunkInfo) {
			long minOffset = Int64.MaxValue;
			int minStreamID = -1;

			for (int i = 0; i < _streamList.Count; i++) {
				AVIStream stream = _streamList[i];

				if (stream.ChunkIndex < stream.ChunkList.Count) {
					long thisOffset = stream.ChunkList[stream.ChunkIndex].Offset;
					if (thisOffset < minOffset) {
						minOffset = thisOffset;
						minStreamID = i;
					}
				}
			}

			if (minStreamID != -1) {
				streamID = minStreamID;
				chunkInfo = _streamList[streamID].ChunkList[_streamList[streamID].ChunkIndex];
				_streamList[streamID].ChunkIndex++;
				return true;
			}
			else {
				streamID = -1;
				chunkInfo = new StreamChunkInfo();
				return false;
			}
		}

		private bool NextChunkByMOVI(out int streamID, out StreamChunkInfo chunkInfo) {
			uint chunkID, dataSize, listType;

			streamID = -1;
			chunkInfo = new StreamChunkInfo();

			Seek(_nextChunkOffset);

			while (true) {
				chunkInfo.Offset = ReadChunkHeader(out chunkID, out dataSize, out listType);
				if (chunkInfo.Offset == -1) {
					return false;
				}
				chunkInfo.Offset += 8;

				if ((chunkID == ckIDLIST) || (chunkID == ckIDRIFF)) {
					continue;
				}

				streamID = AVIHelper.StreamID(chunkID, false);
				if ((streamID != -1) && (streamID < _streamList.Count)) {
					if (_streamList[streamID].FourCC == 0) {
						_streamList[streamID].FourCC = chunkID;
					}
					chunkInfo.Size = dataSize;
					chunkInfo.IsKeyFrame = _streamList[streamID].IsFirstChunk ||
						(_streamList[streamID].Type != AVIStreamType.Video);
					_streamList[streamID].IsFirstChunk = false;
					_nextChunkOffset = _fileOffset + ((dataSize + 1) & 0xFFFFFFFE);
					return true;
				}

				SkipChunkData(dataSize);
			}
		}

		private long ReadChunkHeader(out uint chunkID, out uint dataSize, out uint listType) {
			long offset = _fileOffset;
			long remaining = _fileLength - _fileOffset;
			byte[] hdr = new byte[8];

			chunkID = 0;
			dataSize = 0;
			listType = 0;

			if (remaining < 8) {
				return -1;
			}
			_fs.Read(hdr, 0, 8);
			_fileOffset += 8;
			chunkID = BitConverterLE.ToUInt32(hdr, 0);
			dataSize = BitConverterLE.ToUInt32(hdr, 4);

			if ((chunkID == ckIDLIST) || (chunkID == ckIDRIFF)) {
				if (remaining < 12) {
					return -1;
				}
				_fs.Read(hdr, 0, 4);
				_fileOffset += 4;
				listType = BitConverterLE.ToUInt32(hdr, 0);
				dataSize -= 4;
			}
			else {
				listType = 0;
			}

			return offset;
		}

		private byte[] ReadChunkData(uint dataSize) {
			long remaining = _fileLength - _fileOffset;
			byte[] data;

			if (remaining < dataSize) {
				dataSize = (uint)remaining;
			}
			data = new byte[dataSize];
			_fs.Read(data, 0, (int)dataSize);
			_fileOffset += dataSize;
			if (((dataSize & 1) != 0) && (remaining > dataSize)) {
				_fs.ReadByte();
				_fileOffset += 1;
			}

			return data;
		}

		private void SkipChunkData(uint dataSize) {
			dataSize = (dataSize + 1) & 0xFFFFFFFE;
			Seek(_fileOffset + (long)dataSize);
		}

		private void Seek(long offset) {
			if (offset > _fileLength) offset = _fileLength;

			long distance = offset - _fileOffset;

			if (distance == 0) {
			}
			else if ((distance > 0) && (distance <= 8192)) {
				byte[] tmp = new byte[distance];
				_fs.Read(tmp, 0, tmp.Length);
			}
			else {
				_fs.Seek(offset, SeekOrigin.Begin);
			}

			_fileOffset = offset;
		}

		private void ParseOldIndex(byte[] data) {
			const uint AVIIF_KEYFRAME = 0x10;

			int dataPos = 0;
			StreamChunkInfo chunkInfo = new StreamChunkInfo();
			AVIOLDINDEXENTRY entry;
			int streamID;
			long firstVideoChunkOffset = -1;
			long offsetCorrection;

			while (dataPos <= (data.Length - 16)) {
				entry.dwChunkID = BitConverterLE.ToUInt32(data, dataPos     );
				entry.dwFlags   = BitConverterLE.ToUInt32(data, dataPos +  4);
				entry.dwOffset  = BitConverterLE.ToUInt32(data, dataPos +  8);
				entry.dwSize    = BitConverterLE.ToUInt32(data, dataPos + 12);
				dataPos += 16;

				streamID = AVIHelper.StreamID(entry.dwChunkID, false);
				if ((streamID == -1) || (streamID >= _streamList.Count)) {
					continue;
				}
				if (_streamList[streamID].FourCC == 0) {
					_streamList[streamID].FourCC = entry.dwChunkID;
				}
				chunkInfo.Offset = (long)entry.dwOffset;
				chunkInfo.Size = entry.dwSize;
				chunkInfo.IsKeyFrame = (entry.dwFlags & AVIIF_KEYFRAME) != 0;

				if ((firstVideoChunkOffset == -1) && (streamID == _videoStreamID)) {
					firstVideoChunkOffset = chunkInfo.Offset;
				}

				_streamList[streamID].ChunkList.Add(chunkInfo);
			}

			if ((_firstVideoChunkOffset == -1) || (firstVideoChunkOffset == -1)) {
				throw new Exception("Video stream not found.");
			}

			// Add 8 because the offset needs to point to the start of the data
			offsetCorrection = (_firstVideoChunkOffset - firstVideoChunkOffset) + 8;

			foreach (AVIStream stream in _streamList) {
				for (int i = 0; i < stream.ChunkList.Count; i++) {
					chunkInfo = stream.ChunkList[i];
					chunkInfo.Offset += offsetCorrection;
					stream.ChunkList[i] = chunkInfo;
				}
			}
		}

		private bool ParseOpenDMLIndex(byte[] data) {
			const byte AVI_INDEX_OF_INDEXES = 0x00;
			const byte AVI_INDEX_OF_CHUNKS = 0x01;

			if (data[3] == AVI_INDEX_OF_INDEXES) {
				return ParseOpenDMLSuperIndex(data);
			}
			if (data[3] == AVI_INDEX_OF_CHUNKS) {
				ParseOpenDMLStandardIndex(data);
				return true;
			}

			throw new Exception("Invalid OpenDML index.");
		}

		private bool ParseOpenDMLSuperIndex(byte[] data) {
			AVISUPERINDEX header;
			AVISUPERINDEXENTRY entry;
			int dataPos = 0;
			long oldOffset = _fileOffset;
			uint chunkID, dataSize, listType;
			byte[] standardIndex;

			header = StructHelper<AVISUPERINDEX>.FromBytes(data, 0, false);
			dataPos += StructHelper<AVISUPERINDEX>.SizeOf;

			while (dataPos <= (data.Length - 16)) {
				if (header.nEntriesInUse == 0) {
					break;
				}

				entry.qwOffset   = BitConverterLE.ToUInt64(data, dataPos     );
				entry.dwSize     = BitConverterLE.ToUInt32(data, dataPos +  8);
				entry.dwDuration = BitConverterLE.ToUInt32(data, dataPos + 12);
				dataPos += 16;

				Seek((long)entry.qwOffset);
				if (ReadChunkHeader(out chunkID, out dataSize, out listType) == -1) {
					return false;
				}
				standardIndex = ReadChunkData(dataSize);
				if (standardIndex.Length < dataSize) {
					return false;
				}
				ParseOpenDMLStandardIndex(standardIndex);

				header.nEntriesInUse--;
			}

			Seek(oldOffset);
			return true;
		}

		private void ParseOpenDMLStandardIndex(byte[] data) {
			AVISTDINDEX header;
			AVISTDINDEXENTRY entry;
			int dataPos = 0;
			int streamID;
			List<StreamChunkInfo> chunkList;
			StreamChunkInfo ci = new StreamChunkInfo();

			header = StructHelper<AVISTDINDEX>.FromBytes(data, 0, false);
			dataPos += StructHelper<AVISTDINDEX>.SizeOf;

			streamID = AVIHelper.StreamID(header.dwChunkID, false);
			if ((streamID == -1) || (streamID >= _streamList.Count)) {
				throw new Exception("Invalid chunk ID in OpenDML standard index.");
			}
			if (_streamList[streamID].FourCC == 0) {
				_streamList[streamID].FourCC = header.dwChunkID;
			}
			chunkList = _streamList[streamID].ChunkList;

			while (dataPos <= (data.Length - 8)) {
				if (header.nEntriesInUse == 0) {
					break;
				}

				entry.dwOffset = BitConverterLE.ToUInt32(data, dataPos    );
				entry.dwSize   = BitConverterLE.ToUInt32(data, dataPos + 4);
				dataPos += 8;

				ci.Offset = (long)(header.qwBaseOffset + entry.dwOffset);
				ci.Size = entry.dwSize & 0x7FFFFFFF;
				ci.IsKeyFrame = ((entry.dwSize & 0x80000000) == 0);
				chunkList.Add(ci);

				header.nEntriesInUse--;
			}
		}

		private class AVIStream {
			public AVIStreamType Type;
			public AVISTREAMHEADER Header;
			public BITMAPINFOHEADER VideoFormat;
			public WAVEFORMATEX AudioFormat;
			public byte[] FormatExtra;
			public byte[] STRNData;
			public uint FourCC;
			public List<StreamChunkInfo> ChunkList;
			public int ChunkIndex;
			public bool IsFirstChunk;

			public AVIStream(AVIStreamType type) {
				Type = type;
				FormatExtra = new byte[0];
				ChunkList = new List<StreamChunkInfo>();
				ChunkIndex = 0;
				IsFirstChunk = true;
			}
		}
	}

	public class AVIWriter {
		const uint MaxOpenDMLSuperIndexEntries = 256;
		const uint MaxOpenDMLStandardIndexEntries = 8192;
		const long MaxAVISize = 0x7FFFFFFF;
		const long MaxAVIXSize = 0x7FFFFFFF;

		private FileStream _fs;
		private long _fileOffset;
		private bool _isFileStarted;
		private List<AVIStream> _streamList;
		private int _videoStreamID;
		private Stack<long> _openLists;
		private List<ListInfo> _closedLists;
		private long _riffOffset;
		private long _avihOffset;
		private long _dmlhOffset;
		private long _moviOffset;

		public AVIWriter(string path) {
			_fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 65536);
			_fileOffset = 0;
			_streamList = new List<AVIStream>();
			_videoStreamID = -1;
			_openLists = new Stack<long>();
			_closedLists = new List<ListInfo>();
		}

		public int AddStream(AVIStreamType type) {
			int streamID = _streamList.Count;

			if ((type == AVIStreamType.Video) && (_videoStreamID == -1)) {
				_videoStreamID = streamID;
			}

			_streamList.Add(new AVIStream(type, streamID));

			return streamID;
		}

		public void SetStreamHeader(int streamID, AVISTREAMHEADER header) {
			_streamList[streamID].Header = header;
		}

		public void SetStreamFormat(int streamID, BITMAPINFOHEADER format) {
			_streamList[streamID].VideoFormat = format;
		}

		public void SetStreamFormat(int streamID, WAVEFORMATEX format) {
			_streamList[streamID].AudioFormat = format;
		}

		public void SetStreamFormatExtra(int streamID, byte[] formatExtra) {
			if (_isFileStarted) {
				throw new Exception("Cannot change stream format extra data after file has been started.");
			}
			_streamList[streamID].FormatExtra = formatExtra;
		}

		public void SetStreamTwoCC(int streamID, string twoCC) {
			_streamList[streamID].FourCC = AVIHelper.StreamFourCC(streamID, twoCC, false);
		}

		public void SetStreamName(int streamID, byte[] name) {
			_streamList[streamID].STRNData = name;
		}

		public void WriteChunk(int streamID, byte[] buff, bool isKeyFrame, int duration) {
			if (!_isFileStarted) {
				StartFile();
			}

			CheckExtend((uint)buff.Length);

			StreamChunkInfo x = new StreamChunkInfo();
			x.Offset = WriteChunk(_streamList[streamID].FourCC, buff);
			x.Size = (uint)buff.Length;
			x.Duration = (uint)duration;
			x.IsKeyFrame = isKeyFrame;
			_streamList[streamID].ChunkList.Add(x);
		}

		public void Close() {
			if (!_isFileStarted) {
				StartFile();
			}

			EndRIFF(true);
			WriteHeaders();
			FinalizeLists();

			_fs.Close();
		}

		private void StartFile() {
			if (_videoStreamID == -1) {
				throw new Exception("AVI must have a video stream.");
			}

			_isFileStarted = true;

			_riffOffset = StartList("RIFF", "AVI ");
			StartList("LIST", "hdrl");
			_avihOffset = WriteChunk("avih", (uint)StructHelper<AVIMAINHEADER>.SizeOf);

			for (int i = 0; i < _streamList.Count; i++) {
				AVIStream s = _streamList[i];

				StartList("LIST", "strl");
				s.STRHOffset = WriteChunk("strh", (uint)StructHelper<AVISTREAMHEADER>.SizeOf);
				s.STRFOffset = WriteChunk("strf", s.MakeSTRFChunk());
				if (s.STRNData != null) {
					WriteChunk("strn", s.STRNData);
				}
				s.INDXOffset = WriteChunk("JUNK", OpenDMLSuperIndexSize(MaxOpenDMLSuperIndexEntries));
				EndList();
			}

			StartList("LIST", "odml");
			_dmlhOffset = WriteChunk("dmlh", MakeDMLHChunk(0));
			EndList();

			EndList(); // 'hdrl'

			// Pad to a multiple of 2K (8K at least)
			if ((_fileOffset < 8192) || ((_fileOffset % 2048) != 0)) {
				int paddingStart = (int)_fileOffset + 8;
				int toNext2K = (2048 - (paddingStart % 2048)) % 2048;
				int to8K = 8192 - paddingStart;

				WriteChunk("JUNK", (uint)Math.Max(to8K, toNext2K));
			}

			_moviOffset = StartList("LIST", "movi");
		}

		private void CheckExtend(uint dataSize) {
			long estSize, maxSize;

			estSize = (_fileOffset - _riffOffset) + dataSize;

			if (_riffOffset == 0) {
				// Include the old-style index in the estimate
				for (int i = 0; i < _streamList.Count; i++) {
					estSize += _streamList[i].ChunkList.Count * 16;
				}

				maxSize = MaxAVISize;
			}
			else {
				maxSize = MaxAVIXSize;
			}

			// Take off a little because the estimate leaves out some details
			maxSize -= 1024;

			if (estSize > maxSize) {
				EndRIFF(false);
				ExtendRIFF();
			}
		}

		private void EndRIFF(bool closing) {
			if (closing && (_riffOffset != 0)) {
				MakeOpenDMLIndexes();
			}

			EndList(); // 'movi'

			if (_riffOffset == 0) {
				for (int i = 0; i < _streamList.Count; i++) {
					_streamList[i].ChunksInFirstMOVI = _streamList[i].ChunkList.Count;
				}

				WriteChunk("idx1", MakeOldIndexChunk());
			}

			EndList(); // 'AVI ' or 'AVIX'
		}

		private void ExtendRIFF() {
			_riffOffset = StartList("RIFF", "AVIX");
			_moviOffset = StartList("LIST", "movi");
		}

		private void WriteHeaders() {
			const uint AVIF_HASINDEX      = 0x00000010;
			const uint AVIF_ISINTERLEAVED = 0x00000100;

			AVIMAINHEADER aviH = new AVIMAINHEADER();
			AVIStream vidStr = _streamList[_videoStreamID];
			AVISTREAMHEADER vidStrH = vidStr.Header;
			BITMAPINFOHEADER vidStrF = vidStr.VideoFormat;

			if (vidStrH.dwRate != 0) {
				aviH.dwMicroSecPerFrame = Convert.ToUInt32(((double)vidStrH.dwScale / vidStrH.dwRate) * 1000000.0);
			}
			aviH.dwFlags = AVIF_HASINDEX | AVIF_ISINTERLEAVED;
			aviH.dwTotalFrames = (uint)vidStr.ChunksInFirstMOVI;
			aviH.dwStreams = (uint)_streamList.Count;
			aviH.dwWidth = (uint)vidStrF.biWidth;
			aviH.dwHeight = (uint)Math.Abs(vidStrF.biHeight);

			Seek(_avihOffset);
			WriteChunk("avih", StructHelper<AVIMAINHEADER>.ToBytes(aviH, false));

			for (int i = 0; i < _streamList.Count; i++) {
				AVIStream s = _streamList[i];
				AVISTREAMHEADER sHeader = s.Header;

				sHeader.dwLength = (s.Type == AVIStreamType.Video) ? (uint)s.ChunkList.Count :
					CalculateDuration(s.ChunkList, 0, s.ChunkList.Count);
				sHeader.dwSuggestedBufferSize = FindLargestChunk(s.ChunkList);

				Seek(s.STRHOffset);
				WriteChunk("strh", StructHelper<AVISTREAMHEADER>.ToBytes(sHeader, false));

				Seek(s.STRFOffset);
				WriteChunk("strf", s.MakeSTRFChunk());

				if (s.OpenDMLSuperIndex != null) {
					Seek(s.INDXOffset);
					WriteChunk("indx", s.OpenDMLSuperIndex);
				}
			}

			Seek(_dmlhOffset);
			WriteChunk("dmlh", MakeDMLHChunk((uint)vidStr.ChunkList.Count));
		}

		private long StartList(string chunkID, string listType) {
			long offset = _fileOffset;
			byte[] hdr = new byte[12];
			_openLists.Push(offset);
			BitConverterLE.WriteBytes(AVIHelper.FourCC(chunkID), hdr, 0);
			BitConverterLE.WriteBytes((uint)0, hdr, 4);
			BitConverterLE.WriteBytes(AVIHelper.FourCC(listType), hdr, 8);
			_fs.Write(hdr, 0, 12);
			_fileOffset += 12;
			return offset;
		}

		private void EndList() {
			ListInfo li;
			li.Offset = _openLists.Pop();
			li.Size = (uint)(_fileOffset - li.Offset - 8);
			_closedLists.Add(li);
		}

		private void FinalizeLists() {
			byte[] hdr = new byte[4];
			foreach (ListInfo li in _closedLists) {
				Seek(li.Offset + 4);
				BitConverterLE.WriteBytes(li.Size, hdr, 0);
				_fs.Write(hdr, 0, 4);
			}
			_fileOffset += 4;
		}

		private void Seek(long offset) {
			_fs.Seek(offset, SeekOrigin.Begin);
			_fileOffset = offset;
		}

		private long WriteChunk(uint chunkID, byte[] data) {
			long offset = _fileOffset;
			byte[] hdr = new byte[8];
			uint len = (uint)data.Length;
			BitConverterLE.WriteBytes(chunkID, hdr, 0);
			BitConverterLE.WriteBytes(len, hdr, 4);
			_fs.Write(hdr, 0, 8);
			_fs.Write(data, 0, (int)len);
			_fileOffset += (long)len + 8;
			if ((len & 1) != 0) {
				_fs.WriteByte(0);
				_fileOffset += 1;
			}
			return offset;
		}

		private long WriteChunk(string chunkID, byte[] data) {
			return WriteChunk(AVIHelper.FourCC(chunkID), data);
		}

		private long WriteChunk(string chunkID, uint length) {
			return WriteChunk(AVIHelper.FourCC(chunkID), new byte[length]);
		}

		private byte[] MakeDMLHChunk(uint dwTotalFrames) {
			byte[] buff = new byte[248];
			BitConverterLE.WriteBytes(dwTotalFrames, buff, 0);
			return buff;
		}

		private byte[] MakeOldIndexChunk() {
			const uint AVIIF_KEYFRAME = 0x10;

			List<List<StreamChunkInfo>> cilList = new List<List<StreamChunkInfo>>();
			List<int> cilIndex = new List<int>();
			List<int> cilLength = new List<int>();
			List<uint> cilFourCC = new List<uint>();
			AVIOLDINDEXENTRY entry;
			byte[] buff;
			int i, entryCount, buffPos, u;

			entryCount = 0;
			for (i = 0; i < _streamList.Count; i++) {
				AVIStream s = _streamList[i];

				if (s.ChunkList.Count > 0) {
					cilList.Add(s.ChunkList);
					cilIndex.Add(0);
					cilLength.Add(s.ChunksInFirstMOVI);
					cilFourCC.Add(s.FourCC);

					entryCount += s.ChunksInFirstMOVI;
				}
			}

			buffPos = 0;
			buff = new byte[entryCount * 16];
			while (entryCount > 0) {
				// Find the chunk with the lowest offset
				u = 0;
				for (i = 1; i < cilList.Count; i++) {
					if (cilList[i][cilIndex[i]].Offset < cilList[u][cilIndex[u]].Offset) {
						u = i;
					}
				}

				entry.dwChunkID = cilFourCC[u];
				entry.dwFlags = cilList[u][cilIndex[u]].IsKeyFrame ? AVIIF_KEYFRAME : 0;
				entry.dwOffset = (uint)(cilList[u][cilIndex[u]].Offset - (_moviOffset + 8));
				entry.dwSize = cilList[u][cilIndex[u]].Size;

				BitConverterLE.WriteBytes(entry.dwChunkID, buff, buffPos     );
				BitConverterLE.WriteBytes(entry.dwFlags,   buff, buffPos +  4);
				BitConverterLE.WriteBytes(entry.dwOffset,  buff, buffPos +  8);
				BitConverterLE.WriteBytes(entry.dwSize,    buff, buffPos + 12);
				buffPos += 16;

				// If all the chunks from this stream have been written in the index,
				// stop checking this stream
				cilIndex[u]++;
				if (cilIndex[u] >= cilLength[u]) {
					cilList.RemoveAt(u);
					cilIndex.RemoveAt(u);
					cilLength.RemoveAt(u);
					cilFourCC.RemoveAt(u);
				}

				entryCount--;
			}

			return buff;
		}

		private void MakeOpenDMLIndexes() {
			for (int iStream = 0; iStream < _streamList.Count; iStream++) {
				const byte AVI_INDEX_OF_INDEXES = 0x00;

				AVIStream stream = _streamList[iStream];
				List<StreamChunkInfo> chunkList = stream.ChunkList;

				AVISUPERINDEX header = new AVISUPERINDEX();
				AVISUPERINDEXENTRY entry;
				int entriesPerChunk, entryOffset, chunksLeft, buffPos;
				uint indexChunkID = AVIHelper.StreamFourCC(AVIHelper.StreamID(stream.FourCC, false), "ix", true);
				byte[] buff;

				buffPos = 0;
				buff = new byte[OpenDMLSuperIndexSize(MaxOpenDMLSuperIndexEntries)];
				entriesPerChunk = (int)CalculateOpenDMLStandardIndexEntryCount(chunkList);
				entryOffset = 0;
				chunksLeft = chunkList.Count;

				header.wLongsPerEntry = 4;
				header.bIndexSubType = 0;
				header.bIndexType= AVI_INDEX_OF_INDEXES;
				header.nEntriesInUse = (uint)((chunksLeft + (entriesPerChunk - 1)) / entriesPerChunk);
				header.dwChunkID = stream.FourCC;

				if (header.nEntriesInUse > MaxOpenDMLSuperIndexEntries) {
					throw new Exception("Too many super-index entries.");
				}

				buffPos += StructHelper<AVISUPERINDEX>.ToBytes(header, buff, buffPos, false);

				while (chunksLeft > 0) {
					int length = Math.Min(entriesPerChunk, chunksLeft);
					byte[] tmp = MakeOpenDMLStandardIndexChunk(chunkList, entryOffset, length,
						stream.FourCC);

					CheckExtend((uint)tmp.Length);

					entry.qwOffset = (ulong)WriteChunk(indexChunkID, tmp);
					entry.dwSize = (uint)tmp.Length + 8;
					entry.dwDuration = (stream.Type == AVIStreamType.Video) ? (uint)length :
						CalculateDuration(chunkList, entryOffset, length);

					BitConverterLE.WriteBytes(entry.qwOffset,   buff, buffPos     );
					BitConverterLE.WriteBytes(entry.dwSize,     buff, buffPos +  8);
					BitConverterLE.WriteBytes(entry.dwDuration, buff, buffPos + 12);
					buffPos += 16;

					entryOffset += length;
					chunksLeft -= length;
				}

				stream.OpenDMLSuperIndex = buff;
			}
		}

		private int CalculateOpenDMLStandardIndexEntryCount(List<StreamChunkInfo> chunkList) {
			int blockSize = (int)MaxOpenDMLStandardIndexEntries;
			int i;

			// Find the maximum block size without the relative offsets going over 2^32
			do {
				int nextBlock = 0;
				long maxOffset = 0;

				for (i = 0; i < chunkList.Count; i++) {
					if (i == nextBlock) {
						maxOffset = chunkList[i].Offset + 0xFFFFFFFFL;
						nextBlock += blockSize;
					}
					else if (chunkList[i].Offset > maxOffset) {
						blockSize = (i < blockSize) ? i : blockSize - 1;
						break;
					}
				}
			}
			while (i < chunkList.Count);

			return blockSize;
		}

		private byte[] MakeOpenDMLStandardIndexChunk(List<StreamChunkInfo> chunkList, int start, int length, uint streamFourCC) {
			const byte AVI_INDEX_OF_CHUNKS = 0x01;

			long baseOffset = chunkList[start].Offset + 8;
			AVISTDINDEX header = new AVISTDINDEX();
			AVISTDINDEXENTRY entry;
			byte[] buff = new byte[OpenDMLStandardIndexSize((uint)length)];
			int buffPos = 0;

			header.wLongsPerEntry = 2;
			header.bIndexSubType = 0;
			header.bIndexType = AVI_INDEX_OF_CHUNKS;
			header.nEntriesInUse = (uint)length;
			header.dwChunkID = streamFourCC;
			header.qwBaseOffset = (ulong)baseOffset;

			buffPos += StructHelper<AVISTDINDEX>.ToBytes(header, buff, buffPos, false);

			for (int i = 0; i < length; i++) {
				StreamChunkInfo c = chunkList[start + i];

				entry.dwOffset = (uint)((c.Offset + 8) - baseOffset);
				entry.dwSize = c.Size | (c.IsKeyFrame ? 0 : 0x80000000);

				BitConverterLE.WriteBytes(entry.dwOffset, buff, buffPos    );
				BitConverterLE.WriteBytes(entry.dwSize  , buff, buffPos + 4);
				buffPos += 8;
			}

			return buff;
		}

		private uint FindLargestChunk(List<StreamChunkInfo> chunkList) {
			uint max = 0;

			for (int i = 0; i < chunkList.Count; i++) {
				if (chunkList[i].Size > max) {
					max = chunkList[i].Size;
				}
			}

			return max;
		}

		private uint CalculateDuration(List<StreamChunkInfo> chunkList, int start, int length) {
			long duration = 0;

			for (int i = 0; i < length; i++) {
				duration += chunkList[start + i].Duration;
			}

			return (uint)Math.Min(duration, 0xFFFFFFFFL);
		}

		private static uint OpenDMLSuperIndexSize(uint entryCount) {
			return (uint)(StructHelper<AVISUPERINDEX>.SizeOf +
				(StructHelper<AVISUPERINDEXENTRY>.SizeOf * entryCount));
		}

		private static uint OpenDMLStandardIndexSize(uint entryCount) {
			return (uint)(StructHelper<AVISTDINDEX>.SizeOf +
				(StructHelper<AVISTDINDEXENTRY>.SizeOf * entryCount));
		}

		private class AVIStream {
			public AVIStreamType Type;
			public uint FourCC;
			public AVISTREAMHEADER Header;
			public BITMAPINFOHEADER VideoFormat;
			public WAVEFORMATEX AudioFormat;
			public byte[] FormatExtra;
			public byte[] STRNData;
			public long STRHOffset;
			public long STRFOffset;
			public long INDXOffset;
			public List<StreamChunkInfo> ChunkList;
			public int ChunksInFirstMOVI;
			public byte[] OpenDMLSuperIndex;

			public AVIStream(AVIStreamType type, int streamID) {
				Type = type;
				FourCC = AVIHelper.StreamFourCC(streamID, AVIHelper.StreamTwoCC(type), false);
				FormatExtra = new byte[0];
				ChunkList = new List<StreamChunkInfo>();
			}

			public byte[] MakeSTRFChunk() {
				byte[] header, full;

				if (Type == AVIStreamType.Video) {
					header = StructHelper<BITMAPINFOHEADER>.ToBytes(VideoFormat, false);
				}
				else if (Type == AVIStreamType.Audio) {
					header = StructHelper<WAVEFORMATEX>.ToBytes(AudioFormat, false);
				}
				else {
					header = new byte[0];
				}

				full = new byte[header.Length + FormatExtra.Length];
				Buffer.BlockCopy(header, 0, full, 0, header.Length);
				Buffer.BlockCopy(FormatExtra, 0, full, header.Length, FormatExtra.Length);

				return full;
			}
		}

		private struct ListInfo {
			public long Offset;
			public uint Size;
		}
	}

	// AVIReader sets Offset to the beginning of the chunk data, AVIWriter sets Offset
	// to the beginning of the chunk header.
	internal struct StreamChunkInfo {
		public long Offset;
		private uint SizeAndFlags;
		public uint Duration;

		private static uint KeyFrameFlag = 0x80000000;
		private static uint SizeBits = 0x7FFFFFFF;

		public uint Size {
			get {
				return SizeAndFlags & SizeBits;
			}
			set {
				SizeAndFlags &= KeyFrameFlag;
				SizeAndFlags |= (value & SizeBits);
			}
		}

		public bool IsKeyFrame {
			get {
				return (SizeAndFlags & KeyFrameFlag) != 0;
			}
			set {
				SizeAndFlags &= SizeBits;
				SizeAndFlags |= (value ? KeyFrameFlag : 0);
			}
		}
	}

	internal static class AVIHelper {
		public static uint FourCC(string fourCC) {
			byte[] bytes = Encoding.ASCII.GetBytes(fourCC);
			return (bytes.Length == 4) ? BitConverterLE.ToUInt32(bytes, 0) : 0x20202020;
		}

		public static string FourCC(uint fourCC) {
			byte[] bytes = new byte[4];
			string s;
			BitConverterLE.WriteBytes(fourCC, bytes, 0);
			s = Encoding.ASCII.GetString(bytes).Replace('\0', ' ');
			return (s.Length == 4) ? s : new string(' ', 4);
		}

		public static uint StreamFourCC(int streamID, string twoCC, bool twoCCFirst) {
			if ((streamID < 0) || (streamID > 255)) {
				throw new Exception("Invalid stream ID.");
			}

			string sID, sFCC;
			sID = streamID.ToString("X");
			if (sID.Length == 1) {
				sID = "0" + sID;
			}
			sFCC = twoCCFirst ? twoCC + sID : sID + twoCC;

			return AVIHelper.FourCC(sFCC);
		}

		public static int StreamID(uint fourCC, bool twoCCFirst) {
			int hH, hL;

			if (!twoCCFirst) {
				hH = (int)((fourCC & 0x000000FF));
				hL = (int)((fourCC & 0x0000FF00) >> 8);
			}
			else {
				hH = (int)((fourCC & 0x00FF0000) >> 16);
				hL = (int)((fourCC & 0xFF000000) >> 24);
			}

			hL = HexCharToNum(hL);
			hH = HexCharToNum(hH);

			if ((hL != -1) && (hH != -1)) {
				return (hH * 16) + hL;
			}
			else {
				return -1;
			}
		}

		public static string StreamTwoCC(uint fourCC, bool twoCCFirst) {
			string fccStr = FourCC(fourCC);
			return twoCCFirst ? fccStr.Substring(0, 2) : fccStr.Substring(2, 2);
		}

		public static string StreamTwoCC(AVIStreamType streamType) {
			switch (streamType) {
				case AVIStreamType.Video:
					return "dc";
				case AVIStreamType.Audio:
					return "wb";
				default:
					return "xx";
			}
		}

		private static int HexCharToNum(int h) {
			if ((h >= '0') && (h <= '9')) {
				return h - '0';
			}
			if ((h >= 'A') && (h <= 'F')) {
				return (h - 'A') + 10;
			}
			if ((h >= 'a') && (h <= 'f')) {
				return (h - 'a') + 10;
			}

			return -1;
		}
	}

	public enum AVIStreamType {
		Video,
		Audio,
		Other
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	internal struct AVIMAINHEADER {
		public uint dwMicroSecPerFrame;
		public uint dwMaxBytesPerSec;
		public uint dwPaddingGranularity;
		public uint dwFlags;
		public uint dwTotalFrames;
		public uint dwInitialFrames;
		public uint dwStreams;
		public uint dwSuggestedBufferSize;
		public uint dwWidth;
		public uint dwHeight;
		public uint dwReserved1;
		public uint dwReserved2;
		public uint dwReserved3;
		public uint dwReserved4;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct AVISTREAMHEADER {
		public uint fccType;
		public uint fccHandler;
		public uint dwFlags;
		public ushort wPriority;
		public ushort wLanguage;
		public uint dwInitialFrames;
		public uint dwScale;
		public uint dwRate;
		public uint dwStart;
		public uint dwLength;
		public uint dwSuggestedBufferSize;
		public uint dwQuality;
		public uint dwSampleSize;
		public short left;
		public short top;
		public short right;
		public short bottom;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct BITMAPINFOHEADER {
		public uint biSize;
		public int biWidth;
		public int biHeight;
		public ushort biPlanes;
		public ushort biBitCount;
		public uint biCompression;
		public uint biSizeImage;
		public int biXPelsPerMeter;
		public int biYPelsPerMeter;
		public uint biClrUsed;
		public uint biClrImportant;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct WAVEFORMATEX {
		public ushort wFormatTag;
		public ushort nChannels;
		public uint nSamplesPerSec;
		public uint nAvgBytesPerSec;
		public ushort nBlockAlign;
		public ushort wBitsPerSample;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	internal struct AVIOLDINDEXENTRY {
		public uint dwChunkID;
		public uint dwFlags;
		public uint dwOffset;
		public uint dwSize;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	internal struct AVISUPERINDEX {
		public ushort wLongsPerEntry;
		public byte bIndexSubType;
		public byte bIndexType;
		public uint nEntriesInUse;
		public uint dwChunkID;
		public uint dwReserved1;
		public uint dwReserved2;
		public uint dwReserved3;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	internal struct AVISUPERINDEXENTRY {
		public ulong qwOffset;
		public uint dwSize;
		public uint dwDuration;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	internal struct AVISTDINDEX {
		public ushort wLongsPerEntry;
		public byte bIndexSubType;
		public byte bIndexType;
		public uint nEntriesInUse;
		public uint dwChunkID;
		public ulong qwBaseOffset;
		public uint dwReserved3;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	internal struct AVISTDINDEXENTRY {
		public uint dwOffset;
		public uint dwSize;
	}
}
