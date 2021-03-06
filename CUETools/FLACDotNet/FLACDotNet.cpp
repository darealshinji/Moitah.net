// ****************************************************************************
// 
// Copyright (c) 2006-2007 Moitah (moitah@yahoo.com)
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 
//   * Redistributions of source code must retain the above copyright notice,
//     this list of conditions and the following disclaimer.
//   * Redistributions in binary form must reproduce the above copyright
//     notice, this list of conditions and the following disclaimer in the
//     documentation and/or other materials provided with the distribution.
//   * Neither the name of the author nor the names of its contributors may be
//     used to endorse or promote products derived from this software without
//     specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.
// 
// ****************************************************************************

using namespace System;
using namespace System::Runtime::InteropServices;

#include "FLAC\all.h"

namespace FLACDotNet {
	[UnmanagedFunctionPointer(CallingConvention::Cdecl)]
	public delegate FLAC__StreamDecoderWriteStatus DecoderWriteDelegate(const FLAC__StreamDecoder *decoder, const FLAC__Frame *frame, const FLAC__int32 * const buffer[], void *client_data);
	[UnmanagedFunctionPointer(CallingConvention::Cdecl)]
	public delegate void DecoderMetadataDelegate(const FLAC__StreamDecoder *decoder, const FLAC__StreamMetadata *metadata, void *client_data);
	[UnmanagedFunctionPointer(CallingConvention::Cdecl)]
	public delegate void DecoderErrorDelegate(const FLAC__StreamDecoder *decoder, FLAC__StreamDecoderErrorStatus status, void *client_data);

	public ref class FLACReader {
	public:
		FLACReader(String^ path) {
			IntPtr pathChars;
			FILE *hFile;

			_writeDel = gcnew DecoderWriteDelegate(this, &FLACReader::WriteCallback);
			_metadataDel = gcnew DecoderMetadataDelegate(this, &FLACReader::MetadataCallback);
			_errorDel = gcnew DecoderErrorDelegate(this, &FLACReader::ErrorCallback);

			_sampleOffset = 0;
			_samplesWaiting = false;
			_sampleBuffer = nullptr;

			pathChars = Marshal::StringToHGlobalUni(path);
			hFile = _wfopen((const wchar_t*)pathChars.ToPointer(), L"rb");
			Marshal::FreeHGlobal(pathChars);
			if (!hFile) {
				throw gcnew Exception("Unable to open file.");
			}

			_decoder = FLAC__stream_decoder_new();
			
			if (FLAC__stream_decoder_init_FILE(_decoder, hFile,
				(FLAC__StreamDecoderWriteCallback)Marshal::GetFunctionPointerForDelegate(_writeDel).ToPointer(),
				(FLAC__StreamDecoderMetadataCallback)Marshal::GetFunctionPointerForDelegate(_metadataDel).ToPointer(),
				(FLAC__StreamDecoderErrorCallback)Marshal::GetFunctionPointerForDelegate(_errorDel).ToPointer(),
				NULL) != FLAC__STREAM_DECODER_INIT_STATUS_OK)
			{
				throw gcnew Exception("Unable to initialize the decoder.");
			}

			if (!FLAC__stream_decoder_process_until_end_of_metadata(_decoder)) {
				throw gcnew Exception("Unable to retrieve metadata.");
			}
		}

		property Int32 BitsPerSample {
			Int32 get() {
				return _bitsPerSample;
			}
		}

		property Int32 ChannelCount {
			Int32 get() {
				return _channelCount;
			}
		}

		property Int32 SampleRate {
			Int32 get() {
				return _sampleRate;
			}
		}

		property Int64 Length {
			Int64 get() {
				return _sampleCount;
			}
		}

		property Int64 Position {
			Int64 get() {
				return _sampleOffset;
			}
			void set(Int64 offset) {
				_sampleOffset = offset;
				_samplesWaiting = false;
				if (!FLAC__stream_decoder_seek_absolute(_decoder, offset)) {
					throw gcnew Exception("Unable to seek.");
				}
			}
		}

		property Int64 Remaining {
			Int64 get() {
				return _sampleCount - _sampleOffset;
			}
		}

		void Close() {
			FLAC__stream_decoder_finish(_decoder);
			FLAC__stream_decoder_delete(_decoder);
		}

		Int32 Read([Out] array<Int32, 2>^% sampleBuffer) {
			int sampleCount;

			while (!_samplesWaiting) {
				if (!FLAC__stream_decoder_process_single(_decoder)) {
					throw gcnew Exception("An error occurred while decoding.");
				}
			}

			sampleCount = _sampleBuffer->GetLength(0);
			sampleBuffer = _sampleBuffer;
			_sampleOffset += sampleCount;
			_samplesWaiting = false;

			return sampleCount;
		}

	private:
		DecoderWriteDelegate^ _writeDel;
		DecoderMetadataDelegate^ _metadataDel;
		DecoderErrorDelegate^ _errorDel;
		FLAC__StreamDecoder *_decoder;
		Int64 _sampleCount, _sampleOffset;
		Int32 _bitsPerSample, _channelCount, _sampleRate;
		array<Int32, 2>^ _sampleBuffer;
		bool _samplesWaiting;

		FLAC__StreamDecoderWriteStatus WriteCallback(const FLAC__StreamDecoder *decoder,
			const FLAC__Frame *frame, const FLAC__int32 * const buffer[], void *client_data)
		{
			Int32 sampleCount = frame->header.blocksize;

			if (_samplesWaiting) {
				throw gcnew Exception("Received unrequested samples.");
			}

			if ((frame->header.bits_per_sample != _bitsPerSample) ||
				(frame->header.channels != _channelCount) ||
				(frame->header.sample_rate != _sampleRate))
			{
				throw gcnew Exception("Format changes within a file are not allowed.");
			}

			if ((_sampleBuffer == nullptr) || (_sampleBuffer->GetLength(0) != sampleCount)) {
				_sampleBuffer = gcnew array<Int32, 2>(sampleCount, _channelCount);
			}

			for (Int32 iChan = 0; iChan < _channelCount; iChan++) {
				interior_ptr<Int32> pMyBuffer = &_sampleBuffer[0, iChan];
				const FLAC__int32 *pFLACBuffer = buffer[iChan];
				const FLAC__int32 *pFLACBufferEnd = pFLACBuffer + sampleCount;

				while (pFLACBuffer < pFLACBufferEnd) {
					*pMyBuffer = *pFLACBuffer;
					pMyBuffer += _channelCount;
					pFLACBuffer++;
				}
			}

			_samplesWaiting = true;

			return FLAC__STREAM_DECODER_WRITE_STATUS_CONTINUE;
		}

		void MetadataCallback(const FLAC__StreamDecoder *decoder,
			const FLAC__StreamMetadata *metadata, void *client_data)
		{
			if (metadata->type == FLAC__METADATA_TYPE_STREAMINFO) {
				_bitsPerSample = metadata->data.stream_info.bits_per_sample;
				_channelCount = metadata->data.stream_info.channels;
				_sampleRate = metadata->data.stream_info.sample_rate;
				_sampleCount = metadata->data.stream_info.total_samples;
			}
		}

		void ErrorCallback(const FLAC__StreamDecoder *decoder,
			FLAC__StreamDecoderErrorStatus status, void *client_data)
		{
			switch (status) {
				case FLAC__STREAM_DECODER_ERROR_STATUS_LOST_SYNC:
					throw gcnew Exception("Synchronization was lost.");
				case FLAC__STREAM_DECODER_ERROR_STATUS_BAD_HEADER:
					throw gcnew Exception("Encountered a corrupted frame header.");
				case FLAC__STREAM_DECODER_ERROR_STATUS_FRAME_CRC_MISMATCH:
					throw gcnew Exception("Frame CRC mismatch.");
				default:
					throw gcnew Exception("An unknown error has occurred.");
			}
		}
	};

	public ref class FLACWriter {
	public:
		FLACWriter(String^ path, Int32 bitsPerSample, Int32 channelCount, Int32 sampleRate) {
			_initialized = false;
			_path = path;
			_finalSampleCount = 0;
			_samplesWritten = 0;
			_bitsPerSample = bitsPerSample;
			_channelCount = channelCount;
			_sampleRate = sampleRate;
			_compressionLevel = 5;
			_paddingLength = 8192;
			_verify = false;

			_encoder = FLAC__stream_encoder_new();

			FLAC__stream_encoder_set_bits_per_sample(_encoder, bitsPerSample);
			FLAC__stream_encoder_set_channels(_encoder, channelCount);
			FLAC__stream_encoder_set_sample_rate(_encoder, sampleRate);
		}

		void Close() {
			FLAC__stream_encoder_finish(_encoder);

			for (int i = 0; i < _metadataCount; i++) {
				FLAC__metadata_object_delete(_metadataList[i]);
			}
			delete[] _metadataList;
			_metadataList = 0;
			_metadataCount = 0;

			FLAC__stream_encoder_delete(_encoder);

			if ((_finalSampleCount != 0) && (_samplesWritten != _finalSampleCount)) {
				throw gcnew Exception("Samples written differs from the expected sample count.");
			}
		}

		property Int64 FinalSampleCount {
			Int64 get() {
				return _finalSampleCount;
			}
			void set(Int64 value) {
				if (value < 0) {
					throw gcnew Exception("Invalid final sample count.");
				}
				if (_initialized) {
					throw gcnew Exception("Final sample count cannot be changed after encoding begins.");
				}
				_finalSampleCount = value;
			}
		}

		property Int32 CompressionLevel {
			Int32 get() {
				return _compressionLevel;
			}
			void set(Int32 value) {
				if ((value < 0) || (value > 8)) {
					throw gcnew Exception("Invalid compression level.");
				}
				_compressionLevel = value;
			}
		}

		property Boolean Verify {
			Boolean get() {
				return _verify;
			}
			void set(Boolean value) {
				_verify = value;
			}
		}

		property Int32 PaddingLength {
			Int32 get() {
				return _paddingLength;
			}
			void set(Int32 value) {
				if (value < 0) {
					throw gcnew Exception("Invalid padding length.");
				}
				_paddingLength = value;
			}
		}

		void Write(array<Int32, 2>^ sampleBuffer, Int32 sampleCount) {
			if (!_initialized) Initialize();

			pin_ptr<Int32> pSampleBuffer = &sampleBuffer[0, 0];

			if (!FLAC__stream_encoder_process_interleaved(_encoder,
				(const FLAC__int32*)pSampleBuffer, sampleCount))
			{
				throw gcnew Exception("An error occurred while encoding.");
			}

			_samplesWritten += sampleCount;
		}

	private:
		FLAC__StreamEncoder *_encoder;
		bool _initialized;
		String^ _path;
		Int64 _finalSampleCount, _samplesWritten;
		Int32 _bitsPerSample, _channelCount, _sampleRate;
		Int32 _compressionLevel;
		Int32 _paddingLength;
		Boolean _verify;
		FLAC__StreamMetadata **_metadataList;
		int _metadataCount;

		void Initialize() {
			FLAC__StreamMetadata *padding, *seektable, *vorbiscomment;
			IntPtr pathChars;
			FILE *hFile;

			_metadataList = new FLAC__StreamMetadata*[8];
			_metadataCount = 0;

			if (_finalSampleCount != 0) {
				seektable = FLAC__metadata_object_new(FLAC__METADATA_TYPE_SEEKTABLE);
				FLAC__metadata_object_seektable_template_append_spaced_points_by_samples(
					seektable, _sampleRate * 10, _finalSampleCount);
				FLAC__metadata_object_seektable_template_sort(seektable, true);
				_metadataList[_metadataCount++] = seektable;
			}

			vorbiscomment = FLAC__metadata_object_new(FLAC__METADATA_TYPE_VORBIS_COMMENT);
			_metadataList[_metadataCount++] = vorbiscomment;
	 
			if (_paddingLength != 0) {
				padding = FLAC__metadata_object_new(FLAC__METADATA_TYPE_PADDING);
				padding->length = _paddingLength;
				_metadataList[_metadataCount++] = padding;
			}

			FLAC__stream_encoder_set_metadata(_encoder, _metadataList, _metadataCount);

			FLAC__stream_encoder_set_verify(_encoder, _verify);

			if (_finalSampleCount != 0) {
				FLAC__stream_encoder_set_total_samples_estimate(_encoder, _finalSampleCount);
			}

			FLAC__stream_encoder_set_compression_level(_encoder, _compressionLevel);

			pathChars = Marshal::StringToHGlobalUni(_path);
			hFile = _wfopen((const wchar_t*)pathChars.ToPointer(), L"w+b");
			Marshal::FreeHGlobal(pathChars);

			if (FLAC__stream_encoder_init_FILE(_encoder, hFile, NULL, NULL) !=
				FLAC__STREAM_ENCODER_INIT_STATUS_OK)
			{
				throw gcnew Exception("Unable to initialize the encoder.");
			}

			_initialized = true;
		}
	};
}