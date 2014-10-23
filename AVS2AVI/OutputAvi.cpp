#include "stdafx.h"
#include ".\outputavi.h"

OutputAvi::OutputAvi(char *path, DWORD length, DWORD rate, DWORD scale, DWORD fourCC, DWORD quality, BITMAPINFOHEADER *bih)
{
	AVISTREAMINFO streamInfo;

	memset(&streamInfo, 0, sizeof(AVISTREAMINFO));

	streamInfo.fccType               = streamtypeVIDEO;
	streamInfo.fccHandler            = fourCC;
	streamInfo.dwQuality             = quality;
	streamInfo.dwScale               = scale;
	streamInfo.dwRate                = rate;
	streamInfo.dwLength              = length;
	streamInfo.dwSuggestedBufferSize = 0;

	streamInfo.rcFrame.left = 0;
	streamInfo.rcFrame.top = 0;
	streamInfo.rcFrame.right = bih->biWidth;
	streamInfo.rcFrame.bottom = abs(bih->biHeight);

	Init(path, &streamInfo, bih);
}

OutputAvi::OutputAvi(char *path, AVISTREAMINFO *streamInfo, BITMAPINFOHEADER *bih)
{
	Init(path, streamInfo, bih);
}

void OutputAvi::Init(char *path, AVISTREAMINFO *streamInfo, BITMAPINFOHEADER *bih)
{
	aviout = new AVIOutputFile();
	aviout->initOutputStreams();

	AVIStreamHeader_fixed *pOutSI = &aviout->videoOut->streamInfo;

	pOutSI->fccType               = streamInfo->fccType;
	pOutSI->fccHandler            = streamInfo->fccHandler;
	pOutSI->dwFlags               = streamInfo->dwFlags;
	pOutSI->wPriority             = streamInfo->wPriority;
	pOutSI->wLanguage             = streamInfo->wLanguage;
	pOutSI->dwInitialFrames       = streamInfo->dwInitialFrames;
	pOutSI->dwScale               = streamInfo->dwScale;	
	pOutSI->dwRate                = streamInfo->dwRate;
	pOutSI->dwStart               = streamInfo->dwStart;
	pOutSI->dwLength              = streamInfo->dwLength;
	pOutSI->dwSuggestedBufferSize = streamInfo->dwSuggestedBufferSize;
	pOutSI->dwQuality             = streamInfo->dwQuality;
	pOutSI->dwSampleSize          = streamInfo->dwSampleSize;
	pOutSI->rcFrame.left          = (SHORT)streamInfo->rcFrame.left;
	pOutSI->rcFrame.top           = (SHORT)streamInfo->rcFrame.top;
	pOutSI->rcFrame.right         = (SHORT)streamInfo->rcFrame.right;
	pOutSI->rcFrame.bottom        = (SHORT)streamInfo->rcFrame.bottom;

	aviout->videoOut->setCompressed(TRUE);

	aviout->videoOut->allocFormat(bih->biSize);
	memcpy(aviout->videoOut->getFormat(), bih, bih->biSize);

	aviout->disable_os_caching();

	aviout->init(path, streamInfo->rcFrame.right, streamInfo->rcFrame.bottom,
		TRUE, FALSE, FALSE, (512 * 1024), FALSE);
}

OutputAvi::~OutputAvi(void)
{
	aviout->finalize();

	delete aviout;
}

void OutputAvi::writeData(void* data, int dataSize, bool keyframe)
{
	aviout->videoOut->write(keyframe ? AVIIF_KEYFRAME : 0, (char *)data, dataSize, 1);
}
