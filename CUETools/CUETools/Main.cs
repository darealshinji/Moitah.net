// ****************************************************************************
// 
// CUE Tools
// Copyright (C) 2006-2007  Moitah (moitah@yahoo.com)
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
using System.Text;
using System.IO;

namespace JDP {
	static class General {
		public static int TimeFromString(string s) {
			string[] n = s.Split(':');
			if (n.Length != 3) {
				throw new Exception("Invalid timestamp.");
			}
			int min, sec, frame;

			min = Int32.Parse(n[0]);
			sec = Int32.Parse(n[1]);
			frame = Int32.Parse(n[2]);

			return frame + (sec * 75) + (min * 60 * 75);
		}

		public static string TimeToString(int t) {
			int min, sec, frame;

			frame = t % 75;
			t /= 75;
			sec = t % 60;
			t /= 60;
			min = t;

			return String.Format("{0:00}:{1:00}:{2:00}", min, sec, frame);
		}

		public static CUELine FindCUELine(List<CUELine> list, string command) {
			command = command.ToUpper();
			foreach (CUELine line in list) {
				if (line.Params[0].ToUpper() == command) {
					return line;
				}
			}
			return null;
		}
	}

	delegate void SetStatus(string status);

	enum CUEStyle {
		SingleFile,
		GapsPrepended,
		GapsAppended,
		GapsLeftOut
	}

	class CUESheet {
		private bool _stop;
		private List<CUELine> _attributes;
		private List<TrackInfo> _tracks;
		private List<SourceInfo> _sources;
		private List<string> _sourcePaths, _trackFilenames;
		private string _htoaFilename, _singleFilename;
		private bool _hasHTOAFilename, _hasTrackFilenames, _hasSingleFilename, _appliedWriteOffset;
		private bool _paddedToFrame, _usePregapForFirstTrackInSingleFile, _preserveHTOA;
		private int _writeOffset, _flacCompressionLevel, _wvCompressionMode, _wvExtraMode;
		private bool _flacVerify;

		public CUESheet(string path, bool autoCorrectFilenames) {
			string cueDir, lineStr, command, pathAudio, fileType;
			CUELine line;
			TrackInfo trackInfo;
			int tempTimeLength, timeRelativeToFileStart, absoluteFileStartTime;
			int fileTimeLengthSamples, fileTimeLengthFrames, i, trackNumber;
			bool seenFirstFileIndex, seenDataTrack;
			List<IndexInfo> indexes;
			IndexInfo indexInfo;
			SourceInfo sourceInfo;

			_stop = false;
			_attributes = new List<CUELine>();
			_tracks = new List<TrackInfo>();
			_sources = new List<SourceInfo>();
			_sourcePaths = new List<string>();
			_paddedToFrame = false;
			_usePregapForFirstTrackInSingleFile = false;
			_preserveHTOA = false;
			_flacCompressionLevel = 5;
			_flacVerify = false;
			_wvCompressionMode = 1;
			_wvExtraMode = 0;
			_appliedWriteOffset = false;
			cueDir = Path.GetDirectoryName(path);
			pathAudio = null;
			indexes = new List<IndexInfo>();
			trackInfo = null;
			absoluteFileStartTime = 0;
			fileTimeLengthSamples = 0;
			fileTimeLengthFrames = 0;
			trackNumber = 0;
			seenFirstFileIndex = false;
			seenDataTrack = false;

			if (autoCorrectFilenames) {
				CorrectAudioFilenames(path, false);
			}

			using (StreamReader sr = new StreamReader(path, CUESheet.Encoding)) {
				while ((lineStr = sr.ReadLine()) != null) {
					line = new CUELine(lineStr);
					if (line.Params.Count > 0) {
						command = line.Params[0].ToUpper();

						if (command == "FILE") {
							fileType = line.Params[2].ToUpper();
							if ((fileType == "BINARY") || (fileType == "MOTOROLA")) {
								seenDataTrack = true;
							}
							else if (seenDataTrack) {
								throw new Exception("Audio tracks cannot appear after data tracks.");
							}
							else {
								pathAudio = LocateFile(cueDir, line.Params[1]);
								if (pathAudio == null) {
									throw new Exception("Unable to locate file \"" + line.Params[1] + "\".");
								}
								_sourcePaths.Add(pathAudio);
								absoluteFileStartTime += fileTimeLengthFrames;
								fileTimeLengthSamples = GetSampleLength(pathAudio);
								fileTimeLengthFrames = (int)((fileTimeLengthSamples + 587) / 588);
								seenFirstFileIndex = false;
							}
						}
						else if (command == "TRACK") {
							if (line.Params[2].ToUpper() != "AUDIO") {
								seenDataTrack = true;
							}
							else if (seenDataTrack) {
								throw new Exception("Audio tracks cannot appear after data tracks.");
							}
							else {
								trackNumber = Int32.Parse(line.Params[1]);
								if (trackNumber != _tracks.Count + 1) {
									throw new Exception("Invalid track number.");
								}
								trackInfo = new TrackInfo();
								_tracks.Add(trackInfo);
							}
						}
						else if (seenDataTrack) {
							// Ignore lines belonging to data tracks
						}
						else if (command == "INDEX") {
							timeRelativeToFileStart = General.TimeFromString(line.Params[2]);
							if (!seenFirstFileIndex) {
								if (timeRelativeToFileStart != 0) {
									throw new Exception("First index must start at file beginning.");
								}
								seenFirstFileIndex = true;
								sourceInfo.Path = pathAudio;
								sourceInfo.Offset = 0;
								sourceInfo.Length = fileTimeLengthSamples;
								_sources.Add(sourceInfo);
								if ((fileTimeLengthSamples % 588) != 0) {
									sourceInfo.Path = null;
									sourceInfo.Offset = 0;
									sourceInfo.Length = (fileTimeLengthFrames * 588) - fileTimeLengthSamples;
									_sources.Add(sourceInfo);
									_paddedToFrame = true;
								}
							}
							indexInfo.Track = trackNumber;
							indexInfo.Index = Int32.Parse(line.Params[1]);
							indexInfo.Time = absoluteFileStartTime + timeRelativeToFileStart;
							indexes.Add(indexInfo);
						}
						else if (command == "PREGAP") {
							if (seenFirstFileIndex) {
								throw new Exception("Pregap must occur at the beginning of a file.");
							}
							tempTimeLength = General.TimeFromString(line.Params[1]);
							indexInfo.Track = trackNumber;
							indexInfo.Index = 0;
							indexInfo.Time = absoluteFileStartTime;
							indexes.Add(indexInfo);
							sourceInfo.Path = null;
							sourceInfo.Offset = 0;
							sourceInfo.Length = tempTimeLength * 588;
							_sources.Add(sourceInfo);
							absoluteFileStartTime += tempTimeLength;
						}
						else if (command == "POSTGAP") {
							throw new Exception("POSTGAP command isn't supported.");
						}
						else if ((command == "REM") &&
							(line.Params.Count >= 3) &&
							(line.Params[1].Length >= 10) &&
							(line.Params[1].Substring(0, 10).ToUpper() == "REPLAYGAIN"))
						{
							// Remove ReplayGain lines
						}
						else {
							if (trackInfo != null) {
								trackInfo.Attributes.Add(line);
							}
							else {
								_attributes.Add(line);
							}
						}
					}
				}
			}

			if (trackNumber == 0) {
				throw new Exception("File must contain at least one audio track.");
			}

			// Add dummy track for calculation purposes
			indexInfo.Track = trackNumber + 1;
			indexInfo.Index = 1;
			indexInfo.Time = absoluteFileStartTime + fileTimeLengthFrames;
			indexes.Add(indexInfo);

			// Calculate the length of each index
			for (i = 0; i < indexes.Count - 1; i++) {
				indexInfo = indexes[i];

				tempTimeLength = indexes[i + 1].Time - indexInfo.Time;
				if (tempTimeLength > 0) {
					_tracks[indexInfo.Track - 1].AddIndex((indexInfo.Index == 0), tempTimeLength);
				}
				else if (tempTimeLength < 0) {
					throw new Exception("Indexes must be in chronological order.");
				}
			}

			for (i = 0; i < TrackCount; i++) {
				if (_tracks[i].LastIndex < 1) {
					throw new Exception("Track must have an INDEX 01.");
				}
			}

			// Store the audio filenames, generating generic names if necessary
			_hasSingleFilename = (_sourcePaths.Count == 1);
			_singleFilename = _hasSingleFilename ? Path.GetFileName(_sourcePaths[0]) :
				"Range.wav";

			_hasHTOAFilename = (_sourcePaths.Count == (TrackCount + 1));
			_htoaFilename = _hasHTOAFilename ? Path.GetFileName(_sourcePaths[0]) : "01.00.wav";

			_hasTrackFilenames = (_sourcePaths.Count == TrackCount) || _hasHTOAFilename;
			_trackFilenames = new List<string>();
			for (i = 0; i < TrackCount; i++) {
				_trackFilenames.Add( _hasTrackFilenames ? Path.GetFileName(
					_sourcePaths[i + (_hasHTOAFilename ? 1 : 0)]) : String.Format("{0:00}.wav", i + 1) );
			}
		}

		public static Encoding Encoding {
			get {
				return Encoding.GetEncoding(1252);
			}
		}

		private static string LocateFile(string dir, string file) {
			List<string> dirList, fileList;
			string altDir, path;

			dirList = new List<string>();
			fileList = new List<string>();
			altDir = Path.GetDirectoryName(file);
			file = Path.GetFileName(file);

			dirList.Add(dir);
			if (altDir.Length != 0) {
				dirList.Add(Path.IsPathRooted(altDir) ? altDir : Path.Combine(dir, altDir));
			}

			fileList.Add(file);
			fileList.Add(file.Replace(' ', '_'));
			fileList.Add(file.Replace('_', ' '));

			for (int iDir = 0; iDir < dirList.Count; iDir++) {
				for (int iFile = 0; iFile < fileList.Count; iFile++) {
					path = Path.Combine(dirList[iDir], fileList[iFile]);

					if (File.Exists(path)) {
						return path;
					}
				}
			}

			return null;
		}

		private int GetSampleLength(string path) {
			IAudioSource audioSource;

			audioSource = AudioReadWrite.GetAudioSource(path);
			audioSource.Close();

			if ((audioSource.BitsPerSample != 16) ||
				(audioSource.ChannelCount != 2) ||
				(audioSource.SampleRate != 44100) ||
				(audioSource.Length > Int32.MaxValue))
			{
				throw new Exception("Audio format is invalid.");
			}

			return (int)audioSource.Length;
		}

		public void Write(string path, CUEStyle style) {
			int i, iTrack, iIndex, timeRelativeToFileStart;
			TrackInfo track;
			bool htoaToFile = ((style == CUEStyle.GapsAppended) && _preserveHTOA &&
				(_tracks[0].IndexLengths[0] != 0));

			timeRelativeToFileStart = 0;

			using (StreamWriter sw = new StreamWriter(path, false, CUESheet.Encoding)) {
				for (i = 0; i < _attributes.Count; i++) {
					WriteLine(sw, 0, _attributes[i]);
				}

				if (style == CUEStyle.SingleFile) {
					WriteLine(sw, 0, String.Format("FILE \"{0}\" WAVE", _singleFilename));
				}
				if (htoaToFile) {
					WriteLine(sw, 0, String.Format("FILE \"{0}\" WAVE", _htoaFilename));
				}

				for (iTrack = 0; iTrack < TrackCount; iTrack++) {
					track = _tracks[iTrack];

					if ((style == CUEStyle.GapsPrepended) ||
						(style == CUEStyle.GapsLeftOut) ||
						((style == CUEStyle.GapsAppended) &&
						((track.IndexLengths[0] == 0) || ((iTrack == 0) && !htoaToFile))) )
					{
						WriteLine(sw, 0, String.Format("FILE \"{0}\" WAVE", _trackFilenames[iTrack]));
						timeRelativeToFileStart = 0;
					}

					WriteLine(sw, 1, String.Format("TRACK {0:00} AUDIO", iTrack + 1));
					for (i = 0; i < track.Attributes.Count; i++) {
						WriteLine(sw, 2, track.Attributes[i]);
					}

					for (iIndex = 0; iIndex <= track.LastIndex; iIndex++) {
						if (track.IndexLengths[iIndex] != 0) {
							if ((iIndex == 0) &&
								((style == CUEStyle.GapsLeftOut) ||
								((style == CUEStyle.GapsAppended) && (iTrack == 0) && !htoaToFile) ||
								((style == CUEStyle.SingleFile) && (iTrack == 0) && _usePregapForFirstTrackInSingleFile)))
							{
								WriteLine(sw, 2, "PREGAP " + General.TimeToString(track.IndexLengths[iIndex]));
							}
							else {
								WriteLine(sw, 2, String.Format( "INDEX {0:00} {1}", iIndex,
									General.TimeToString(timeRelativeToFileStart) ));
								timeRelativeToFileStart += track.IndexLengths[iIndex];

								if ((style == CUEStyle.GapsAppended) && (iIndex == 0)) {
									WriteLine(sw, 0, String.Format("FILE \"{0}\" WAVE", _trackFilenames[iTrack]));
									timeRelativeToFileStart = 0;
								}
							}
						}
					}
				}
			}
		}

		public void WriteAudioFiles(string dir, CUEStyle style, SetStatus statusDel) {
			const int buffLen = 16384;
			int iTrack, iIndex, iSource, iDest, i, j, samplesRemIndex, samplesRemSource, copyCount;
			string[] destPaths;
			int[] destLengths;
			byte[] buff = new byte[buffLen * 2 * 2];
			TrackInfo track;
			IAudioSource audioSource = null;
			IAudioDest audioDest = null;
			bool htoaToFile = ((style == CUEStyle.GapsAppended) && _preserveHTOA &&
				(_tracks[0].IndexLengths[0] != 0));
			bool discardOutput;

			if (_usePregapForFirstTrackInSingleFile) {
				throw new Exception("UsePregapForFirstTrackInSingleFile is not supported for writing audio files.");
			}

			if (style == CUEStyle.SingleFile) {
				destPaths = new string[1];
				destPaths[0] = Path.Combine(dir, _singleFilename);
			}
			else {
				destPaths = new string[TrackCount + (htoaToFile ? 1 : 0)];
				if (htoaToFile) {
					destPaths[0] = Path.Combine(dir, _htoaFilename);
				}
				for (i = 0; i < TrackCount; i++) {
					destPaths[i + (htoaToFile ? 1 : 0)] = Path.Combine(dir, _trackFilenames[i]);
				}
			}
			for (i = 0; i < destPaths.Length; i++) {
				for (j = 0; j < _sourcePaths.Count; j++) {
					if (destPaths[i].ToLower() == _sourcePaths[j].ToLower()) {
						throw new Exception("Source and destination audio file paths cannot be the same.");
					}
				}
			}

			if (_writeOffset != 0) {
				int absOffset = Math.Abs(_writeOffset);
				SourceInfo sourceInfo;

				sourceInfo.Path = null;
				sourceInfo.Offset = 0;
				sourceInfo.Length = absOffset;

				if (_writeOffset < 0) {
					_sources.Insert(0, sourceInfo);

					int last = _sources.Count - 1;
					while (absOffset >= _sources[last].Length) {
						absOffset -= _sources[last].Length;
						_sources.RemoveAt(last--);
					}
					sourceInfo = _sources[last];
					sourceInfo.Length -= absOffset;
					_sources[last] = sourceInfo;
				}
				else {
					_sources.Add(sourceInfo);

					while (absOffset >= _sources[0].Length) {
						absOffset -= _sources[0].Length;
						_sources.RemoveAt(0);
					}
					sourceInfo = _sources[0];
					sourceInfo.Offset += absOffset;
					sourceInfo.Length -= absOffset;
					_sources[0] = sourceInfo;
				}

				_appliedWriteOffset = true;
			}

			destLengths = CalculateAudioFileLengths(style);

			iSource = -1;
			iDest = -1;
			samplesRemSource = 0;

			if (style == CUEStyle.SingleFile) {
				iDest++;
				audioDest = GetAudioDest(destPaths[iDest], destLengths[iDest]);
			}

			for (iTrack = 0; iTrack < TrackCount; iTrack++) {
				statusDel(String.Format("Writing track {0:00}...", iTrack + 1));

				track = _tracks[iTrack];

				if ((style == CUEStyle.GapsPrepended) || (style == CUEStyle.GapsLeftOut)) {
					if (audioDest != null) audioDest.Close();
					iDest++;
					audioDest = GetAudioDest(destPaths[iDest], destLengths[iDest]);
				}

				for (iIndex = 0; iIndex <= track.LastIndex; iIndex++) {
					if ((style == CUEStyle.GapsAppended) && (iIndex == 1)) {
						if (audioDest != null) audioDest.Close();
						iDest++;
						audioDest = GetAudioDest(destPaths[iDest], destLengths[iDest]);
					}

					samplesRemIndex = track.IndexLengths[iIndex] * 588;

					if ((style == CUEStyle.GapsAppended) && (iIndex == 0) && (iTrack == 0)) {
						discardOutput = !htoaToFile;
						if (htoaToFile) {
							iDest++;
							audioDest = GetAudioDest(destPaths[iDest], destLengths[iDest]);
						}
					}
					else if ((style == CUEStyle.GapsLeftOut) && (iIndex == 0)) {
						discardOutput = true;
					}
					else {
						discardOutput = false;
					}

					while (samplesRemIndex != 0) {
						if (samplesRemSource == 0) {
							if (audioSource != null) audioSource.Close();
							audioSource = GetAudioSource(++iSource);
							samplesRemSource = _sources[iSource].Length;
						}

						copyCount = Math.Min(Math.Min(samplesRemIndex, samplesRemSource), buffLen);

						audioSource.Read(buff, copyCount);
						if (!discardOutput) audioDest.Write(buff, copyCount);

						samplesRemIndex -= copyCount;
						samplesRemSource -= copyCount;

						lock (this) {
							if (_stop) {
								audioSource.Close();
								try { audioDest.Close(); } catch {}
								throw new StopException();
							}
						}
					}
				}
			}

			if (audioSource != null) audioSource.Close();
			audioDest.Close();
		}

		public static void CorrectAudioFilenames(string path, bool always) {
			string[] audioExts = new string[] { "*.wav", "*.flac", "*.wv" };
			List<string> lines = new List<string>();
			List<int> filePos = new List<int>();
			bool foundAll = true;
			string[] audioFiles = null;
			string lineStr;
			CUELine line;
			string dir;
			int i;

			dir = Path.GetDirectoryName(path);

			using (StreamReader sr = new StreamReader(path, CUESheet.Encoding)) {
				while ((lineStr = sr.ReadLine()) != null) {
					lines.Add(lineStr);
					line = new CUELine(lineStr);
					if ((line.Params.Count == 3) && (line.Params[0].ToUpper() == "FILE")) {
						string fileType = line.Params[2].ToUpper();
						if ((fileType != "BINARY") && (fileType != "MOTOROLA")) {
							filePos.Add(lines.Count - 1);
							foundAll &= (LocateFile(dir, line.Params[1]) != null);
						}
					}
				}
			}

			if (foundAll && !always) {
				return;
			}

			for (i = 0; i < audioExts.Length; i++) {
				audioFiles = Directory.GetFiles(dir, audioExts[i]);
				if (audioFiles.Length == filePos.Count) {
					break;
				}
			}
			if (i == audioExts.Length) {
				throw new Exception("Unable to locate the audio files.");
			}
			Array.Sort(audioFiles);

			for (i = 0; i < filePos.Count; i++) {
				lines[filePos[i]] = "FILE \"" + Path.GetFileName(audioFiles[i]) + "\" WAVE";
			}

			using (StreamWriter sw = new StreamWriter(path, false, CUESheet.Encoding)) {
				for (i = 0; i < lines.Count; i++) {
					sw.WriteLine(lines[i]);
				}
			}
		}

		private int[] CalculateAudioFileLengths(CUEStyle style) {
			int iTrack, iIndex, iFile;
			TrackInfo track;
			int[] fileLengths;
			bool htoaToFile = ((style == CUEStyle.GapsAppended) && _preserveHTOA &&
				(_tracks[0].IndexLengths[0] != 0));
			bool discardOutput;

			if (style == CUEStyle.SingleFile) {
				fileLengths = new int[1];
				iFile = 0;
			}
			else {
				fileLengths = new int[TrackCount + (htoaToFile ? 1 : 0)];
				iFile = -1;
			}

			for (iTrack = 0; iTrack < TrackCount; iTrack++) {
				track = _tracks[iTrack];

				if ((style == CUEStyle.GapsPrepended) || (style == CUEStyle.GapsLeftOut)) {
					iFile++;
				}

				for (iIndex = 0; iIndex <= track.LastIndex; iIndex++) {
					if ((style == CUEStyle.GapsAppended) && (iIndex == 1)) {
						iFile++;
					}

					if ((style == CUEStyle.GapsAppended) && (iIndex == 0) && (iTrack == 0)) {
						discardOutput = !htoaToFile;
						if (htoaToFile) {
							iFile++;
						}
					}
					else if ((style == CUEStyle.GapsLeftOut) && (iIndex == 0)) {
						discardOutput = true;
					}
					else {
						discardOutput = false;
					}

					if (!discardOutput) {
						fileLengths[iFile] += track.IndexLengths[iIndex] * 588;
					}
				}
			}

			return fileLengths;
		}

		public void Stop() {
			lock (this) {
				_stop = true;
			}
		}

		public int TrackCount {
			get {
				return _tracks.Count;
			}
		}

		private IAudioDest GetAudioDest(string path, int finalSampleCount) {
			IAudioDest dest = AudioReadWrite.GetAudioDest(path, 16, 2, 44100, finalSampleCount);

			if (dest is FLACWriter) {
				FLACWriter w = (FLACWriter)dest;
				w.CompressionLevel = _flacCompressionLevel;
				w.Verify = _flacVerify;
			}
			if (dest is WavPackWriter) {
				WavPackWriter w = (WavPackWriter)dest;
				w.CompressionMode = _wvCompressionMode;
				w.ExtraMode = _wvExtraMode;
			}

			return dest;
		}

		private IAudioSource GetAudioSource(int sourceIndex) {
			SourceInfo sourceInfo = _sources[sourceIndex];
			IAudioSource audioSource;

			if (sourceInfo.Path == null) {
				audioSource = new SilenceGenerator(sourceInfo.Offset + sourceInfo.Length);
			}
			else {
				audioSource = AudioReadWrite.GetAudioSource(sourceInfo.Path);
			}

			audioSource.Position = sourceInfo.Offset;

			return audioSource;
		}

		private void WriteLine(StreamWriter sw, int level, CUELine line) {
			WriteLine(sw, level, line.ToString());
		}

		private void WriteLine(StreamWriter sw, int level, string line) {
			sw.Write(new string(' ', level * 2));
			sw.WriteLine(line);
		}

		public List<CUELine> Attributes {
			get {
				return _attributes;
			}
		}

		public List<TrackInfo> Tracks {
			get { 
				return _tracks;
			}
		}

		public bool HasHTOAFilename {
			get {
				return _hasHTOAFilename;
			}
		}

		public string HTOAFilename {
			get {
				return _htoaFilename;
			}
			set {
				_htoaFilename = value;
			}
		}

		public bool HasTrackFilenames {
			get {
				return _hasTrackFilenames;
			}
		}

		public List<string> TrackFilenames {
			get {
				return _trackFilenames;
			}
		}

		public bool HasSingleFilename {
			get {
				return _hasSingleFilename;
			}
		}

		public string SingleFilename {
			get {
				return _singleFilename;
			}
			set {
				_singleFilename = value;
			}
		}

		public string Artist {
			get {
				CUELine line = General.FindCUELine(_attributes, "PERFORMER");
				return (line == null) ? String.Empty : line.Params[1];
			}
		}

		public string Title {
			get {
				CUELine line = General.FindCUELine(_attributes, "TITLE");
				return (line == null) ? String.Empty : line.Params[1];
			}
		}

		public int WriteOffset {
			get {
				return _writeOffset;
			}
			set {
				if (_appliedWriteOffset) {
					throw new Exception("Cannot change write offset after audio files have been written.");
				}
				_writeOffset = value;
			}
		}

		public bool PaddedToFrame {
			get {
				return _paddedToFrame;
			}
		}

		public bool UsePregapForFirstTrackInSingleFile {
			get {
				return _usePregapForFirstTrackInSingleFile;
			}
			set{
				_usePregapForFirstTrackInSingleFile = value;
			}
		}

		public bool PreserveHTOA {
			get {
				return _preserveHTOA;
			}
			set {
				_preserveHTOA = value;
			}
		}

		public int FLACCompressionLevel {
			get {
				return _flacCompressionLevel;
			}
			set {
				_flacCompressionLevel = value;
			}
		}

		public bool FLACVerify {
			get {
				return _flacVerify;
			}
			set {
				_flacVerify = value;
			}
		}

		public int WVCompressionMode {
			get {
				return _wvCompressionMode;
			}
			set {
				_wvCompressionMode = value;
			}
		}

		public int WVExtraMode {
			get {
				return _wvExtraMode;
			}
			set {
				_wvExtraMode = value;
			}
		}
	}

	class CUELine {
		private List<String> _params;
		private List<bool> _quoted;

		public CUELine() {
			_params = new List<string>();
			_quoted = new List<bool>();
		}

		public CUELine(string line) {
			int start, end, lineLen;
			bool isQuoted;

			_params = new List<string>();
			_quoted = new List<bool>();

			start = 0;
			lineLen = line.Length;

			while (true) {
				while ((start < lineLen) && (line[start] == ' ')) {
					start++;
				}
				if (start >= lineLen) {
					break;
				}

				isQuoted = (line[start] == '"');
				if (isQuoted) {
					start++;
				}

				end = line.IndexOf(isQuoted ? '"' : ' ', start);
				if (end == -1) {
					end = lineLen;
				}

				_params.Add(line.Substring(start, end - start));
				_quoted.Add(isQuoted);

				start = isQuoted ? end + 1 : end;
			}
		}

		public List<string> Params {
			get {
				return _params;
			}
		}

		public List<bool> IsQuoted {
			get {
				return _quoted;
			}
		}

		public override string ToString() {
			if (_params.Count != _quoted.Count) {
				throw new Exception("Parameter and IsQuoted lists must match.");
			}

			StringBuilder sb = new StringBuilder();
			int last = _params.Count - 1;

			for (int i = 0; i <= last; i++) {
				if (_quoted[i]) sb.Append('"');
				sb.Append(_params[i]);
				if (_quoted[i]) sb.Append('"');
				if (i < last) sb.Append(' ');
			}

			return sb.ToString();
		}
	}

	class TrackInfo {
		private List<int> _indexLengths;
		private List<CUELine> _attributes;

		public TrackInfo() {
			_indexLengths = new List<int>();
			_attributes = new List<CUELine>();

			_indexLengths.Add(0);
		}

		public int LastIndex {
			get {
				return _indexLengths.Count - 1;
			}
		}

		public List<int> IndexLengths {
			get {
				return _indexLengths;
			}
		}

		public void AddIndex(bool isGap, int length) {
			if (isGap) {
				_indexLengths[0] = length;
			}
			else {
				_indexLengths.Add(length);
			}
		}

		public List<CUELine> Attributes {
			get {
				return _attributes;
			}
		}

		public string Artist {
			get {
				CUELine line = General.FindCUELine(_attributes, "PERFORMER");
				return (line == null) ? String.Empty : line.Params[1];
			}
		}

		public string Title {
			get {
				CUELine line = General.FindCUELine(_attributes, "TITLE");
				return (line == null) ? String.Empty : line.Params[1];
			}
		}
	}

	struct IndexInfo {
		public int Track;
		public int Index;
		public int Time;
	}

	struct SourceInfo {
		public string Path;
		public int Offset;
		public int Length;
	}

	class StopException : Exception {
		public StopException() : base() {
		}
	}

	class SilenceGenerator : IAudioSource {
		private int _sampleOffset, _sampleCount;

		public SilenceGenerator(int sampleCount) {
			_sampleOffset = 0;
			_sampleCount = sampleCount;
		}

		public long Length {
			get {
				return _sampleCount;
			}
		}

		public long Remaining {
			get {
				return _sampleCount - _sampleOffset;
			}
		}

		public long Position {
			get {
				return (long)_sampleOffset;
			}
			set {
				_sampleOffset = (int)value;
			}
		}

		public int BitsPerSample {
			get {
				return 16;
			}
		}

		public int ChannelCount {
			get {
				return 2;
			}
		}

		public int SampleRate {
			get {
				return 44100;
			}
		}

		public int Read(byte[] buff, int sampleCount) {
			int samplesRemaining, byteCount, i;

			samplesRemaining = _sampleCount - _sampleOffset;
			if (sampleCount < 0) {
				sampleCount = 0;
			}
			else if (sampleCount > samplesRemaining) {
				sampleCount = samplesRemaining;
			}

			byteCount = sampleCount * 2 * 2;
			for (i = 0; i < byteCount; i++) {
				buff[i] = 0;
			}

			_sampleOffset += sampleCount;

			return sampleCount;
		}

		public void Close() {
		}
	}
}