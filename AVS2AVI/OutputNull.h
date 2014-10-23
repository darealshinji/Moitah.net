#pragma once
#include "outputformat.h"

class OutputNull :
	public OutputFormat
{
public:
	void writeData(void* data, int dataSize, bool keyframe){}
	OutputNull(void){}

	~OutputNull(void){}
};
