// ****************************************************************************
// 
// MPEG4 Modifier CL
// Copyright (C) 2005-2007  Moitah (moitah@yahoo.com)
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
using System.IO;

namespace JDP {
	class MPEG4ModifierCL {
		private static bool _loading;
		private static string _pathSrc = null;
		private static string _pathDst = null;
		private static bool _alwaysWrite = false;
		private static bool _showInfo = false;
		private static bool _writeFrameList = false;
		private static string _frameListPath = null;
		private static bool _unpack = false;
		private static bool _pack = false;
		private static bool _changePAR = false;
		private static PARType _parType = PARType.VGA_1_1;
		private static double _customPAR = 1.0;
		private static bool _changeDAR = false;
		private static double _customDAR = 1.0;
		private static bool _changeFieldOrder = false;
		private static bool _isTFF = false;

		[STAThread]
		static int Main(string[] args) {
			bool badArgs = false;
			int argCount = args.Length;
			int argIndex = 0;

			Console.WriteLine("MPEG4 Modifier CL v1.4.4a");
			Console.WriteLine("http://www.moitah.net/");
			Console.WriteLine();

			try {
				while (argIndex < argCount) {
					switch (args[argIndex]) {
						case "--always-write":
							_alwaysWrite = true;
							break;
						case "--unpack":
							_unpack = true;
							break;
						case "--pack":
							_pack = true;
							break;
						case "--par":
							_changePAR = true;
							switch (args[++argIndex]) {
								case "square":
									_parType = PARType.VGA_1_1;
									break;
								case "43pal":
									_parType = PARType.PAL_4_3;
									break;
								case "43ntsc":
									_parType = PARType.NTSC_4_3;
									break;
								case "169pal":
									_parType = PARType.PAL_16_9;
									break;
								case "169ntsc":
									_parType = PARType.NTSC_16_9;
									break;
								case "custom":
									string[] customPARStr = args[++argIndex].Split(':');
									if (customPARStr.Length != 2) {
										throw new Exception();
									}
									_parType = PARType.Custom;
									_customPAR = Double.Parse(customPARStr[0]) / Double.Parse(customPARStr[1]);
									break;
								default:
									throw new Exception();
							}
							break;
						case "--dar":
							_changeDAR = true;
							switch (args[++argIndex]) {
								case "custom":
									string[] customDARStr = args[++argIndex].Split(':');
									if (customDARStr.Length != 2) {
										throw new Exception();
									}
									_customDAR = Double.Parse(customDARStr[0]) / Double.Parse(customDARStr[1]);
									break;
								default:
									throw new Exception();
							}
							break;
						case "--field-order":
							_changeFieldOrder = true;
							switch (args[++argIndex]) {
								case "tff":
									_isTFF = true;
									break;
								case "bff":
									_isTFF = false;
									break;
								default:
									throw new Exception();
							}
							break;
						case "--info":
							_showInfo = true;
							break;
						case "--frame-list":
							_writeFrameList = true;
							_frameListPath = args[++argIndex];
							break;
						default:
							goto BreakArgLoop;
					}

					argIndex++;
				}
			}
			catch {
				badArgs = true;
			}
		BreakArgLoop:

			if ((argIndex >= (argCount - 2)) && (argIndex < argCount)) {
				_pathSrc = args[argIndex++];
				if (argIndex < argCount) {
					_pathDst = args[argIndex++];
				}
				else {
					if (_unpack || _pack || _changePAR) {
						badArgs = true;
					}
					if (!_showInfo && !_writeFrameList) {
						badArgs = true;
					}
				}
			}
			else {
				badArgs = true;
			}

			if ((_pack && _unpack) || (_changePAR && _changeDAR)) {
				badArgs = true;
			}

			if (badArgs) {
				Console.WriteLine("Arguments: [switches] source_path [dest_path]");
				Console.WriteLine("  dest_path is only optional with --info or --frame-list");
				Console.WriteLine();
				Console.WriteLine("Switches:");
				Console.WriteLine("  --unpack             Remove packed bitstream.");
				Console.WriteLine("  --pack               Add packed bitstream.");
				Console.WriteLine("  --par <val>          Set the pixel aspect ratio, <val> is:");
				Console.WriteLine("                         square - Square Pixel Shape");
				Console.WriteLine("                         43pal - 4:3 PAL Pixel Shape");
				Console.WriteLine("                         43ntsc - 4:3 NTSC Pixel Shape");
				Console.WriteLine("                         169pal - 16:9 PAL Pixel Shape");
				Console.WriteLine("                         169ntsc - 16:9 NTSC Pixel Shape");
				Console.WriteLine("                         custom <width:height> - Custom Pixel Shape");
				Console.WriteLine("  --dar <val>          Set the display aspect ratio, <val> is:");
				Console.WriteLine("                         custom <width:height> - Custom Display Shape");
				Console.WriteLine("  --field-order <val>  Change the interlaced field order, <val> is:");
				Console.WriteLine("                         tff - Top Field First");
				Console.WriteLine("                         bff - Bottom Field First");
				Console.WriteLine("  --info               Display detailed information about the video.");
				Console.WriteLine("  --frame-list <path>  Write a text file containing each frame's type,");
				Console.WriteLine("                       timestamp, and size to the location specified.");
				Console.WriteLine("  --always-write       Write a new file even if the video format isn't being");
				Console.WriteLine("                       changed (useful for converting OpenDML AVIs < 2GB to ");
				Console.WriteLine("                       standard AVIs).");
				return 1;
			}

			try {
				return Run();
			}
			catch (Exception ex) {
				Console.WriteLine("Error: " + ex.Message);
				return 1;
			}
		}

		private static int Run() {
			if (!File.Exists(_pathSrc)) {
				throw new Exception("Source file doesn't exist.");
			}
			if ((_pathDst != null) && File.Exists(_pathDst)) {
				throw new Exception("Destination file already exists.");
			}

			bool modifying = false;

			_loading = true;
			AVIModifier aviMod = new AVIModifier(_pathSrc);
			MPEG4FrameModifier mp4Mod = new MPEG4FrameModifier();
			aviMod.FrameModifier = mp4Mod;
			mp4Mod.VideoModifier = aviMod;
			aviMod.ProgressCallback = new ProgressCallback(Progress);
			aviMod.Preview();
			Progress(1.0);
			Console.WriteLine();

			if (_showInfo) {
				Console.WriteLine("---------- Video Info ----------");
				Console.Write(mp4Mod.GenerateStats());
				Console.WriteLine("--------------------------------");
			}

			if (_writeFrameList) {
				Console.Write("Writing frame list...");
				try {
					FileStream fs = new FileStream(_frameListPath, FileMode.CreateNew, FileAccess.Write);
					using (StreamWriter sw = new StreamWriter(fs)) {
						mp4Mod.DumpFrameList(sw);
					}
				}
				catch {
					Console.WriteLine();
					throw new Exception("Cannot write frame list, make sure the file doesn't already exist.");
				}
				Console.WriteLine(" Done.");
			}

			if (_pathDst == null) {
				return 0;
			}

			if (_unpack && mp4Mod.IsPacked) {
				modifying = true;
				mp4Mod.NewIsPacked = false;
				mp4Mod.UserDataList = mp4Mod.SuggestedUserData();
			}
			if (_pack && !mp4Mod.IsPacked && mp4Mod.ContainsBVOPs) {
				modifying = true;
				mp4Mod.NewIsPacked = true;
				mp4Mod.UserDataList = mp4Mod.SuggestedUserData();
			}
			if (_changePAR || _changeDAR) {
				MPEG4PAR pold = mp4Mod.PARInfo;
				MPEG4PAR pnew = new MPEG4PAR();

				if (_changePAR) {
					if (_parType != PARType.Custom) {
						pnew.Type = _parType;
					}
					else {
						pnew.SetCustomPAR(_customPAR);
					}
				}
				else if (_changeDAR) {
					double rar = (double)mp4Mod.FrameWidth / (double)mp4Mod.FrameHeight;
					pnew.SetCustomPAR(_customDAR / rar);
				}

				if ((pold.Type != pnew.Type) ||
					((pnew.Type == PARType.Custom) && 
					((pnew.Width != pold.Width) || (pnew.Height != pold.Height))))
				{
					modifying = true;
					mp4Mod.PARInfo = pnew;
				}
			}
			if (_changeFieldOrder && mp4Mod.IsInterlaced && (mp4Mod.TopFieldFirst != _isTFF)) {
				modifying = true;
				mp4Mod.TopFieldFirst = _isTFF;
			}

			if (!modifying && !_alwaysWrite) {
				Console.WriteLine("Aborting: Video already has the desired format.");
				return 2;
			}

			_loading = false;
			aviMod.Write(_pathDst);
			Progress(1.0);
			Console.WriteLine();

			Console.WriteLine("Video was written successfully.");
			return 0;
		}

		private static bool Progress(double progress) {
			if (progress < 0.0) {
				progress = 0.0;
			}
			else if (progress > 1.0) {
				progress = 1.0;
			}

			Console.Write("\r{0}: {1:0.0}%", (_loading ? "Loading" : "Saving"), progress * 100.0);

			return false;
		}
	}
}