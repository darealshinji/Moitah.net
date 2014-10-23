#pragma once

class OutputFormat
{
public:
	virtual void writeData(void* data, int dataSize, bool keyframe) {};
	virtual ~OutputFormat() {};
};