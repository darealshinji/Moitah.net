#pragma once
#include "stdafx.h"
#include "outputformat.h"

class OutputLog :
	public OutputFormat
{
private:
	FILE* outFile;
public:
	OutputLog(char* filename);
	void writeData(void* data, int dataSize, bool keyframe);
	~OutputLog(void);
};
