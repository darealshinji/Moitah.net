#pragma once
#include "outputformat.h"
#include "virtualdub\AVIOutput.h"

class OutputAvi :
	public OutputFormat
{
private:
	AVIOutputFile* aviout;
	void Init(char *path, AVISTREAMINFO *streamInfo, BITMAPINFOHEADER *bih);

public:
	OutputAvi(char *path, DWORD length, DWORD rate, DWORD scale, DWORD fourCC, DWORD quality, BITMAPINFOHEADER *bih);
	OutputAvi(char *path, AVISTREAMINFO *streamInfo, BITMAPINFOHEADER *bih);
	void writeData(void* data, int dataSize, bool keyframe);
	~OutputAvi(void);
};
