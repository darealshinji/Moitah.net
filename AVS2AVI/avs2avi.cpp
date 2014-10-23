/*******************************************************************************
*                                                                              *
* AVS2AVI                                                                      *
* Copyright (C) 2002-2004  Christophe Paris (christophe.paris@free.fr)         *
*                          int21h                                              *
*                          David Leatherdale (dave@leatherdale.net)            *
*                          Moitah (moitah@excite.com)                          *
*                                                                              *
* This program is free software; you can redistribute it and/or modify         *
* it under the terms of the GNU General Public License as published by         *
* the Free Software Foundation; either version 2 of the License, or            *
* (at your option) any later version.                                          *
*                                                                              *
* This program is distributed in the hope that it will be useful,              *
* but WITHOUT ANY WARRANTY; without even the implied warranty of               *
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the                *
* GNU General Public License for more details.                                 *
*                                                                              *
* You should have received a copy of the GNU General Public License            *
* along with this program; if not, write to the Free Software                  *
* Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA    *
*                                                                              *
*******************************************************************************/


#include <conio.h>
#include <shlwapi.h>
#include "stdafx.h"
#include "OutputAvi.h"
#include "OutputLog.h"
#include "OutputNull.h"

//major_v.minor_v for display purposes
#define major_v "1"
#define minor_v "39"

#define IS_FIRST_PASS (current_pass == 1)
#define IS_LAST_PASS (current_pass == NbPass)
#define NO_RECOMPRESS (CompVars.fccHandler == FCC_NULL)

#define VIDEO_BUFFERS 2

#ifndef ABOVE_NORMAL_PRIORITY_CLASS
#define ABOVE_NORMAL_PRIORITY_CLASS 0x00008000; // Yes you should update your platform SDK.
#endif

struct VideoFrame {
	void *pData;
	int bufferSize;  // Size allocated for the buffer
	int dataSize;    // Size actually used by the frame data
	bool isKeyFrame;
};

enum OutputFormatID {
	OUT_NULL,
	OUT_AVI,
	OUT_LOG
};

enum FourCCs {
	FCC_NULL = 0x00000000,
	FCC_DIB  = 0x20424944  // 'DIB '
};

enum WindowWaitMode {
	WW_OFF,
	WW_ALL,
	WW_LAST
};

int outputType = OUT_AVI;
OutputFormat* out = NULL;
HINSTANCE g_hInst; // vdubstuff

PAVIFILE pSRCFile = NULL;
PAVISTREAM pSRCVideoStream = NULL;
AVISTREAMINFO SRCStreamInfo;
BITMAPINFOHEADER bmihVideoSource;

COMPVARS CompVars;
COMPVARS FirstCompVars;
COMPVARS SecondCompVars;
VideoFrame SRCFrame[VIDEO_BUFFERS];

char SourcePath[MAX_PATH] = "";
char DestPath[MAX_PATH] = "";
char StatePath[MAX_PATH] = "";
const char* FourCC = NULL;
BOOL FrontendDisplay = FALSE;
BOOL OverwriteDest = FALSE;
BOOL SaveState = FALSE;
BOOL LoadState = FALSE;
BOOL Quiet = FALSE;
BOOL UseFourCC = FALSE;
BOOL Stop = FALSE;
BOOL InstantStats = TRUE;
BOOL SkipEncode = FALSE;
int WindowWait = WW_OFF;

HANDLE hEventWinCreated, hMutexStats, hSemWaitRead, hSemWaitWrite, hEventWriteFinished;
HWND hHiddenWin;

int NbPass = 1;
int current_pass = 1;
int statsSwitchSecs = 60;
int statsUpdatesPerSec = 4;
unsigned int FrameRead, FrameWritten, KFCount;
__int64 dstTotalDataSize;
__int64 perfFreq, passStartTime, priStatsStartTime, priStatsLastTime, secStatsStartTime, switchStatsTime;
int priStatsStartFrame, priStatsLastFrame, secStatsStartFrame, waitToUpdateStats, updateStatsInterval;

// ----------------------------------------------------------------------------

void InitializeSource(char* filename);
void CloseSource();
void LoadSourceInfo();
void DisplaySourceInfo();
void InitializeDestination(char* filename);
void CloseDestination();
void DisplayDestinationInfo();
void ChooseOneCompatibleCompressor(COMPVARS *cvar);
void ChooseCompressorOnFourCC(const char* FourCC);
void ChooseCompatibleCompressor();
void FreeCompressors();
void DoPass();
void ReadThread();
void WriteThread();
void StatsThread();
void CleanupAndExit(const int returnCode);
void ShowLastWindowsErrorAndExit(const char *pFormat,...);
void DisplayEndStats();
void ShowErrorAndExit(const char* errorMessage, ...);
void LoadCodecParams(const char* filename);
void LoadOneCodecParam(FILE* fileState, COMPVARS* cvar);
void SaveCodecParams(const char* filename);
void SaveOneCodecParam(FILE* fileState, COMPVARS* cvar);
const char* FourCCToString(const DWORD fourCC);
char* TimeMSToString(const int timeMS, char* buff, BOOL showMS);

// ----------------------------------------------------------------------------

void disp_u(const char* text, ...)
{
	if (FrontendDisplay) {
		return;
	}

	va_list vArgs;
	va_start(vArgs, text);
	vprintf(text, vArgs);
	va_end(vArgs);
}

// ----------------------------------------------------------------------------

void disp_f(const char* text, ...)
{
	if (!FrontendDisplay) {
		return;
	}

	int buffLen = (int)strlen(text) + 16;
	char *buff = new char[buffLen];

	sprintf(buff, "%s\n", text);

	va_list vArgs;
	va_start(vArgs, text);
	vprintf(buff, vArgs);
	va_end(vArgs);

	delete[] buff;
}

// ----------------------------------------------------------------------------

LRESULT CALLBACK WndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam)
{
	switch (message) {
		case WM_CLOSE:
			Stop = TRUE;
			break;
		case WM_DESTROY:
			PostQuitMessage(0);
			return 0;
	}

	return DefWindowProc(hWnd, message, wParam, lParam);
}

// ----------------------------------------------------------------------------

void HiddenWindow()
{
	HINSTANCE hInst;
	char winClassName[] = "AVS2AVI";
	char winTitle[] = "AVS2AVI";
	WNDCLASS winClassInfo;
	MSG msg;
	BOOL retGM;

	hInst = (HINSTANCE)GetModuleHandle(NULL);

	ZeroMemory(&winClassInfo, sizeof(WNDCLASS));
	winClassInfo.lpfnWndProc = (WNDPROC)WndProc;
	winClassInfo.hInstance = hInst;
	winClassInfo.hCursor = LoadCursor(NULL, IDC_ARROW);
	winClassInfo.hbrBackground = (HBRUSH)(COLOR_BTNFACE + 1);
	winClassInfo.lpszClassName = winClassName;

	RegisterClass(&winClassInfo);

	hHiddenWin = CreateWindow(winClassName, winTitle, WS_OVERLAPPEDWINDOW,
		0, 0, 200, 200, NULL, NULL, hInst, NULL);
	SetEvent(hEventWinCreated);
	if (!hHiddenWin) {
		return;
	}

	while (true) {
		retGM = GetMessage(&msg, NULL, 0, 0);
		if (!retGM || (retGM == -1)) {
			return;
		}

		DispatchMessage(&msg);
	}
}

// ----------------------------------------------------------------------------

void PassWindowMessages()
{
	MSG msg;

	while (PeekMessage(&msg, NULL, 0, 0, PM_REMOVE)) {
		DispatchMessage(&msg);
	}
}

// ----------------------------------------------------------------------------

BOOL CALLBACK CountThreadWindowsCB(HWND hwnd, LPARAM lParam)
{
	int *pWinCount = (int *)lParam;
	(*pWinCount)++;
	return TRUE;
}

// ----------------------------------------------------------------------------

int CountThreadWindows()
{
	int winCount = 0;
	EnumThreadWindows(GetCurrentThreadId(), CountThreadWindowsCB, (LPARAM)&winCount);
	return winCount;
}

// ----------------------------------------------------------------------------

void Usage()
{
	//      1234567890123456789012345678901234567890123456789012345678901234567890123456789
	disp_u("   Usage: avs2avi avs_filename [destination_filename] [switches]\n\n");
	disp_u("   destination_filename is relative to avs_filename, if omitted it will be\n");
	disp_u("   the same as avs_filename but with the proper extension.\n\n");
	disp_u("   Switches:\n");
	disp_u("      -w          : Overwrite destination file if it already exists\n");
	disp_u("      -P <passes> : Enable multi-pass encoding mode\n");
	disp_u("      -p [0-2]    : Priority (0: Idle, 1: Normal, 2: Above Normal)\n");
	disp_u("      -s <file>   : Save codec parameters to <file>\n");
	disp_u("      -l <file>   : Load codec parameters from <file>\n");
	disp_u("      -c <4cc>    : Use codec <4cc> with default settings (\"null\" for no\n");
	disp_u("                    recompression)\n");
	disp_u("      -e          : Exit after codec selection (for use with -s)\n");
	disp_u("      -q          : Enable quiet mode for more speed\n");
	disp_u("      -o [format] : Output format (a: AVI (default), l: Log, n: Null)\n");
	disp_u("      -x [a,l]    : Wait until XviD status window is closed (a: for all passes,\n");
	disp_u("                    l: only for the last pass)\n");
}

// ----------------------------------------------------------------------------

void UpdateCurrentLine(char* line)
{
	const int max_text = 79;
	static char buf[max_text + 1];
	int i = 0;

	while (i < max_text) {
		if ((buf[i] = line[i]) == '\0') break;
		i++;
	}
	while (i < max_text) {
		buf[i++] = ' ';
	}
	buf[max_text] = '\0';

	disp_u("\r%s", buf);
}

// ----------------------------------------------------------------------------

void EraseCurrentLine()
{
	disp_u("\r                                                                               \r");
}

// ----------------------------------------------------------------------------

#define NEXT_ARG (++i < argc)
#define IS_SWITCH (argv[i][0] == '-')
#define ARG_LEN strlen(argv[i])

BOOL ParseCommandLine(int argc, char* argv[])
{
	int i = 0;

	// Get the source path
	if (!NEXT_ARG) return FALSE;
	if (IS_SWITCH) return FALSE;
	strcpy(SourcePath, argv[i]);

	// Get the dest path
	if (NEXT_ARG) {
		if (!IS_SWITCH) strcpy(DestPath, argv[i]);
		else i--;
	}

	// Parse the switches
	while (NEXT_ARG) {
		if ((ARG_LEN != 2) || !IS_SWITCH) {
			return FALSE;
		}

		switch (argv[i][1]) {
			case 'f':
				FrontendDisplay = TRUE;
				break;
			case 'q':
				Quiet = TRUE;
				break;
			case 'w':
				OverwriteDest = TRUE;
				break;
			case 'e':
				SkipEncode = TRUE;
				break;
			case '2':
				NbPass = 2;
				break;
			case 'l':
				if (!NEXT_ARG) return FALSE;
				if (SaveState) return FALSE;
				strcpy(StatePath, argv[i]);
				LoadState = TRUE;
				break;
			case 's':
				if (!NEXT_ARG) return FALSE;
				if (LoadState) return FALSE;
				strcpy(StatePath, argv[i]);
				SaveState = TRUE;
				break;
			case 'P':
				if (!NEXT_ARG) return FALSE;
				NbPass = atoi(argv[i]);
				if (NbPass <= 0) {
					return FALSE;
				}
				break;
			case 'c':
				if (!NEXT_ARG) return FALSE;
				FourCC = argv[i];
				UseFourCC = TRUE;
				break;
			case 'p': {
				if (!NEXT_ARG || (ARG_LEN != 1)) return FALSE;
				DWORD priorityClass;
				switch (argv[i][0]) {
					case '0':
						priorityClass = IDLE_PRIORITY_CLASS;
						break;
					case '1':
						priorityClass = NORMAL_PRIORITY_CLASS;
						break;
					case '2':
						priorityClass = ABOVE_NORMAL_PRIORITY_CLASS;
						break;
					default:
						return FALSE;
				}
				SetPriorityClass(GetCurrentProcess(), priorityClass);
				break;
			}
			case 'o':
				if (!NEXT_ARG || (ARG_LEN != 1)) return FALSE;
				switch (argv[i][0]) {
					case 'a':
						outputType = OUT_AVI;
						break;
					case 'l':
						outputType = OUT_LOG;
						break;
					case 'n':
						outputType = OUT_NULL;
						DestPath[0] = '\0';
						break;
					default:
						return FALSE;
				}
				break;
			case 'x':
				if (!NEXT_ARG || (ARG_LEN != 1)) return FALSE;
				switch (argv[i][0]) {
					case 'a':
						WindowWait = WW_ALL;
						break;
					case 'l':
						WindowWait = WW_LAST;
						break;
					default:
						return FALSE;
				}
				break;
			default:
				return FALSE;
		}
	}

	return TRUE;
}

#undef NEXT_ARG
#undef IS_SWITCH
#undef ARG_LEN

// ----------------------------------------------------------------------------

void GetExtensionForFormat(char* buff, int format)
{
	switch (format) {
		case (OUT_AVI):
			strcpy(buff, ".avi");
			break;
		case (OUT_LOG):
			strcpy(buff, ".txt");
			break;
		default:
			buff[0] = '\0';
	}
}

// ----------------------------------------------------------------------------

BOOL PathMakeAbsolute(char* path)
{
	char buff[MAX_PATH];
	int len;

	len = GetFullPathName(path, MAX_PATH, buff, NULL);
	if (!len || (len > MAX_PATH)) {
		return FALSE;
	}
	strcpy(path, buff);

	return TRUE;
}

// ----------------------------------------------------------------------------

BOOL PathAddExtensionAlways(char* path, const char* ext)
{
	int pathLen, extLen;

	pathLen = (int)strlen(path);
	extLen = (int)strlen(ext);
	if ((pathLen + extLen) >= MAX_PATH) {
		return FALSE;
	}
	strcpy( &(path[pathLen]) , ext);

	return TRUE;
}

// ----------------------------------------------------------------------------

BOOL CheckOutputFile(const char* path, BOOL overwrite)
{
	BOOL ret = FALSE;

	if (!PathFileExists(path)) {
		// File not found, good
		ret = TRUE;
	}
	else {
		if (overwrite) {
			// Open with CREATE_ALWAYS to empty the file
			HANDLE hFile = CreateFile(path, GENERIC_READ | GENERIC_WRITE,
				FILE_SHARE_READ, NULL, CREATE_ALWAYS, 0, NULL);
			if (hFile != INVALID_HANDLE_VALUE) {
				ret = TRUE;
				CloseHandle(hFile);
			}
		}
	}

	return ret;
}

// ----------------------------------------------------------------------------

void DoPathMagic()
{
	char sourceDir[MAX_PATH], properExt[16];

	// Do source path
	if (!PathMakeAbsolute(SourcePath))
		ShowErrorAndExit("There was a problem with the source path.");
	if (!PathFileExists(SourcePath))
		ShowErrorAndExit("Unable to find the source file.");

	// Change the current directory to that of the source file
	strcpy(sourceDir, SourcePath);
	if (!PathRemoveFileSpec(sourceDir))
		ShowErrorAndExit("There was a problem with the source path.");
	if (!SetCurrentDirectory(sourceDir))
		ShowErrorAndExit("Unable to set the current directory.");

	// Do destination path
	if ((outputType != OUT_NULL) && !SkipEncode) {
		GetExtensionForFormat(properExt, outputType);
		if (strlen(DestPath)) {
			if (!PathMakeAbsolute(DestPath))
				ShowErrorAndExit("There was a problem with the destination path.");

			// If the current extension isn't correct, add it
			if (strcmp(PathFindExtension(DestPath), properExt)) {
				if (!PathAddExtensionAlways(DestPath, properExt))
					ShowErrorAndExit("There was a problem with the destination path.");
			}
		}
		else {
			// No dest path specified, use the same as the source with proper extension
			strcpy(DestPath, SourcePath);
			if (!PathRenameExtension(DestPath, properExt))
				ShowErrorAndExit("There was a problem with the destination path.");
		}
		if (!CheckOutputFile(DestPath, OverwriteDest))
			ShowErrorAndExit("Destination file already exists (or unable to overwrite).");
	}

	// Do state path
	if (strlen(StatePath)) {
		if (!PathMakeAbsolute(StatePath))
			ShowErrorAndExit("There was a problem with the state path.");
		
		if (LoadState) {
			if (!PathFileExists(StatePath))
				ShowErrorAndExit("Unable to find the state file.");
		}
		else {
			if (!CheckOutputFile(StatePath, TRUE))
				ShowErrorAndExit("Unable to overwrite the state file.");
		}
	}
}

// ----------------------------------------------------------------------------

void ShowLastWindowsErrorAndExit(const char *pFormat,...)
{
	char ContextMsg[512];

	// Format the variable length parameter list
	va_list va;
	va_start(va, pFormat);
	vsprintf(ContextMsg, pFormat, va);
	va_end(va);

	// Get windows error msg
	LPVOID lpErrorMsg;
	FormatMessage(
		FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM,
		NULL,
		GetLastError(),
		MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),          // Default language
		(LPTSTR) &lpErrorMsg,
		0,
		NULL
		);

	// Show complete msg
	disp_u("\n%s : %s\n", ContextMsg, lpErrorMsg);
	disp_f("ERROR :%s", ContextMsg);

	// Free the buffer.
	LocalFree( lpErrorMsg );

	// Exit
	CleanupAndExit(1);
}

// ----------------------------------------------------------------------------

const char* FourCCToString(const DWORD fourCC)
{
	static char buff[5] = {0,0,0,0,0};
	char* fcc = (char*)&fourCC;
	for(int i=0; i < 4; i++)
		buff[i] = fcc[i];

	return buff;
}

// ----------------------------------------------------------------------------

char* TimeMSToString(const int timeMS, char* buff, BOOL showMS)
{
	int hh,mm,ss, ms;
	ms = timeMS % 1000;
	ss = timeMS / 1000;
	mm = (ss / 60) % 60;
	hh = ss / 3600;
	ss = ss % 60;

	if (showMS)
		sprintf(buff,"%02d:%02d:%02d.%03d", hh, mm, ss, ms);
	else
		sprintf(buff,"%02d:%02d:%02d", hh, mm, ss);

	return buff;
}

// ----------------------------------------------------------------------------

#define sizeKB 1024
#define sizeMB 1048576

char* SizeToString(__int64 byteSize, char* buff, int buffLen)
{
	double displaySize;
	const char* displayString;

	if (byteSize >= sizeMB) {
		displaySize = (double)byteSize / sizeMB;
		displayString = "%.2f MB";
	}
	else if (byteSize >= sizeKB) {
		displaySize = (double)byteSize / sizeKB;
		displayString = "%.2f KB";
	}
	else {
		displaySize = (double)byteSize;
		displayString = "%.0f B";
	}

	_snprintf(buff, buffLen, displayString, displaySize);
	return buff;
}

#undef sizeKB
#undef sizeMB

// ----------------------------------------------------------------------------

__int64 FileSizeByPath(const char* filePath)
{
	HANDLE hFile;
	LARGE_INTEGER fileSize;

	hFile = CreateFile(filePath, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE,
		NULL, OPEN_EXISTING, 0, NULL);
	if (hFile == INVALID_HANDLE_VALUE) {
		return -1;
	}

	fileSize.LowPart = GetFileSize(hFile, (DWORD*)&fileSize.HighPart);
	if ((fileSize.LowPart == INVALID_FILE_SIZE) && (GetLastError() != NO_ERROR)) {
		fileSize.QuadPart = -1;
	}

	CloseHandle(hFile);

	return fileSize.QuadPart;
}

// ----------------------------------------------------------------------------

void OnCtrlC(int signal)
{
	Stop = TRUE;
}

// ----------------------------------------------------------------------------

int main(int argc, char* argv[])
{
	HANDLE hThreadHiddenWin;

	BOOL validCmdLine = ParseCommandLine(argc,argv);

	if (FrontendDisplay) {
		// Disable buffering for stdout
		setvbuf(stdout, NULL, _IONBF, 0);

		// Create the hidden window
		DWORD threadID;
		hEventWinCreated = CreateEvent(NULL, FALSE, FALSE, NULL);
		hThreadHiddenWin = CreateThread(NULL, 0, (LPTHREAD_START_ROUTINE)HiddenWindow,
			NULL, 0, &threadID);
		WaitForSingleObject(hEventWinCreated, INFINITE);
		CloseHandle(hEventWinCreated);
	}

	disp_u("\nAVS2AVI v%s.%s (c) 2002-2004:\n",major_v,minor_v);
	disp_u("Christophe Paris, David Leatherdale, int21h, Moitah\n");
	disp_u("http://www.avs2avi.org/\n\n");

	if(!validCmdLine) {
		Usage();
		return 1;
	}

	// Install Ctrl-C handler
	signal(SIGINT, OnCtrlC);

	disp_f("VERSION 1 1 :%s.%s", major_v, minor_v);
	disp_f("INIT %d", hHiddenWin);

	DoPathMagic();

	LoadSourceInfo();
	DisplaySourceInfo();

	// Choose compressor(s)
	if (LoadState) {
		LoadCodecParams(StatePath);
	}
	else if (UseFourCC) {
		ChooseCompressorOnFourCC(FourCC);
	}
	else {
		ChooseCompatibleCompressor();
	}

	if (SaveState) {
		SaveCodecParams(StatePath);
	}

	if (!SkipEncode) {
		DisplayDestinationInfo();
		disp_f("ENC_START %d %d", NbPass, SRCStreamInfo.dwLength);

		// Main loop
		for (current_pass = 1; (current_pass <=  NbPass) && !Stop; current_pass++) {
			if (current_pass == 1) {
				CompVars = FirstCompVars;
			}
			else if (current_pass == 2) {
				CompVars = SecondCompVars;
			}

			DoPass();
		}
	}

	FreeCompressors();

	if (!SkipEncode) {
		DisplayEndStats();
	}

	if (FrontendDisplay) {
		if (hHiddenWin) {
			// Wait for hidden window to finish up
			SendMessage(hHiddenWin, WM_CLOSE, 0, 0);
			WaitForSingleObject(hThreadHiddenWin, INFINITE);
		}
		CloseHandle(hThreadHiddenWin);
	}

	exit(0);
}

// ----------------------------------------------------------------------------

void CleanupAndExit(const int returnCode)
{
	FreeCompressors();
	CloseDestination();
	CloseSource();

	exit(returnCode);
}

// ----------------------------------------------------------------------------

void ShowErrorAndExit(const char* errorMessage, ...)
{
	char buff[256];

	va_list vArgs;
	va_start(vArgs, errorMessage);
	_vsnprintf(buff, 256, errorMessage, vArgs);
	va_end(vArgs);

	disp_u("\nError: %s\n", buff);
	disp_f("ERROR :%s", buff);

	CleanupAndExit(1);
}

// ----------------------------------------------------------------------------

void ShowCompressorInfo(COMPVARS *cvar)
{
	if (cvar->fccHandler == FCC_NULL) {
		disp_u("  * No Recompression\n");
	}
	else {
		char codecName[128];
		ICINFO icinfo;

		// Show compressor name and FourCC
		ZeroMemory(&icinfo,sizeof(ICINFO));
		icinfo.dwSize = sizeof(ICINFO);
		if(ICGetInfo(cvar->hic, &icinfo, sizeof(ICINFO)))
		{
			WideCharToMultiByte(CP_ACP, 0, icinfo.szDescription, -1, codecName, 128, 0, 0);
			disp_u("  * Name: %s\n", codecName);
			disp_u("  * FourCC: %s\n", FourCCToString(icinfo.fccHandler));
		}
	}
}

// ----------------------------------------------------------------------------

void ChooseOneCompatibleCompressor(COMPVARS *cvar)
{
	// Choose a compatible compressor
	ZeroMemory(cvar,sizeof(COMPVARS));
	cvar->cbSize = sizeof(COMPVARS);
	if(ICCompressorChoose(NULL, ICMF_CHOOSE_DATARATE | ICMF_CHOOSE_KEYFRAME,
		&bmihVideoSource, NULL, cvar, NULL) == FALSE)
	{
		ShowErrorAndExit("Compressor selection aborted.");
	}

	if (cvar->fccHandler == FCC_DIB) {
		// "Full Frames (Uncompressed)"
		ShowErrorAndExit("Valid compressor not chosen.");
	}
	else if (SaveState && (cvar->fccHandler == FCC_NULL)) {
		// "No Recompression"
		ShowErrorAndExit("No Recompression isn't allowed in codec parameters file.");
	}
}

// ----------------------------------------------------------------------------

void ChooseCompatibleCompressor()
{
	// Choose a compatible compressor
	ChooseOneCompatibleCompressor(&FirstCompVars);

	disp_u("Compressor:\n");
	ShowCompressorInfo(&FirstCompVars);

	if (NbPass > 1) {
		// Choose a compatible compressor for second pass
		ChooseOneCompatibleCompressor(&SecondCompVars);

		// Check both compressor are the same
		if(FirstCompVars.fccHandler != SecondCompVars.fccHandler) {
			disp_u("Second Compressor:\n");
			ShowCompressorInfo(&SecondCompVars);
			ShowErrorAndExit("First and second compressors don't match!");
		}
	}
}

// ----------------------------------------------------------------------------

void ChooseOneCompressorOnFourCC(const char* FourCC, COMPVARS *cvar)
{
	ZeroMemory(cvar, sizeof(COMPVARS));
	cvar->cbSize = sizeof(COMPVARS);
	cvar->dwFlags = ICMF_COMPVARS_VALID;
	cvar->fccType = ICTYPE_VIDEO;
	if (stricmp(FourCC, "null") == 0) {
		// No Recompression
		cvar->fccHandler = FCC_NULL;
		cvar->hic = NULL;
	}
	else {
		cvar->fccHandler = *(DWORD*)FourCC;
		cvar->hic = ICOpen(cvar->fccType, cvar->fccHandler, ICMODE_COMPRESS);
		if(cvar->hic == NULL) {
			ShowErrorAndExit("There is no codec with FourCC \"%s\"!", FourCC);
		}
	}
	cvar->lKey = 15;
	cvar->lQ = ICQUALITY_DEFAULT;
}

// ----------------------------------------------------------------------------

void ChooseCompressorOnFourCC(const char* FourCC)
{
	ChooseOneCompressorOnFourCC(FourCC, &FirstCompVars);

	disp_u("Compressor:\n");
	ShowCompressorInfo(&FirstCompVars);

	if (NbPass > 1) {
		ChooseOneCompressorOnFourCC(FourCC, &SecondCompVars);
	}
}

// ----------------------------------------------------------------------------

void FreeCompressors()
{
	ICCompressorFree(&FirstCompVars);
	if (NbPass > 1) {
		ICCompressorFree(&SecondCompVars);
	}
}

// ----------------------------------------------------------------------------

void InitializeSource(char* filename)
{
	AVIFileInit();

	// Open AVS file
	if (AVIFileOpen(&pSRCFile, filename, OF_SHARE_DENY_WRITE, 0) != AVIERR_OK) {
		ShowLastWindowsErrorAndExit("AVIFileOpen failed, unable to open \"%s\"", filename);
	}

	// Get the video stream
	if (AVIFileGetStream(pSRCFile, &pSRCVideoStream, streamtypeVIDEO, 0) != AVIERR_OK) {
		ShowLastWindowsErrorAndExit("AVIFileGetStream failed, unable to get video stream 0");
	}

	// Read the BITMAPINFOHEADER for the first frame
	LONG cbBMI = sizeof(BITMAPINFOHEADER);
	ZeroMemory(&bmihVideoSource, cbBMI);
	bmihVideoSource.biSize = cbBMI;
	if(AVIStreamReadFormat(pSRCVideoStream, 0, &bmihVideoSource, &cbBMI) != AVIERR_OK) {
		ShowLastWindowsErrorAndExit("AVIStreamReadFormat failed");
	}

	ZeroMemory(&SRCStreamInfo, sizeof(AVISTREAMINFO));
	if(AVIStreamInfo(pSRCVideoStream, &SRCStreamInfo, sizeof(AVISTREAMINFO)) != AVIERR_OK) {
		ShowLastWindowsErrorAndExit("AVIStreamInfo failed");
	}
}

// ----------------------------------------------------------------------------

void CloseSource()
{
	if(pSRCVideoStream) {
		AVIStreamRelease(pSRCVideoStream);
		pSRCVideoStream = NULL;
	}
	if(pSRCFile) {
		AVIFileRelease(pSRCFile);
		pSRCFile = NULL;
	}
	AVIFileExit();
}

// ----------------------------------------------------------------------------

void LoadSourceInfo()
{
	InitializeSource((char*)SourcePath);
	CloseSource();
}

// ----------------------------------------------------------------------------

void DisplaySourceInfo()
{
	disp_u("Source:\n");
	disp_u("  * Filename: \"%s\"\n", SourcePath);

	switch(bmihVideoSource.biCompression) {
		case mmioFOURCC( 'Y','U','Y','2'):
		case mmioFOURCC( 'Y','V','1','2'):
			disp_u("  * FourCC: %s\n", FourCCToString(bmihVideoSource.biCompression));
			break;
		case BI_RGB:
			disp_u("  * FourCC: None (RGB%d)\n", bmihVideoSource.biBitCount);
			break;
		default: {
			disp_u("  * FourCC: %s (Not tested)\n", FourCCToString(bmihVideoSource.biCompression));
		}
	}

	disp_u("  * Frames: %d\n", SRCStreamInfo.dwLength);
	disp_u("  * Resolution: %dx%d\n", SRCStreamInfo.rcFrame.right, SRCStreamInfo.rcFrame.bottom);
	disp_u("  * Frame rate: %.3f FPS\n", (float)SRCStreamInfo.dwRate / (float)SRCStreamInfo.dwScale);
}

// ----------------------------------------------------------------------------

void InitializeDestination(char* filename)
{
	if(!IS_LAST_PASS || (outputType == OUT_NULL))
	{
		out = new OutputNull();
	} 
	else
	{
		switch (outputType)
		{
			case OUT_AVI: {
				if (NO_RECOMPRESS) {
					out = new OutputAvi(filename, &SRCStreamInfo, &bmihVideoSource);
				}
				else {
					int formatSize = ICCompressGetFormatSize(CompVars.hic, (void*)&bmihVideoSource);
					BITMAPINFOHEADER *bih = (BITMAPINFOHEADER*)malloc(formatSize);

					memset(bih, 0, formatSize);
					ICCompressGetFormat(CompVars.hic, &bmihVideoSource, bih);
					if (bih->biSize == 0) bih->biSize = formatSize;

					out = new OutputAvi(filename, SRCStreamInfo.dwLength, SRCStreamInfo.dwRate,
						SRCStreamInfo.dwScale, CompVars.fccHandler, CompVars.lQ, bih);

					free(bih);
				}
				break;
			}
			case OUT_LOG:
				out = new OutputLog(filename);
				break;
		}
	}

	if (!NO_RECOMPRESS) {
		// Initialize compressor

		ICCOMPRESSFRAMES iccf;
		ZeroMemory(&iccf, sizeof(ICCOMPRESSFRAMES));
		iccf.dwRate = SRCStreamInfo.dwRate;
		iccf.dwScale = SRCStreamInfo.dwScale;
		iccf.lQuality = CompVars.lQ;
		iccf.lDataRate = CompVars.lDataRate<<10;
		iccf.lKeyRate = CompVars.lKey;

		ICSendMessage(CompVars.hic, ICM_COMPRESS_FRAMES_INFO, (DWORD)&iccf, sizeof(ICCOMPRESSFRAMES));
	}
}

// ----------------------------------------------------------------------------

void CloseDestination()
{
	if (out) {
		delete out;
		out = NULL;
	}
}

// ----------------------------------------------------------------------------

void DisplayDestinationInfo()
{
	disp_u("Destination:\n");
	if (outputType != OUT_NULL) {
		disp_u("  * Filename: \"%s\"\n", DestPath);
	}

	switch (outputType) {
		case OUT_AVI:
			// I'm not showing this since it's default...
			//disp_u("  * Format: AVI\n");
			break;
		case OUT_LOG:
			disp_u("  * Format: Log\n");
			break;
		case OUT_NULL:
			disp_u("  * Format: Null\n");
			break;
	}
}

// ----------------------------------------------------------------------------

void LoadCodecParams(const char* filename)
{
	int numPassFile, currentPassFile;
	FILE* fileState = fopen(filename,"rb");

	if (!fileState) {
		ShowErrorAndExit("There was a problem opening \"%s\".", filename);
	}

	// Read number of passes (current pass is ignored, left for backwards compatibility)
	fread(&numPassFile, sizeof(numPassFile), 1, fileState);
	fread(&currentPassFile, sizeof(currentPassFile), 1, fileState);

	if (numPassFile != NbPass) {
		fclose(fileState);
		ShowErrorAndExit("The file you are trying to open is a %d pass file.", numPassFile);
	}

	// Read 1st pass codec info
	LoadOneCodecParam(fileState, &FirstCompVars);
	disp_u("Compressor:\n");
	ShowCompressorInfo(&FirstCompVars);

	// Read 2nd pass codec info
	if(NbPass > 1) {
		LoadOneCodecParam(fileState, &SecondCompVars);
	}

	fclose(fileState);
}

// ----------------------------------------------------------------------------

void LoadOneCodecParam(FILE* fileState, COMPVARS* cvar)
{
	LONG lStateSize;
	void* memState;

	ZeroMemory(cvar, sizeof(COMPVARS));
	cvar->cbSize = sizeof(COMPVARS);
	cvar->dwFlags = ICMF_COMPVARS_VALID;
	cvar->fccType = ICTYPE_VIDEO;

	fread(&cvar->fccHandler, sizeof(cvar->fccHandler), 1, fileState);
	fread(&cvar->lKey,       sizeof(cvar->lKey),       1, fileState);
	fread(&cvar->lQ,         sizeof(cvar->lQ),         1, fileState);
	fread(&cvar->lDataRate,  sizeof(cvar->lDataRate),  1, fileState);
	fread(&lStateSize,       sizeof(lStateSize),       1, fileState);

	memState = malloc(lStateSize);
	fread(memState,          lStateSize,               1, fileState);

	cvar->hic = ICOpen(cvar->fccType, cvar->fccHandler, ICMODE_COMPRESS);
	if ( !(cvar->hic) ) {
		free(memState);
		ShowErrorAndExit("Unable to open compressor with FourCC \"%s\".",
			FourCCToString(cvar->fccHandler));
	}

	// Ignore return (ICSetState sometimes returns 0 even when successful)
	ICSetState(cvar->hic, memState, lStateSize);

	free(memState);
}

// ----------------------------------------------------------------------------

void SaveCodecParams(const char* filename)
{
	FILE* fileState = fopen(filename,"wb");

	if (!fileState) {
		ShowErrorAndExit("There was a problem opening \"%s\".", filename);
	}

	// Write number of passes
	fwrite(&NbPass, sizeof(NbPass), 1, fileState);
	fwrite(&current_pass, sizeof(current_pass), 1, fileState);

	// Write 1st pass codec state
	SaveOneCodecParam(fileState, &FirstCompVars);

	// Write 2nd pass codec state if needed
	if (NbPass > 1) {
		SaveOneCodecParam(fileState, &SecondCompVars);
	}

	fclose(fileState);
}

// ----------------------------------------------------------------------------

void SaveOneCodecParam(FILE* fileState, COMPVARS* cvar)
{
	LONG lStateSize;
	void* memState;

	lStateSize = ICGetStateSize(cvar->hic);
	memState = malloc(lStateSize);
	if (ICGetState(cvar->hic, memState, lStateSize) != ICERR_OK) {
		free(memState);
		ShowErrorAndExit("ICGetState failed");
	}

	fwrite(&cvar->fccHandler, sizeof(cvar->fccHandler), 1, fileState);
	fwrite(&cvar->lKey,       sizeof(cvar->lKey),       1, fileState);
	fwrite(&cvar->lQ,         sizeof(cvar->lQ),         1, fileState);
	fwrite(&cvar->lDataRate,  sizeof(cvar->lDataRate),  1, fileState);
	fwrite(&lStateSize,       sizeof(lStateSize),       1, fileState);
	fwrite(memState,          lStateSize,               1, fileState);

	free(memState);
}

// ----------------------------------------------------------------------------

void ReadThread()
{
	int buffSize;
	int ret;
	int SRCIndex;

	// Calculate initial buffer size
	if (NO_RECOMPRESS) {
		buffSize = SRCStreamInfo.dwSuggestedBufferSize;
		if (buffSize <= 0) {
			buffSize = 256 * 1024;
		}
	}
	else {
		buffSize = bmihVideoSource.biSizeImage;
	}

	// Allocate buffers
	for(SRCIndex = 0; SRCIndex < VIDEO_BUFFERS; SRCIndex++) {
		SRCFrame[SRCIndex].bufferSize = buffSize;
		SRCFrame[SRCIndex].pData = malloc(buffSize);
	}

	for (DWORD frameNumber = 0;
		(frameNumber < SRCStreamInfo.dwLength) && !Stop;
		frameNumber++)
	{
		//Make sure writing is done
		while (WaitForSingleObject(hSemWaitWrite, 250) == WAIT_TIMEOUT) {
			if (Stop) goto stopread;
		}

		// Read source frame
		SRCIndex = frameNumber % VIDEO_BUFFERS;
		do {
			ret = AVIStreamRead(pSRCVideoStream, frameNumber, 1, SRCFrame[SRCIndex].pData,
				SRCFrame[SRCIndex].bufferSize, (LONG *)&SRCFrame[SRCIndex].dataSize, NULL);

			if (ret == AVIERR_BUFFERTOOSMALL) {
				// Reallocate buffer
				free(SRCFrame[SRCIndex].pData);
				AVIStreamSampleSize(pSRCVideoStream, frameNumber, (LONG *)&buffSize);
				buffSize = (buffSize * 3) / 2;
				SRCFrame[SRCIndex].bufferSize = buffSize;
				SRCFrame[SRCIndex].pData = malloc(buffSize);
			}
			else if (ret != AVIERR_OK) {
				ShowLastWindowsErrorAndExit("AVIStreamRead failed in Read thread (error code = 0x%X).", ret);
			}
		} while (ret == AVIERR_BUFFERTOOSMALL);
		if (NO_RECOMPRESS) {
			SRCFrame[SRCIndex].isKeyFrame = AVIStreamIsKeyFrame(pSRCVideoStream, frameNumber);
		}
		FrameRead++;

		// Update stats
		waitToUpdateStats--;
		if (!Quiet && (waitToUpdateStats <= 0)) {
			__int64 currentTime;
			QueryPerformanceCounter((LARGE_INTEGER*)&currentTime);

			WaitForSingleObject(hMutexStats, INFINITE);
			if (InstantStats && ((currentTime - secStatsStartTime) >= switchStatsTime)) {
				// Switch to newer stats
				priStatsStartTime = secStatsStartTime;
				priStatsStartFrame = secStatsStartFrame;

				// Start new secondary stats
				secStatsStartTime = currentTime;
				secStatsStartFrame = FrameRead;
			}
			priStatsLastTime = currentTime;
			priStatsLastFrame = FrameRead;
			ReleaseMutex(hMutexStats);

			waitToUpdateStats = updateStatsInterval;
		}

		// Signal reading is done
		while(!ReleaseSemaphore(hSemWaitRead, 1, NULL)) {
			disp_u("WARNING: ReleaseSemaphore(hSemWaitRead...) failed!\n");
		}
	}
stopread:

	// Free buffers
	WaitForSingleObject(hEventWriteFinished, INFINITE);
	for (SRCIndex = 0; SRCIndex < VIDEO_BUFFERS; SRCIndex++) {
		free(SRCFrame[SRCIndex].pData);
		SRCFrame[SRCIndex].pData = NULL;
	}
}

// ----------------------------------------------------------------------------

void WriteThread()
{
	int SRCIndex;
	void *DSTData;
	BOOL KF_Flag;
	LONG DSTDataSize;
	DWORD BFrameNum = 0;
	DWORD lastFrame = SRCStreamInfo.dwLength - 1;

	InitializeDestination((char*)DestPath);
	if (!NO_RECOMPRESS) {
		if (!ICSeqCompressFrameStart(&CompVars, (LPBITMAPINFO)&bmihVideoSource)) {
			ShowLastWindowsErrorAndExit("ICSeqCompressFrameStart failed");
		}
	}

	for (DWORD frameNumber = 0;
		(frameNumber <= lastFrame + BFrameNum) && !Stop;
		frameNumber++)
	{
		// Make sure reading is done
		if (frameNumber <= lastFrame) {
			while (WaitForSingleObject(hSemWaitRead, 250) == WAIT_TIMEOUT) {
				if (Stop) goto stopwrite;
			}
		}

		SRCIndex = __min(frameNumber, lastFrame) % VIDEO_BUFFERS;
		if (NO_RECOMPRESS) {
			out->writeData(SRCFrame[SRCIndex].pData, SRCFrame[SRCIndex].dataSize, SRCFrame[SRCIndex].isKeyFrame);
			dstTotalDataSize += SRCFrame[SRCIndex].dataSize;
			if (SRCFrame[SRCIndex].isKeyFrame) KFCount++;
			FrameWritten++;
		}
		else {
			DSTData = ICSeqCompressFrame(&CompVars, 0, SRCFrame[SRCIndex].pData, &KF_Flag, &DSTDataSize);
			if (!DSTData) {
				ShowLastWindowsErrorAndExit("ICSeqCompressFrame failed");
			}
			if (KF_Flag) KFCount++;

			// Detect delay frames due to B-frames (DivX 5/XviD)
			if ((DSTDataSize == 1) && (*(byte*)DSTData == 0x7F)) {
				BFrameNum++;
			}
			else {
				out->writeData(DSTData, DSTDataSize, KF_Flag);
				dstTotalDataSize += DSTDataSize;
				FrameWritten++;
			}
		}

		// Signify writing done
		if (frameNumber <= lastFrame) {
			while (!ReleaseSemaphore(hSemWaitWrite, 1, NULL)) {
				disp_u("WARNING: ReleaseSemaphore(hSemWaitWrite...) failed!\n");
			}
		}

		// Pass along window messages (XviD status window...)
		PassWindowMessages();
	}
stopwrite:

	if (!NO_RECOMPRESS) {
		// End of frame sequence compression
		ICSeqCompressFrameEnd(&CompVars);
	}
	CloseDestination();

	SetEvent(hEventWriteFinished);

	if (!Stop && ((WindowWait == WW_ALL) || (IS_LAST_PASS && (WindowWait == WW_LAST)))) {
		// Wait until all windows owned by this thread are closed (XviD status window...)
		while (CountThreadWindows()) {
			PassWindowMessages();
			Sleep(20);
		}
	}
}

// ----------------------------------------------------------------------------

void StatsThread()
{
	unsigned int elapsedTime, remainingTime;
	double fps;
	char timeBuff[16], sizeBuff[16], line[80];
	__int64 passEndTime;

	fps = 0;
	remainingTime = -1;
	timeBuff[0] = '\0';

	disp_f("PASS_START");

	if(Quiet) {
		disp_u("  * Pass %d/%d: Compressing...",current_pass, NbPass);
	}
	while (!Stop && (WaitForSingleObject(hEventWriteFinished, 0) != WAIT_OBJECT_0)) {
		// Display info
		if (!Quiet) {
			WaitForSingleObject(hMutexStats, INFINITE);
			elapsedTime = (unsigned int)(((priStatsLastTime - priStatsStartTime) * 1000) / perfFreq);
			if (elapsedTime > 0) {
				fps = (double)((priStatsLastFrame - priStatsStartFrame) * 1000) / (double)elapsedTime;
				updateStatsInterval = (int)fps / statsUpdatesPerSec;
				if (updateStatsInterval <= 0) updateStatsInterval = 1;
			}
			ReleaseMutex(hMutexStats);

			if (elapsedTime > 0) {
				remainingTime = (unsigned int)( (double)((SRCStreamInfo.dwLength - FrameRead) * 1000) / fps );
				TimeMSToString(remainingTime, timeBuff, FALSE);
			}
			SizeToString(dstTotalDataSize, sizeBuff, 16);

			_snprintf(line, 80, "  * Pass %d/%d: Frame %d/%d, %s, %.2f FPS, ETA %s",
				current_pass, NbPass, FrameRead, SRCStreamInfo.dwLength, sizeBuff,
				fps, timeBuff);
			UpdateCurrentLine(line);

			disp_f("PROGRESS %d %I64d %f %d", FrameRead, dstTotalDataSize, fps, remainingTime);
		}

		Sleep(250);
	}

	QueryPerformanceCounter((LARGE_INTEGER*)&passEndTime);

	// Show overall stats for this pass
	EraseCurrentLine();
	elapsedTime = (unsigned int)(((passEndTime - passStartTime) * 1000) / perfFreq);
	fps = (double)(FrameRead * 1000.0) / (double)elapsedTime;
	disp_u("  * Pass %d/%d: Finished in %s (%.2f FPS)\n", current_pass, NbPass,
		TimeMSToString(elapsedTime,timeBuff,TRUE), fps);

	if (!Stop) {
		disp_f("PASS_END %d %I64d", elapsedTime, dstTotalDataSize);
	}
}

// ----------------------------------------------------------------------------

void DisplayEndStats()
{
	__int64 totalFileSize, displayFileSize;
	char sizeBuff[16];

	totalFileSize = (outputType == OUT_NULL) ? -1 : FileSizeByPath(DestPath);
	displayFileSize = (totalFileSize == -1) ? dstTotalDataSize : totalFileSize;
	SizeToString(displayFileSize, sizeBuff, 16);

	disp_u("  * Frames: %d (%d keyframes)\n", FrameWritten, KFCount);
	disp_u("  * Size: %s\n", sizeBuff);

	if (!Stop) {
		disp_f("ENC_END %I64d %d", displayFileSize, KFCount);
	}
}

// ----------------------------------------------------------------------------

#define THREAD_COUNT 3

void DoPass()
{
	HANDLE TName[THREAD_COUNT];
	DWORD ThreadID;

	InitializeSource((char*)SourcePath);

	hEventWriteFinished = CreateEvent(NULL, TRUE, FALSE, NULL);
	FrameRead = 0;
	FrameWritten = 0;
	KFCount = 0;
	dstTotalDataSize = 0;

	QueryPerformanceFrequency((LARGE_INTEGER*)&perfFreq);
	QueryPerformanceCounter((LARGE_INTEGER*)&passStartTime);
	priStatsStartTime = passStartTime;
	priStatsLastTime = passStartTime;
	secStatsStartTime = passStartTime;
	switchStatsTime = statsSwitchSecs * perfFreq;
	priStatsStartFrame = 0;
	priStatsLastFrame = 0;
	secStatsStartFrame = 0;
	updateStatsInterval = 10;
	waitToUpdateStats = updateStatsInterval;

	hMutexStats = CreateMutex(NULL, FALSE, NULL);
	hSemWaitRead = CreateSemaphore(NULL,0,VIDEO_BUFFERS,NULL);
	hSemWaitWrite = CreateSemaphore(NULL,VIDEO_BUFFERS,VIDEO_BUFFERS,NULL);

	TName[0] = CreateThread(NULL, 0, (LPTHREAD_START_ROUTINE)ReadThread,
		NULL, 0, &ThreadID);
	TName[1]= CreateThread(NULL, 0, (LPTHREAD_START_ROUTINE)WriteThread,
		NULL, 0, &ThreadID);
	TName[2] = CreateThread(NULL, 0, (LPTHREAD_START_ROUTINE)StatsThread,
		NULL, 0, &ThreadID);

	WaitForMultipleObjects(THREAD_COUNT, TName, TRUE, INFINITE);

	for (int i = 0; i < THREAD_COUNT; i++) {
		CloseHandle(TName[i]);
	}
	CloseHandle(hMutexStats);
	CloseHandle(hSemWaitRead);
	CloseHandle(hSemWaitWrite);

	CloseSource();
}

#undef THREAD_COUNT

// ============================================================================
// EOF : avs2avi.cpp
// ============================================================================
