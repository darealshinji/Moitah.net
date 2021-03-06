// ****************************************************************************
// 
// MPEG4 Modifier
// Copyright (C) 2004-2007  Moitah (moitah@yahoo.com)
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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;

namespace JDP {
	delegate bool ProgressCallback(double progress);

	class AVIModifier : IVideoModifier {
		private AVIReader _aviReader;
		private AVIWriter _aviWriter;
		private IFrameModifier _frameModifier;
		private ProgressCallback _progressCallback;
		private bool _ranPreview, _wasStopped;
		private bool[] _needsTwoCC;
		private Queue<AVIChunk> _videoChunks, _otherChunks;
		private Queue<int> _otherChunksPerFrame;

		public AVIModifier(string path) {
			_aviReader = new AVIReader(path);
			_ranPreview = false;
			_wasStopped = false;
		}

		public IFrameModifier FrameModifier {
			set {
				_frameModifier = value;
			}
		}

		public ProgressCallback ProgressCallback {
			set {
				_progressCallback = value;
			}
		}

		public bool WasStopped {
			get {
				return _wasStopped;
			}
		}

		public void Close() {
			_aviReader.Close();
		}

		public void Preview() {
			BITMAPINFOHEADER vidHdr = _aviReader.GetVideoStreamFormat(_aviReader.VideoStreamID);
			_frameModifier.SetVideoInfo(vidHdr.biWidth, Math.Abs(vidHdr.biHeight),
				AVIHelper.FourCC(vidHdr.biCompression));
			Run(false);
			_ranPreview = true;
		}

		public void Write(string path) {
			if (!_ranPreview) {
				Preview();
			}

			_aviWriter = new AVIWriter(path);

			// Set up the streams for the new file
			_needsTwoCC = new bool[_aviReader.StreamCount];
			for (int i = 0; i < _aviReader.StreamCount; i++) {
				AVIStreamType streamType = _aviReader.GetStreamType(i);

				_aviWriter.AddStream(streamType);
				_aviWriter.SetStreamHeader(i, _aviReader.GetStreamHeader(i));
				if (streamType == AVIStreamType.Video) {
					_aviWriter.SetStreamFormat(i, _aviReader.GetVideoStreamFormat(i));
				}
				else if (streamType == AVIStreamType.Audio) {
					_aviWriter.SetStreamFormat(i, _aviReader.GetAudioStreamFormat(i));
				}
				_aviWriter.SetStreamFormatExtra(i, _aviReader.GetStreamFormatExtra(i));
				_aviWriter.SetStreamName(i, _aviReader.GetStreamName(i));

				_needsTwoCC[i] = true;
			}

			try {
				Run(true);
			}
			catch {
				_aviWriter.Close();
				File.Delete(path);
				throw;
			}

			_aviWriter.Close();
			if (_wasStopped) {
				File.Delete(path);
			}
		}

		private void Run(bool modify) {
			int progressUpdateInterval = 100;
			int lastProgressUpdate = Environment.TickCount;
			AVIChunk chunk;
			bool readChunk;
			int otherChunkCount;
			int i;

			_videoChunks = new Queue<AVIChunk>();
			otherChunkCount = 0;
			if (modify) {
				_otherChunks = new Queue<AVIChunk>();
				_otherChunksPerFrame = new Queue<int>();
			}
			_aviReader.SeekToStart();

			if (modify) {
				_frameModifier.ModifyStart();
			}
			else {
				_frameModifier.PreviewStart();
			}

			do {
				if (_progressCallback != null) {
					int timeNow = Environment.TickCount;
					if ((timeNow - lastProgressUpdate) >= progressUpdateInterval) {
						_wasStopped = _progressCallback(_aviReader.Progress);
						if (_wasStopped) {
							return;
						}
						lastProgressUpdate = timeNow;
					}
				}

				readChunk = _aviReader.ReadChunk(out chunk.StreamID, out chunk.Data, out chunk.IsKeyFrame, out chunk.Duration);

				if (readChunk) {
					if (chunk.StreamID == _aviReader.VideoStreamID) {
						if (modify) {
							_frameModifier.ModifyFrame(chunk.Data, chunk.IsKeyFrame);
							_otherChunksPerFrame.Enqueue(otherChunkCount);
							otherChunkCount = 0;
						}
						else {
							_frameModifier.PreviewFrame(chunk.Data);
						}
					}
					else {
						if (modify) {
							_otherChunks.Enqueue(chunk);
							otherChunkCount++;
						}
					}
				}
				else {
					if (modify) {
						_frameModifier.ModifyDone();
					}
					else {
						_frameModifier.PreviewDone();
					}
				}

				if (modify) {
					// If there are video chunks waiting to be written, write them along with their
					// corresponding chunks from other streams.  If we're done with the video and there
					// are chunks from other streams remaining, write them out.
					while (((_videoChunks.Count > 0) && (_otherChunksPerFrame.Count > 0)) ||
						(!readChunk && (_otherChunks.Count > 0)))
					{
						int otherWriteCount = (_otherChunksPerFrame.Count > 0) ?
							_otherChunksPerFrame.Dequeue() : _otherChunks.Count;

						for (i = 0; i < otherWriteCount; i++) {
							WriteChunk(_otherChunks.Dequeue());
						}

						if (_videoChunks.Count > 0) {
							WriteChunk(_videoChunks.Dequeue());
						}
					}
				}
			} while (readChunk);

			if (_progressCallback != null) {
				_progressCallback(1.0);
			}
		}

		private void WriteChunk(AVIChunk chunk) {
			int streamID = chunk.StreamID;

			if (_needsTwoCC[streamID]) {
				_aviWriter.SetStreamTwoCC(streamID, _aviReader.GetStreamTwoCC(streamID));
				_needsTwoCC[streamID] = false;
			}

			_aviWriter.WriteChunk(streamID, chunk.Data, chunk.IsKeyFrame, chunk.Duration);
		}

		public void WriteFrame(byte[] data, bool isKeyFrame) {
			AVIChunk chunk;
			chunk.StreamID = _aviReader.VideoStreamID;
			chunk.Data = data;
			chunk.IsKeyFrame = isKeyFrame;
			chunk.Duration = 1;
			_videoChunks.Enqueue(chunk);
		}

		[StructLayout(LayoutKind.Auto)]
		private struct AVIChunk {
			public int StreamID;
			public byte[] Data;
			public bool IsKeyFrame;
			public int Duration;
		}
	}
}