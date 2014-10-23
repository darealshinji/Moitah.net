#include ".\outputlog.h"

OutputLog::OutputLog(char* filename)
{
	outFile = fopen(filename,"w");
}

void OutputLog::writeData(void* data, int dataSize, bool keyframe)
{
	fprintf(outFile,"%d %d\n",keyframe, dataSize);
}

OutputLog::~OutputLog(void)
{
	fclose(outFile);
}
