//	VirtualDub - Video processing and capture application
//	Copyright (C) 1998-2001 Avery Lee
//
//	This program is free software; you can redistribute it and/or modify
//	it under the terms of the GNU General Public License as published by
//	the Free Software Foundation; either version 2 of the License, or
//	(at your option) any later version.
//
//	This program is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU General Public License for more details.
//
//	You should have received a copy of the GNU General Public License
//	along with this program; if not, write to the Free Software
//	Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.

#ifndef f_AUDIOSOURCE_H
#define f_AUDIOSOURCE_H

#include <windows.h>
#include <vfw.h>
#include <stdio.h>

#include "DubSource.h"

#include "mp32src1\all.h"
#include "mp32src1\crc.h"
#include "mp32src1\ibitstr.h"
#include "mp32src1\header.h"

#include "vorbis\codec.h"


class IAVIReadHandler;
class IAVIReadStream;

class AudioSource : public DubSource {
public:
	WAVEFORMATEX *getWaveFormat() {
		return (WAVEFORMATEX *)getFormat();
	}
};

class AudioSourceWAV : public AudioSource {
private:
	HMMIO				hmmioFile;
	MMCKINFO			chunkRIFF;
	MMCKINFO			chunkDATA;
	LONG				lCurrentSample;
	LONG				bytesPerSample;

public:
	AudioSourceWAV(char *fn, LONG inputBufferSize);
	~AudioSourceWAV();

	BOOL init();
	virtual int _read(LONG lStart, LONG lCount, LPVOID lpBuffer, LONG cbBuffer, LONG *lSamplesRead, LONG *lBytesRead);
};

class AudioSourceAVI : public AudioSource {
private:
	IAVIReadHandler *pAVIFile;
	IAVIReadStream *pAVIStream;
	int	nStream;
	bool bQuiet;

	BOOL _isKey(LONG lSample);

public:
	AudioSourceAVI(IAVIReadHandler *pAVIFile, int streamno, bool bAutomated);
	~AudioSourceAVI();

	void Reinit();
	bool isStreaming();

	void streamBegin(bool fRealTime);
	void streamEnd();

	BOOL init();
	int _read(LONG lStart, LONG lCount, LPVOID lpBuffer, LONG cbBuffer, LONG *lSamplesRead, LONG *lBytesRead);
};

class AudioSourceMP3 : public AudioSource {
private:
	Header *header;
	Ibitstream *stream;
	FILE *f;
	uint32 pos;

public:
	AudioSourceMP3(char *fn);
	~AudioSourceMP3();

	BOOL init();
	virtual int _read(LONG lStart, LONG lCount, LPVOID lpBuffer, LONG cbBuffer, LONG *lSamplesRead, LONG *lBytesRead);
};

class AudioSourceAC3 : public AudioSource {
private:
	FILE				*ac3File;
	MMCKINFO			chunkRIFF;
	MMCKINFO			chunkDATA;
	LONG				lCurrentSample;
	LONG				bytesPerSample;

public:
	AudioSourceAC3(char *fn, LONG inputBufferSize);
	~AudioSourceAC3();

	BOOL init();
	virtual int _read(LONG lStart, LONG lCount, LPVOID lpBuffer, LONG cbBuffer, LONG *lSamplesRead, LONG *lBytesRead);
};


class AudioSourceOggVorbis : public AudioSource {
private:
	FILE *f;

	ogg_sync_state   oy; /* sync and verify incoming physical bitstream */
	ogg_stream_state os; /* take physical pages, weld into a logical stream of packets */
	ogg_page         og; /* one Ogg bitstream page.  Vorbis packets are inside */
	ogg_packet       op; /* one raw packet of data for decode */
		 
	vorbis_info      vi; /* struct that stores all the static vorbis bitstream
							settings */
	vorbis_comment   vc; /* struct that stores all the bitstream user comments */
	vorbis_dsp_state vd; /* central working state for the packet->PCM decoder */
	vorbis_block     vb; /* local working space for packet->PCM decode */

	int		eos;

	__int64 pos;
	LONG	lCurrentSample;

	int		pcm_samples;
	int		pcm_written;

	BOOL streamInit();
	void readPage();
	void decodePage();

public:
	AudioSourceOggVorbis(char *fn);
	~AudioSourceOggVorbis();

	BOOL init();
	virtual int _read(LONG lStart, LONG lCount, LPVOID lpBuffer, LONG cbBuffer, LONG *lSamplesRead, LONG *lBytesRead);
};


#endif
