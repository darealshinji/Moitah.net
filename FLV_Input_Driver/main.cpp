// ****************************************************************************
// 
// FLV Input Driver Plugin for VirtualDub
// Copyright (C) 2007-2008  Moitah (moitah@yahoo.com)
// 
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
// 
// ****************************************************************************

#define _CRT_SECURE_NO_WARNINGS
#include <vd2/plugin/vdplugin.h>
#include <vd2/plugin/vdinputdriver.h>
#include <vd2/VDXFrame/Unknown.h>
#include <vector>
#include <algorithm>
#include <math.h>
#include <io.h>
#include <windows.h>
#include <vfw.h>
#include "resource.h"

///////////////////////////////////////////////////////////////////////////////

enum FLVFourCC : DWORD {
	kFCC_FLV1 = MAKEFOURCC('F', 'L', 'V', '1'),
	kFCC_VP6F = MAKEFOURCC('V', 'P', '6', 'F'),
};

enum FLVCodec {
	kVC_None,
	kVC_H263,
	kVC_VP6,
	kVC_VP6A,
	kVC_Screen,
	kVC_Screen2,
	kAC_None,
	kAC_PCM,
	kAC_ADPCM,
	kAC_MP3,
	kAC_Nellymoser,
	kAC_Nellymoser8M,
};

enum FrameRateConversion {
	kFRC_None,
	kFRC_Automatic,
	kFRC_ToTarget
};

struct FLVTagInfo {
	sint64	mDataOffset;
	uint32	mDataSize;   // Most significant byte contains flags: 0x01 = Keyframe
	uint32	mTimeStamp;  // In milliseconds
};

struct AudioFrameInfo {
	uint32 mTagIndex;
	uint32 mTagDataOffset;
	uint32 mSize;
};

struct FLVInputOptions {
	int mOptionsVersion;
	int mFrameRateConversion;
	double mTargetFrameRate;

	FLVInputOptions() {
		mOptionsVersion = 1;
		mFrameRateConversion = kFRC_Automatic;
		mTargetFrameRate = 30.0;
	}
};

class FLVData {
public:
	FILE *mFile;
	int mVideoCodec;
	BITMAPINFOHEADER mVideoFormat;
	std::vector<FLVTagInfo> mVideoTags;
	sint64 mVideoTotalBytes;
	uint32 mVideoTimeStampRange;
	double mVideoFrameRate;
	double mVideoFrameRateOriginal;
	int mVideoMaxTimeStampError;
	int mVideoMedianTimeStampError;
	int mVideoAverageTimeStampError;
	int mAudioCodec;
	std::vector<FLVTagInfo> mAudioTags;
	std::vector<AudioFrameInfo> mAudioFrames;
	std::vector<sint64> mAudioStreamOffsets;
	MPEGLAYER3WAVEFORMAT mAudioFormat;   // Used for PCM too (only the WAVEFORMATEX part)
	bool mAudioVariableSizeSamples;
	sint64 mAudioTotalBytes;
	uint32 mAudioMaxFrameSize;
	uint32 mAudioSamplesPerFrame;
};

static HMODULE g_hModule;

BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved) {
	if (fdwReason == DLL_PROCESS_ATTACH) {
		g_hModule = (HMODULE)hinstDLL;
	}
	return TRUE;
}

bool IsWindowsNT() {
	return (GetVersion() < 0x80000000);
}

uint32 GetInt32(uint8 *&buff) {
	uint32 x = ((uint32)buff[0] << 24) |
			   ((uint32)buff[1] << 16) |
			   ((uint32)buff[2] <<  8) |
			   ((uint32)buff[3]      );
	buff += 4;
	return x;
}

uint32 GetInt24(uint8 *&buff) {
	uint32 x = ((uint32)buff[0] << 16) |
			   ((uint32)buff[1] <<  8) |
			   ((uint32)buff[2]      );
	buff += 3;
	return x;
}

uint32 GetInt16(uint8 *&buff) {
	uint32 x = ((uint32)buff[0] <<  8) |
			   ((uint32)buff[1]      );
	buff += 2;
	return x;
}

uint32 GetInt8(uint8 *&buff) {
	return (uint32)*(buff++);
}

uint64 ToBits(uint8 *buff, uint32 buffLen) {
	uint64 bits = 0;
	uint8 *pBits = (uint8*)&bits;
	int i = 7;

	while ((i > 0) && (buffLen > 0)) {
		pBits[i--] = *(buff++);
		buffLen--;
	}

	return bits;
}

uint32 GetBits(uint64 &bits, int len) {
	uint32 x = (uint32)(bits >> (64 - len));
	bits <<= len;
	return x;
}

void GetFrameSize(uint8 *buff, uint32 buffLen, int codec, sint32 &width, sint32 &height) {
	if (codec == kVC_H263) {
		uint64 bits = ToBits(buff + 2, buffLen - 2);
		uint32 format;

		if (buffLen < 3) return;
		if (GetBits(bits, 1) != 1) return;
		GetBits(bits, 5);
		GetBits(bits, 8);
		format = GetBits(bits, 3);

		switch (format) {
			case 0:
				if (buffLen < 5) return;
				width = (sint32)GetBits(bits, 8);
				height = (sint32)GetBits(bits, 8);
				break;
			case 1:
				if (buffLen < 7) return;
				width = (sint32)GetBits(bits, 16);
				height = (sint32)GetBits(bits, 16);
				break;
			case 2: width = 352; height = 288; break;
			case 3: width = 176; height = 144; break;
			case 4: width = 128; height =  96; break;
			case 5: width = 320; height = 240; break;
			case 6: width = 160; height = 120; break;
		}
	}
	else if ((codec == kVC_VP6) || (codec == kVC_VP6A)) {
		uint64 bits = ToBits(buff, buffLen);
		uint32 separatedCoeffFlag, filterHeader;

		if (buffLen < 6) return;
		if (GetBits(bits, 1)) return; // Delta frame
		GetBits(bits, 6);
		separatedCoeffFlag = GetBits(bits, 1);
		GetBits(bits, 5);
		filterHeader = GetBits(bits, 2);
		GetBits(bits, 1);
		if ((separatedCoeffFlag != 0) || (filterHeader == 0)) {
			if (buffLen < 8) return;
			GetBits(bits, 16);
		}

		height = (sint32)GetBits(bits, 8) * 16;
		width = (sint32)GetBits(bits, 8) * 16;
	}
}

void ParseMP3Frames(uint8* buff, uint32 length, uint32 tagIndex, std::vector<AudioFrameInfo> &frameList) {
	const uint32 MPEG1BitRate[] = { 0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 0 };
	const uint32 MPEG2XBitRate[] = { 0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0 };
	const uint32 MPEG1SampleRate[] = { 44100, 48000, 32000, 0 };
	const uint32 MPEG20SampleRate[] = { 22050, 24000, 16000, 0 };
	const uint32 MPEG25SampleRate[] = { 11025, 12000, 8000, 0 };

	int offset = 0;

	while (length >= 4) {
		uint64 header;
		uint32 mpegVersion, layer, bitRate, sampleRate, padding;
		uint32 frameLen;
		AudioFrameInfo frameInfo;

		header = ToBits(buff, 4);
		if (GetBits(header, 11) != 0x7FF) {
			break;
		}
		mpegVersion = GetBits(header, 2);
		layer = GetBits(header, 2);
		GetBits(header, 1);
		bitRate = GetBits(header, 4);
		sampleRate = GetBits(header, 2);
		padding = GetBits(header, 1);

		if ((mpegVersion == 1) || (layer != 1) || (bitRate == 0) ||
			(bitRate == 15) || (sampleRate == 3))
		{
			break;
		}

		bitRate = ((mpegVersion == 3) ? MPEG1BitRate[bitRate] : MPEG2XBitRate[bitRate]) * 1000;

		if (mpegVersion == 3)
			sampleRate = MPEG1SampleRate[sampleRate];
		else if (mpegVersion == 2)
			sampleRate = MPEG20SampleRate[sampleRate];
		else
			sampleRate = MPEG25SampleRate[sampleRate];

		frameLen = ((mpegVersion == 3) ? 144 : 72) * bitRate / sampleRate + padding;
		if (frameLen > length) {
			break;
		}

		frameInfo.mTagIndex = tagIndex;
		frameInfo.mTagDataOffset = offset;
		frameInfo.mSize = frameLen;
		frameList.push_back(frameInfo);

		buff += frameLen;
		offset += frameLen;
		length -= frameLen;
	}
}

void ProcessVideoStream(std::vector<FLVTagInfo> &frameList, FLVInputOptions options,
	uint32 &timeStampRange, double &frameRate, double &frameRateOriginal, int &maxTSError,
	int &medianTSError, int &averageTSError)
{
	uint32 tsBase;
	unsigned int i;
	double idealDelta;
	sint64 sumTSError;
	int dropCount;
	std::vector<int> tsErrorList;

	if (frameList.size() == 0) return;

	tsBase = frameList[0].mTimeStamp;
	timeStampRange = frameList[frameList.size() - 1].mTimeStamp - tsBase;
	frameRateOriginal = (frameList.size() - 1) / ((double)timeStampRange / 1000.0);

	if ((options.mFrameRateConversion == kFRC_ToTarget) && (options.mTargetFrameRate < frameRateOriginal)) {
		options.mFrameRateConversion = kFRC_None;
	}

	if (options.mFrameRateConversion == kFRC_Automatic) {
		uint32 minDelta = 0xFFFFFFFF;
		uint32 minDeltaCount = 0;
		uint32 minDeltaPlusOneCount = 0;

		// Find the smallest timestamp delta
		for (i = 1; i < frameList.size(); i++) {
			uint32 delta = frameList[i].mTimeStamp - frameList[i - 1].mTimeStamp;
			if ((delta > 0) && (delta < minDelta)) {
				minDelta = delta;
			}
		}

		// Count how many times this delta and delta+1 appear (e.g. 33/34 ms for NTSC)
		for (i = 1; i < frameList.size(); i++) {
			uint32 delta = frameList[i].mTimeStamp - frameList[i - 1].mTimeStamp;
			if (delta == minDelta) {
				minDeltaCount++;
			}
			else if (delta == (minDelta + 1)) {
				minDeltaPlusOneCount++;
			}
		}

		idealDelta = (double)((minDeltaCount * minDelta) + (minDeltaPlusOneCount * (minDelta + 1))) /
			(minDeltaCount + minDeltaPlusOneCount);
	}

	if (options.mFrameRateConversion != kFRC_None) {
		std::vector<FLVTagInfo> frameListPadded;

		// Try to maintain constant framerate by adding drops to fill in gaps
		frameListPadded.reserve(frameList.size());
		frameListPadded.push_back(frameList[0]);
		for (i = 1; i < frameList.size(); i++) {
			uint32 tsA = frameList[i - 1].mTimeStamp;
			uint32 tsB = frameList[i    ].mTimeStamp;
			int count;
			if (options.mFrameRateConversion == kFRC_Automatic) {
				count = (int)(((tsB - tsA) / idealDelta) + 0.5);
			}
			else {
				double tsError = (frameListPadded.size() / options.mTargetFrameRate * 1000.0) - (tsB - tsBase);
				count = (int)((-tsError / 1000.0 * options.mTargetFrameRate) + 0.5) + 1;
			}
			if (count > 1) {
				double paddingDelta = (double)(tsB - tsA) / count;
				for (int j = 1; j < count; j++) {
					FLVTagInfo drop;
					drop.mDataOffset = 0;
					drop.mDataSize = 0;
					drop.mTimeStamp = tsA + (uint32)((paddingDelta * j) + 0.5);
					frameListPadded.push_back(drop);
				}
			}
			frameListPadded.push_back(frameList[i]);
		}

		frameList = frameListPadded;
		if (options.mFrameRateConversion == kFRC_ToTarget) {
			timeStampRange = (uint32)(((frameList.size() - 1) / options.mTargetFrameRate * 1000.0) + 0.5);
		}
		frameRate = (frameList.size() - 1) / ((double)timeStampRange / 1000.0);
	}
	else {
		frameRate = frameRateOriginal;
	}

	// Calculate how much each frame's display time will be off from its timestamp
	// when being played back at constant framerate
	sumTSError = 0;
	dropCount = 0;
	for (i = 0; i < frameList.size(); i++) {
		if (frameList[i].mDataSize == 0) {
			dropCount++;
		}
		else {
			int tsError = (int)((i / frameRate * 1000.0) - (frameList[i].mTimeStamp - tsBase));
			tsErrorList.push_back(tsError);
			sumTSError += tsError;
		}
	}
	averageTSError = (int)(((double)sumTSError / (frameList.size() - dropCount)) + 0.5);
	std::sort(tsErrorList.begin(), tsErrorList.end());
	maxTSError = tsErrorList[tsErrorList.size() - 1];
	if (abs(tsErrorList[0]) > abs(maxTSError)) {
		maxTSError = tsErrorList[0];
	}
	medianTSError = tsErrorList[(tsErrorList.size() - 1) / 2];
}

void AnalyzeVideoStream(std::vector<FLVTagInfo> &tagList, sint64 &totalSize) {
	totalSize = 0;
	for (unsigned int i = 0; i < tagList.size(); i++) {
		totalSize += tagList[i].mDataSize & 0x00FFFFFF;
	}
}

void AnalyzeAudioStream(std::vector<FLVTagInfo> &tagList, sint64 &totalSize, uint32 &maxSize,
	std::vector<sint64> &streamOffsetList)
{
	totalSize = 0;
	maxSize = 0;
	for (unsigned int i = 0; i < tagList.size(); i++) {
		uint32 size = tagList[i].mDataSize & 0x00FFFFFF;
		streamOffsetList.push_back(totalSize);
		totalSize += size;
		if (size > maxSize) maxSize = size;
	}
}

///////////////////////////////////////////////////////////////////////////////

class VideoSourceFLV : public vdxunknown<IVDXStreamSource>, public IVDXVideoSource {
public:
	VideoSourceFLV(FLVData& data);
	~VideoSourceFLV();

	int VDXAPIENTRY AddRef();
	int VDXAPIENTRY Release();
	void *VDXAPIENTRY AsInterface(uint32 iid);

	void		VDXAPIENTRY GetStreamSourceInfo(VDXStreamSourceInfo&);
	bool		VDXAPIENTRY Read(sint64 lStart, uint32 lCount, void *lpBuffer, uint32 cbBuffer, uint32 *lBytesRead, uint32 *lSamplesRead);

	const void *VDXAPIENTRY GetDirectFormat();
	int			VDXAPIENTRY GetDirectFormatLen();

	ErrorMode VDXAPIENTRY GetDecodeErrorMode();
	void VDXAPIENTRY SetDecodeErrorMode(ErrorMode mode);
	bool VDXAPIENTRY IsDecodeErrorModeSupported(ErrorMode mode);

	bool VDXAPIENTRY IsVBR();
	sint64 VDXAPIENTRY TimeToPositionVBR(sint64 us);
	sint64 VDXAPIENTRY PositionToTimeVBR(sint64 samples);

	void VDXAPIENTRY GetVideoSourceInfo(VDXVideoSourceInfo& info);

	bool VDXAPIENTRY CreateVideoDecoderModel(IVDXVideoDecoderModel **ppModel);
	bool VDXAPIENTRY CreateVideoDecoder(IVDXVideoDecoder **ppDecoder);

	void		VDXAPIENTRY GetSampleInfo(sint64 sample_num, VDXVideoFrameInfo& frameInfo);

	bool		VDXAPIENTRY IsKey(sint64 lSample);

	sint64		VDXAPIENTRY GetFrameNumberForSample(sint64 sample_num);
	sint64		VDXAPIENTRY GetSampleNumberForFrame(sint64 display_num);
	sint64		VDXAPIENTRY GetRealFrame(sint64 display_num);

	sint64		VDXAPIENTRY GetSampleBytePosition(sint64 sample_num);

protected:
	FLVData&	mData;
};

VideoSourceFLV::VideoSourceFLV(FLVData& data)
	: mData(data)
{
}

VideoSourceFLV::~VideoSourceFLV() {
}

int VideoSourceFLV::AddRef() {
	return vdxunknown<IVDXStreamSource>::AddRef();
}

int VideoSourceFLV::Release() {
	return vdxunknown<IVDXStreamSource>::Release();
}

void *VDXAPIENTRY VideoSourceFLV::AsInterface(uint32 iid) {
	if (iid == IVDXVideoSource::kIID)
		return static_cast<IVDXVideoSource *>(this);

	return vdxunknown<IVDXStreamSource>::AsInterface(iid);
}

void VDXAPIENTRY VideoSourceFLV::GetStreamSourceInfo(VDXStreamSourceInfo& info) {
	int frameCount = mData.mVideoTags.size();
	if (mData.mVideoTimeStampRange != 0) {
		info.mSampleRate.mNumerator = (frameCount - 1) * 1000;
		info.mSampleRate.mDenominator = mData.mVideoTimeStampRange;
	}
	else {
		info.mSampleRate.mNumerator = 30;
		info.mSampleRate.mDenominator = 1;
	}
	info.mSampleCount = frameCount;
}

bool VideoSourceFLV::Read(sint64 lStart64, uint32 lCount, void *lpBuffer, uint32 cbBuffer, uint32 *lBytesRead, uint32 *lSamplesRead) {
	FLVTagInfo tagInfo = mData.mVideoTags[(uint32)lStart64];

	*lBytesRead = tagInfo.mDataSize & 0x00FFFFFF;
	*lSamplesRead = 1;

	if (lpBuffer && *lBytesRead) {
		if (cbBuffer < *lBytesRead)
			return false;

		_fseeki64(mData.mFile, tagInfo.mDataOffset, SEEK_SET);
		fread(lpBuffer, 1, *lBytesRead, mData.mFile);
	}

	return true;
}

const void *VideoSourceFLV::GetDirectFormat() {
	return &mData.mVideoFormat;
}

int VideoSourceFLV::GetDirectFormatLen() {
	return sizeof(mData.mVideoFormat);
}

IVDXStreamSource::ErrorMode VideoSourceFLV::GetDecodeErrorMode() {
	return IVDXStreamSource::kErrorModeReportAll;
}

void VideoSourceFLV::SetDecodeErrorMode(IVDXStreamSource::ErrorMode mode) {
}

bool VideoSourceFLV::IsDecodeErrorModeSupported(IVDXStreamSource::ErrorMode mode) {
	return mode == IVDXStreamSource::kErrorModeReportAll;
}

bool VideoSourceFLV::IsVBR() {
	return false;
}

sint64 VideoSourceFLV::TimeToPositionVBR(sint64 us) {
	return 0;
}

sint64 VideoSourceFLV::PositionToTimeVBR(sint64 samples) {
	return 0;
}

void VideoSourceFLV::GetVideoSourceInfo(VDXVideoSourceInfo& info) {
	info.mFlags = 0;
	info.mWidth = mData.mVideoFormat.biWidth;
	info.mHeight = mData.mVideoFormat.biHeight;
	info.mDecoderModel = VDXVideoSourceInfo::kDecoderModelDefaultIP;
}

bool VideoSourceFLV::CreateVideoDecoderModel(IVDXVideoDecoderModel **ppModel) {
	return false;
}

bool VideoSourceFLV::CreateVideoDecoder(IVDXVideoDecoder **ppDecoder) {
	return false;
}

void VideoSourceFLV::GetSampleInfo(sint64 sample_num, VDXVideoFrameInfo& frameInfo) {
	FLVTagInfo tagInfo = mData.mVideoTags[(uint32)sample_num];

	frameInfo.mBytePosition = (tagInfo.mDataSize != 0) ? tagInfo.mDataOffset : -1;

	if (tagInfo.mDataSize & 0xFF000000) {
		frameInfo.mFrameType = kVDXVFT_Independent;
		frameInfo.mTypeChar = 'K';
	}
	else {
		frameInfo.mFrameType = kVDXVFT_Predicted;
		if (tagInfo.mDataSize != 0) {
			frameInfo.mTypeChar = ' ';
		}
		else {
			frameInfo.mTypeChar = 'D';
		}
	}
}

bool VideoSourceFLV::IsKey(sint64 sample) {
	return !!(mData.mVideoTags[(uint32)sample].mDataSize & 0xFF000000);
}

sint64 VideoSourceFLV::GetFrameNumberForSample(sint64 sample_num) {
	return sample_num;
}

sint64 VideoSourceFLV::GetSampleNumberForFrame(sint64 display_num) {
	return display_num;
}

sint64 VideoSourceFLV::GetRealFrame(sint64 display_num) {
	return display_num;
}

sint64 VideoSourceFLV::GetSampleBytePosition(sint64 sample_num) {
	return -1;
}

///////////////////////////////////////////////////////////////////////////////

class AudioSourceFLV : public vdxunknown<IVDXStreamSource>, public IVDXStreamSourceV3, public IVDXAudioSource {
public:
	AudioSourceFLV(FLVData& data);
	~AudioSourceFLV();

	int VDXAPIENTRY AddRef();
	int VDXAPIENTRY Release();
	void *VDXAPIENTRY AsInterface(uint32 iid);

	void		VDXAPIENTRY GetStreamSourceInfo(VDXStreamSourceInfo&);
	bool		VDXAPIENTRY Read(sint64 lStart, uint32 lCount, void *lpBuffer, uint32 cbBuffer, uint32 *lBytesRead, uint32 *lSamplesRead);

	const void *VDXAPIENTRY GetDirectFormat();
	int			VDXAPIENTRY GetDirectFormatLen();

	ErrorMode VDXAPIENTRY GetDecodeErrorMode();
	void VDXAPIENTRY SetDecodeErrorMode(ErrorMode mode);
	bool VDXAPIENTRY IsDecodeErrorModeSupported(ErrorMode mode);

	bool VDXAPIENTRY IsVBR();
	sint64 VDXAPIENTRY TimeToPositionVBR(sint64 us);
	sint64 VDXAPIENTRY PositionToTimeVBR(sint64 samples);

	void VDXAPIENTRY GetStreamSourceInfoV3(VDXStreamSourceInfoV3& info);

	void VDXAPIENTRY GetAudioSourceInfo(VDXAudioSourceInfo& info);

protected:
	FLVData&	mData;
};

AudioSourceFLV::AudioSourceFLV(FLVData& data)
	: mData(data)
{
}

AudioSourceFLV::~AudioSourceFLV() {
}

int AudioSourceFLV::AddRef() {
	return vdxunknown<IVDXStreamSource>::AddRef();
}

int AudioSourceFLV::Release() {
	return vdxunknown<IVDXStreamSource>::Release();
}

void *VDXAPIENTRY AudioSourceFLV::AsInterface(uint32 iid) {
	if (iid == IVDXAudioSource::kIID)
		return static_cast<IVDXAudioSource *>(this);
	if (iid == IVDXStreamSourceV3::kIID)
		return static_cast<IVDXStreamSourceV3 *>(this);

	return vdxunknown<IVDXStreamSource>::AsInterface(iid);
}

void VDXAPIENTRY AudioSourceFLV::GetStreamSourceInfo(VDXStreamSourceInfo& info) {
	if (mData.mAudioVariableSizeSamples) {
		info.mSampleRate.mNumerator = mData.mAudioFormat.wfx.nSamplesPerSec * (mData.mAudioFormat.wfx.nBlockAlign / mData.mAudioSamplesPerFrame);
		info.mSampleRate.mDenominator = mData.mAudioFormat.wfx.nBlockAlign;
		info.mSampleCount = mData.mAudioFrames.size();
	}
	else {
		info.mSampleRate.mNumerator = mData.mAudioFormat.wfx.nAvgBytesPerSec;
		info.mSampleRate.mDenominator = mData.mAudioFormat.wfx.nBlockAlign;
		info.mSampleCount = mData.mAudioTotalBytes / mData.mAudioFormat.wfx.nBlockAlign;
	}
}

void VDXAPIENTRY AudioSourceFLV::GetStreamSourceInfoV3(VDXStreamSourceInfoV3& info) {
	GetStreamSourceInfo(info.mInfo);

	if (mData.mAudioVariableSizeSamples)
		info.mFlags = VDXStreamSourceInfoV3::kFlagVariableSizeSamples;
}

bool AudioSourceFLV::Read(sint64 lStart64, uint32 lCount, void *lpBuffer, uint32 cbBuffer, uint32 *lBytesRead, uint32 *lSamplesRead) {
	sint64 readOffset;

	if (mData.mAudioVariableSizeSamples) {
		AudioFrameInfo frameInfo = mData.mAudioFrames[(unsigned int)lStart64];
		FLVTagInfo tagInfo = mData.mAudioTags[frameInfo.mTagIndex];

		readOffset = tagInfo.mDataOffset + frameInfo.mTagDataOffset;
		*lBytesRead = frameInfo.mSize;
		*lSamplesRead = 1;
	}
	else {
		FLVTagInfo tagInfo;
		uint32 bytesPerSample, tagIndex, tagDataSize, tagDataSkip;
		sint64 desiredStreamOffset, tagStreamOffset;

		bytesPerSample = mData.mAudioFormat.wfx.nBlockAlign;
		desiredStreamOffset = lStart64 * bytesPerSample;
		tagIndex = (uint32)(((double)desiredStreamOffset / mData.mAudioTotalBytes) * mData.mAudioTags.size());

		while (true) {
			if (tagIndex >= mData.mAudioTags.size()) {
				*lBytesRead = 0;
				*lSamplesRead = 0;
				return true;
			}

			tagInfo = mData.mAudioTags[tagIndex];
			tagDataSize = tagInfo.mDataSize & 0x00FFFFFF;
			tagStreamOffset = mData.mAudioStreamOffsets[tagIndex];

			if (tagStreamOffset > desiredStreamOffset) {
				tagIndex = (tagIndex == 0) ? 0xFFFFFFFF : (tagIndex - 1);
			}
			else if (tagStreamOffset + tagDataSize <= desiredStreamOffset) {
				tagIndex++;
			}
			else {
				tagDataSkip = (uint32)(desiredStreamOffset - tagStreamOffset);
				break;
			}
		}

		readOffset = tagInfo.mDataOffset + tagDataSkip;
		*lSamplesRead = min((tagDataSize - (uint32)tagDataSkip) / bytesPerSample, lCount);
		*lBytesRead = *lSamplesRead * bytesPerSample;
	}

	if (lpBuffer && *lBytesRead) {
		if (cbBuffer < *lBytesRead)
			return false;

		_fseeki64(mData.mFile, readOffset, SEEK_SET);
		fread(lpBuffer, 1, *lBytesRead, mData.mFile);
	}

	return true;
}

const void *AudioSourceFLV::GetDirectFormat() {
	return &mData.mAudioFormat;
}

int AudioSourceFLV::GetDirectFormatLen() {
	return sizeof(mData.mAudioFormat.wfx) + mData.mAudioFormat.wfx.cbSize;
}

IVDXStreamSource::ErrorMode AudioSourceFLV::GetDecodeErrorMode() {
	return IVDXStreamSource::kErrorModeReportAll;
}

void AudioSourceFLV::SetDecodeErrorMode(IVDXStreamSource::ErrorMode mode) {
}

bool AudioSourceFLV::IsDecodeErrorModeSupported(IVDXStreamSource::ErrorMode mode) {
	return mode == IVDXStreamSource::kErrorModeReportAll;
}

bool AudioSourceFLV::IsVBR() {
	return false;
}

sint64 AudioSourceFLV::TimeToPositionVBR(sint64 us) {
	return 0;
}

sint64 AudioSourceFLV::PositionToTimeVBR(sint64 samples) {
	return 0;
}

void AudioSourceFLV::GetAudioSourceInfo(VDXAudioSourceInfo& info) {
}

///////////////////////////////////////////////////////////////////////////////

class InputOptionsFLV : public vdxunknown<IVDXInputOptions> {
public:
	uint32 VDXAPIENTRY Write(void *buf, uint32 buflen);

	FLVInputOptions mOptions;
};

uint32 InputOptionsFLV::Write(void *buf, uint32 buflen) {
	if (buflen >= sizeof(FLVInputOptions)) {
		*((FLVInputOptions*)buf) = mOptions;
	}
	return sizeof(FLVInputOptions);
}

///////////////////////////////////////////////////////////////////////////////

class InputFileFLV : public vdxunknown<IVDXInputFile> {
public:
	InputFileFLV(const VDXInputDriverContext& context);

	void VDXAPIENTRY Init(const wchar_t *szFile, IVDXInputOptions *opts);
	bool VDXAPIENTRY Append(const wchar_t *szFile);

	bool VDXAPIENTRY PromptForOptions(VDXHWND, IVDXInputOptions **);
	bool VDXAPIENTRY CreateOptions(const void *buf, uint32 len, IVDXInputOptions **);
	void VDXAPIENTRY DisplayInfo(VDXHWND hwndParent);

	bool VDXAPIENTRY GetVideoSource(int index, IVDXVideoSource **);
	bool VDXAPIENTRY GetAudioSource(int index, IVDXAudioSource **);

protected:
	static INT_PTR CALLBACK OptionsDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam, LPARAM lParam);

	FLVData	mData;
	const VDXInputDriverContext& mContext;
};

InputFileFLV::InputFileFLV(const VDXInputDriverContext& context)
	: mContext(context)
{
}

void InputFileFLV::Init(const wchar_t *szFile, IVDXInputOptions *opts) {
	FLVInputOptions options;
	sint64 fileSize, filePos, fileRemain;
	uint8* buff = NULL;
	uint32 buffLen;
	uint8* pBuff;
	uint8 flags;
	uint32 dataOffset;
	sint32 width, height;
	uint32 sampleRate, bitsPerSample, channelCount;

	if (opts) {
		options = ((InputOptionsFLV*)opts)->mOptions;
	}

	if (IsWindowsNT()) {
		mData.mFile = _wfopen(szFile, L"rb");
	}
	else {
		char szFileA[1024];
		wcstombs(szFileA, szFile, 1024);
		szFileA[1023] = 0;
		mData.mFile = fopen(szFileA, "rb");
	}
	if (!mData.mFile) {
		mContext.mpCallbacks->SetError("Unable to open file: %ls", szFile);
		goto Finally;
	}

	_fseeki64(mData.mFile, 0, SEEK_END);
	fileSize = _ftelli64(mData.mFile);
	_fseeki64(mData.mFile, 0, SEEK_SET);

	buffLen = 4096;
	buff = new uint8[buffLen];
	mData.mVideoCodec = kVC_None;
	width = 0;
	height = 0;
	mData.mAudioCodec = kAC_None;
	sampleRate = 0;
	bitsPerSample = 0;
	channelCount = 0;

	// Read the main header
	fread(buff, 1, 9, mData.mFile);
	pBuff = buff;
	if (GetInt32(pBuff) != 0x464C5601) { // 'F' 'L' 'V' 0x01
		mContext.mpCallbacks->SetError("This doesn't appear to be a FLV file.");
		goto Finally;
	}
	flags = GetInt8(pBuff);
	dataOffset = GetInt32(pBuff);

	// The remainder of the file is a series of 'tags', each of which holds
	// a video frame or chunk of audio
	filePos = dataOffset + 4;
	_fseeki64(mData.mFile, filePos, SEEK_SET);
	while ((fileRemain = fileSize - filePos) >= 12) {
		uint32 tagType, dataSize, timeStamp, mediaInfo;
		FLVTagInfo tagInfo;

		// Read tag header
		fread(buff, 1, 12, mData.mFile);
		pBuff = buff;
		tagType = GetInt8(pBuff);
		dataSize = GetInt24(pBuff);
		timeStamp = GetInt24(pBuff);
		timeStamp |= GetInt8(pBuff) << 24;
		GetInt24(pBuff);

		if (((tagType == 0x09) || (tagType == 0x08)) && (dataSize != 0)) {
			// This byte is counted in dataSize but isn't part of the video frame
			// or audio data
			mediaInfo = GetInt8(pBuff);

			tagInfo.mDataOffset = filePos + 12;
			tagInfo.mDataSize = dataSize - 1;
			tagInfo.mTimeStamp = timeStamp;

			if ((fileRemain - 12) < tagInfo.mDataSize) {
				// Incomplete file, stop
				break;
			}

			if (tagType == 0x09) { // Video
				sint32 skipSize = 0;

				if (mData.mVideoCodec == kVC_None) {
					switch (mediaInfo & 0x0F) {
						case 2: mData.mVideoCodec = kVC_H263;	 break;
						case 4: mData.mVideoCodec = kVC_VP6;	 break;
						case 5: mData.mVideoCodec = kVC_VP6A;	 break;
						case 3: mData.mVideoCodec = kVC_Screen;	 break;
						case 6: mData.mVideoCodec = kVC_Screen2; break;
					}
				}

				// There is some extra data at the beginning of VP6 frames in FLV
				// that the decoder doesn't want
				if (mData.mVideoCodec == kVC_VP6) {
					skipSize = 1;
				}
				else if (mData.mVideoCodec == kVC_VP6A) {
					skipSize = 4;
				}

				// FLV container doesn't hold width/height, parse frame to find it
				if (((width == 0) || (height == 0)) && (tagInfo.mDataSize > (uint32)skipSize)) {
					uint32 readLen = min(tagInfo.mDataSize, 16);
					fread(buff, 1, readLen, mData.mFile);
					GetFrameSize(buff + skipSize, readLen - skipSize, mData.mVideoCodec, width, height);
				}

				tagInfo.mDataOffset += skipSize;
				tagInfo.mDataSize = max((sint32)tagInfo.mDataSize - skipSize, 0);

				// Mark keyframes
				if ((mediaInfo >> 4) == 1) {
					tagInfo.mDataSize |= 0x01000000;
				}

				mData.mVideoTags.push_back(tagInfo);
			}
			else if (tagType == 0x08) { // Audio
				if (mData.mAudioCodec == kAC_None) {
					switch (mediaInfo >> 4) {
						case 0: mData.mAudioCodec = kAC_PCM;		  break;
						case 1: mData.mAudioCodec = kAC_ADPCM;		  break;
						case 2: mData.mAudioCodec = kAC_MP3;		  break;
						case 5: mData.mAudioCodec = kAC_Nellymoser8M; break;
						case 6: mData.mAudioCodec = kAC_Nellymoser;	  break;
					}
					switch ((mediaInfo >> 2) & 0x03) {
						case 0: sampleRate =  5512; break;
						case 1: sampleRate = 11025; break;
						case 2: sampleRate = 22050; break;
						case 3: sampleRate = 44100; break;
					}
					bitsPerSample = ((mediaInfo >> 1) & 0x01) ? 16 : 8;
					channelCount = (mediaInfo & 0x01) ? 2 : 1;
				}

				if (mData.mAudioCodec == kAC_MP3) {
					if (tagInfo.mDataSize > buffLen) {
						delete[] buff;
						buffLen = tagInfo.mDataSize;
						buff = new uint8[buffLen];
					}
					fread(buff, 1, tagInfo.mDataSize, mData.mFile);
					ParseMP3Frames(buff, tagInfo.mDataSize, mData.mAudioTags.size(), mData.mAudioFrames);
				}

				mData.mAudioTags.push_back(tagInfo);
			}
		}

		filePos += 11 + dataSize + 4;
		_fseeki64(mData.mFile, filePos, SEEK_SET);
	}

	AnalyzeVideoStream(mData.mVideoTags, mData.mVideoTotalBytes);
	ProcessVideoStream(mData.mVideoTags, options, mData.mVideoTimeStampRange, mData.mVideoFrameRate,
		mData.mVideoFrameRateOriginal, mData.mVideoMaxTimeStampError, mData.mVideoMedianTimeStampError,
		mData.mVideoAverageTimeStampError);

	BITMAPINFOHEADER vf;
	memset(&vf, 0, sizeof(vf));
	vf.biSize = sizeof(vf);
	vf.biWidth = width;
	vf.biHeight = height;
	vf.biPlanes = 1;
	vf.biBitCount = 24;
	if (mData.mVideoCodec == kVC_H263) {
		vf.biCompression = kFCC_FLV1;
	}
	else if ((mData.mVideoCodec == kVC_VP6) || (mData.mVideoCodec == kVC_VP6A)) {
		vf.biCompression = kFCC_VP6F;
	}
	vf.biSizeImage = (width * 4) * height;
	vf.biXPelsPerMeter = 0;
	vf.biYPelsPerMeter = 0;
	vf.biClrUsed = 0;
	vf.biClrImportant = 0;

	AnalyzeAudioStream(mData.mAudioTags, mData.mAudioTotalBytes, mData.mAudioMaxFrameSize,
		mData.mAudioStreamOffsets);

	MPEGLAYER3WAVEFORMAT af;
	memset(&af, 0, sizeof(af));
	if (mData.mAudioCodec == kAC_MP3) {
		mData.mAudioVariableSizeSamples = true;
		mData.mAudioSamplesPerFrame = (sampleRate >= 32000) ? 1152 : 576;
		af.wfx.wFormatTag = WAVE_FORMAT_MPEGLAYER3;
		af.wfx.nChannels = channelCount;
		af.wfx.nSamplesPerSec = sampleRate;
		af.wfx.nAvgBytesPerSec = (DWORD)((((double)mData.mAudioTotalBytes * sampleRate) /
			((double)mData.mAudioFrames.size() * mData.mAudioSamplesPerFrame)) + 0.5);
		af.wfx.nBlockAlign = ((mData.mAudioMaxFrameSize + 1151) / 1152) * 1152;
		af.wfx.wBitsPerSample = 0;
		af.wfx.cbSize = MPEGLAYER3_WFX_EXTRA_BYTES;
		af.wID = MPEGLAYER3_ID_MPEG;
		af.fdwFlags = MPEGLAYER3_FLAG_PADDING_ISO;
		af.nBlockSize = mData.mAudioMaxFrameSize;
		af.nFramesPerBlock = 1;
		af.nCodecDelay = 0;
	}
	else if (mData.mAudioCodec == kAC_PCM) {
		mData.mAudioVariableSizeSamples = false;
		af.wfx.wFormatTag = WAVE_FORMAT_PCM;
		af.wfx.nChannels = channelCount;
		af.wfx.nSamplesPerSec = sampleRate;
		af.wfx.nAvgBytesPerSec = channelCount * sampleRate * (bitsPerSample / 8);
		af.wfx.nBlockAlign = channelCount * (bitsPerSample / 8);
		af.wfx.wBitsPerSample = bitsPerSample;
		af.wfx.cbSize = 0;
	}

	mData.mVideoFormat = vf;
	mData.mAudioFormat = af;

Finally:
	if (buff) delete[] buff;
}

bool InputFileFLV::Append(const wchar_t *szFile) {
	return false;
}

bool InputFileFLV::PromptForOptions(VDXHWND hwnd, IVDXInputOptions **ppOptions) {
	InputOptionsFLV *pOptions;

	pOptions = new InputOptionsFLV();
	DialogBoxParam(g_hModule, MAKEINTRESOURCE(IDD_OPTIONS), (HWND)hwnd, OptionsDlgProc, (LPARAM)&(pOptions->mOptions));

	*ppOptions = pOptions;
	pOptions->AddRef();
	return true;
}

INT_PTR APIENTRY InputFileFLV::OptionsDlgProc(HWND hDlg, UINT message, WPARAM wParam, LPARAM lParam) {
	FLVInputOptions *pOptions = (FLVInputOptions*)GetWindowLongPtr(hDlg, DWLP_USER);
	if (message == WM_INITDIALOG) {
		char buff[64];
		int item;
		SetWindowLongPtr(hDlg, DWLP_USER, lParam);
		pOptions = (FLVInputOptions*)lParam;
		sprintf(buff, "%g", pOptions->mTargetFrameRate);
		SetDlgItemText(hDlg, IDC_TARGETFRAMERATE, buff);
		switch (pOptions->mFrameRateConversion) {
			case kFRC_ToTarget:	 item = IDC_FRCTOTARGET;   break;
			case kFRC_None:		 item = IDC_FRCNONE;	   break;
			default:			 item = IDC_FRCAUTOMATIC;
		}
		CheckRadioButton(hDlg, IDC_FRCNONE, IDC_FRCTOTARGET, IDC_FRCAUTOMATIC);
	}
	else if (message == WM_COMMAND) {
		WORD controlID = LOWORD(wParam);
		WORD code = HIWORD(wParam);
		if ((controlID == IDOK) || (controlID == IDCANCEL)) {
			if (LOWORD(wParam) == IDOK) {
				if (IsDlgButtonChecked(hDlg, IDC_FRCTOTARGET) == BST_CHECKED) {
					char buff[64];
					GetDlgItemText(hDlg, IDC_TARGETFRAMERATE, buff, 64);
					pOptions->mFrameRateConversion = kFRC_ToTarget;
					pOptions->mTargetFrameRate = atof(buff);
				}
				else if (IsDlgButtonChecked(hDlg, IDC_FRCNONE) == BST_CHECKED) {
					pOptions->mFrameRateConversion = kFRC_None;
				}
				else {
					pOptions->mFrameRateConversion = kFRC_Automatic;
				}
			}
			EndDialog(hDlg, TRUE);
			return TRUE;
		}
		else if ((controlID == IDC_TARGETFRAMERATE) && (code == EN_UPDATE)) {
			if (IsDlgButtonChecked(hDlg, IDC_FRCTOTARGET) != BST_CHECKED) {
				CheckRadioButton(hDlg, IDC_FRCNONE, IDC_FRCTOTARGET, IDC_FRCTOTARGET);
			}
		}
	}
	return FALSE;
}

bool InputFileFLV::CreateOptions(const void *buf, uint32 len, IVDXInputOptions **ppOptions) {
	InputOptionsFLV *pOptions;

	if (len < sizeof(FLVInputOptions)) {
		return false;
	}

	pOptions = new InputOptionsFLV();
	pOptions->mOptions = *((FLVInputOptions*)buf);
	if (pOptions->mOptions.mOptionsVersion != 1) {
		pOptions->mOptions = FLVInputOptions();
	}

	*ppOptions = pOptions;
	pOptions->AddRef();
	return true;
}

void InputFileFLV::DisplayInfo(VDXHWND hwndParent) {
	char fileInfo[512];
	double videoDuration = 0.0;
	double audioDuration = 0.0;
	char videoCodec[32];
	char audioCodec[32];

	if (mData.mVideoFrameRate > 0.0) {
		videoDuration = mData.mVideoTags.size() / mData.mVideoFrameRate;
	}

	if (mData.mAudioCodec == kAC_MP3) {
		audioDuration = (double)mData.mAudioSamplesPerFrame * mData.mAudioFrames.size() / mData.mAudioFormat.wfx.nSamplesPerSec;
	}
	else if (mData.mAudioCodec == kAC_PCM) {
		audioDuration = (double)(mData.mAudioTotalBytes / mData.mAudioFormat.wfx.nBlockAlign) / mData.mAudioFormat.wfx.nSamplesPerSec;
	}

	switch (mData.mVideoCodec) {
		case kVC_H263:			strcpy(videoCodec, "H.263"); break;
		case kVC_VP6:			strcpy(videoCodec, "VP6"); break;
		case kVC_VP6A:			strcpy(videoCodec, "VP6 with alpha"); break;
		case kVC_Screen:		strcpy(videoCodec, "Screen video"); break;
		case kVC_Screen2:		strcpy(videoCodec, "Screen video 2"); break;
		default:				strcpy(videoCodec, "");
	}

	switch (mData.mAudioCodec) {
		case kAC_PCM:			strcpy(audioCodec, "PCM"); break;
		case kAC_ADPCM:			strcpy(audioCodec, "ADPCM"); break;
		case kAC_MP3:			strcpy(audioCodec, "MP3"); break;
		case kAC_Nellymoser:
		case kAC_Nellymoser8M:	strcpy(audioCodec, "Nellymoser"); break;
		default:				strcpy(audioCodec, "");
	}

	sprintf(fileInfo,
		"Video\r\n\r\n"
		"Codec: %s\r\n"
		"Resolution: %dx%d\r\n"
		"FPS: %.4f (%.4f actual)\r\n"
		"Bitrate: %.0f kbit/s\r\n\r\n"
		"Audio\r\n\r\n"
		"Codec: %s\r\n"
		"Sample rate: %d Hz\r\n"
		"Channels: %d\r\n"
		"Bitrate: %.0f kbit/s\r\n\r\n"
		"Synch Error\r\n\r\n"
		"Median: %d ms\r\n"
		"Average: %d ms\r\n"
		"Maximum: %d ms",
		videoCodec,
		mData.mVideoFormat.biWidth, mData.mVideoFormat.biHeight,
		mData.mVideoFrameRate, mData.mVideoFrameRateOriginal,
		(double)mData.mVideoTotalBytes / (1000 / 8) / videoDuration,
		audioCodec,
		mData.mAudioFormat.wfx.nSamplesPerSec,
		mData.mAudioFormat.wfx.nChannels,
		(double)mData.mAudioTotalBytes / (1000 / 8) / audioDuration,
		-mData.mVideoMedianTimeStampError,
		-mData.mVideoAverageTimeStampError,
		-mData.mVideoMaxTimeStampError
	);

	MessageBox((HWND)hwndParent, fileInfo, "File Information", MB_OK | MB_ICONINFORMATION);
}

bool InputFileFLV::GetVideoSource(int index, IVDXVideoSource **ppVS) {
	*ppVS = NULL;

	if (index)
		return false;

	if (mData.mVideoTags.size() == 0)
		return false;

	if ((mData.mVideoCodec != kVC_H263) &&
		(mData.mVideoCodec != kVC_VP6) &&
		(mData.mVideoCodec != kVC_VP6A))
	{
		return false;
	}

	IVDXVideoSource *pVS = new VideoSourceFLV(mData);
	if (!pVS)
		return false;

	*ppVS = pVS;
	pVS->AddRef();
	return true;
}

bool InputFileFLV::GetAudioSource(int index, IVDXAudioSource **ppAS) {
	*ppAS = NULL;

	if (index)
		return false;

	if (mData.mAudioTags.size() == 0)
		return false;

	if ((mData.mAudioCodec != kAC_MP3) &&
		(mData.mAudioCodec != kAC_PCM))
	{
		return false;
	}

	IVDXAudioSource *pAS = new AudioSourceFLV(mData);
	if (!pAS)
		return false;

	*ppAS = pAS;
	pAS->AddRef();
	return true;
}

///////////////////////////////////////////////////////////////////////////////

class InputFileDriverFLV : public vdxunknown<IVDXInputFileDriver> {
public:
	InputFileDriverFLV(const VDXInputDriverContext& context);
	~InputFileDriverFLV();

	int		VDXAPIENTRY DetectBySignature(const void *pHeader, sint32 nHeaderSize, const void *pFooter, sint32 nFooterSize, sint64 nFileSize);
	bool	VDXAPIENTRY CreateInputFile(uint32 flags, IVDXInputFile **ppFile);

protected:
	const VDXInputDriverContext& mContext;
};

InputFileDriverFLV::InputFileDriverFLV(const VDXInputDriverContext& context)
	: mContext(context)
{
}

InputFileDriverFLV::~InputFileDriverFLV() {
}

int VDXAPIENTRY InputFileDriverFLV::DetectBySignature(const void *pHeader, sint32 nHeaderSize, const void *pFooter, sint32 nFooterSize, sint64 nFileSize) {
	return -1;
}

bool VDXAPIENTRY InputFileDriverFLV::CreateInputFile(uint32 flags, IVDXInputFile **ppFile) {
	IVDXInputFile *p = new InputFileFLV(mContext);
	if (!p)
		return false;

	*ppFile = p;
	p->AddRef();
	return true;
}

///////////////////////////////////////////////////////////////////////////////

bool VDXAPIENTRY flv_create(const VDXInputDriverContext *pContext, IVDXInputFileDriver **ppDriver) {
	IVDXInputFileDriver *p = new InputFileDriverFLV(*pContext);
	if (!p)
		return false;
	*ppDriver = p;
	p->AddRef();
	return true;
}

const uint8 flv_sig[] = {
	'F' , 0xFF,
	'L' , 0xFF,
	'V' , 0xFF,
	0x01, 0xFF,
};

const VDXInputDriverDefinition flv_input={
	sizeof(VDXInputDriverDefinition),
	VDXInputDriverDefinition::kFlagSupportsVideo | VDXInputDriverDefinition::kFlagSupportsAudio,
	0,
	sizeof flv_sig,
	flv_sig,
	L"*.flv",
	L"Flash Video (*.flv)|*.flv",
	L"Flash Video input driver",
	flv_create
};

const VDXPluginInfo flv_plugin={
	sizeof(VDXPluginInfo),
	L"Flash Video input driver",
	L"Moitah",
	L"Loads Flash Video files.",
	0x01010000,
	kVDXPluginType_Input,
	0,
	kVDXPlugin_APIVersion,
	kVDXPlugin_APIVersion,
	kVDXPlugin_InputDriverAPIVersion,
	kVDXPlugin_InputDriverAPIVersion,
	&flv_input
};

///////////////////////////////////////////////////////////////////////////

const VDXPluginInfo *const kPlugins[]={
	&flv_plugin,
	NULL
};

extern "C" __declspec(dllexport) const VDXPluginInfo *const * VDXAPIENTRY VDGetPluginInfo() {
	return kPlugins;
}
