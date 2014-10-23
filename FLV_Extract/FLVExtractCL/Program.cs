using System;
using System.IO;

namespace JDP {
	class Program {
		static bool _autoOverwrite;

		static int Main(string[] args) {
			int argCount = args.Length;
			int argIndex = 0;
			bool extractVideo = false;
			bool extractAudio = false;
			bool extractTimeCodes = false;
			string outputDirectory = null;
			string inputPath;

			Console.WriteLine("FLV Extract CL v" + General.Version);
			Console.WriteLine("Copyright 2006-2012 J.D. Purcell");
			Console.WriteLine("http://www.moitah.net/");
			Console.WriteLine();

			try {
				while (argIndex < argCount) {
					switch (args[argIndex]) {
						case "-v":
							extractVideo = true;
							break;
						case "-a":
							extractAudio = true;
							break;
						case "-t":
							extractTimeCodes = true;
							break;
						case "-o":
							_autoOverwrite = true;
							break;
						case "-d":
							outputDirectory = args[++argIndex];
							break;
						default:
							goto BreakArgLoop;
					}
					argIndex++;
				}
			BreakArgLoop:

				if (argIndex != (argCount - 1)) {
					throw new Exception();
				}
				inputPath = args[argIndex];
			}
			catch {
				Console.WriteLine("Arguments: [switches] source_path");
				Console.WriteLine();
				Console.WriteLine("Switches:");
				Console.WriteLine("  -v         Extract video.");
				Console.WriteLine("  -a         Extract audio.");
				Console.WriteLine("  -t         Extract timecodes.");
				Console.WriteLine("  -o         Overwrite output files without prompting.");
				Console.WriteLine("  -d <dir>   Output directory.  If not specified, output files will be written");
				Console.WriteLine("             in the same directory as the source file.");
				return 1;
			}

			try {
				using (FLVFile flvFile = new FLVFile(Path.GetFullPath(inputPath))) {
					if (outputDirectory != null) {
						flvFile.OutputDirectory = Path.GetFullPath(outputDirectory);
					}
					flvFile.ExtractStreams(extractAudio, extractVideo, extractTimeCodes, PromptOverwrite);
					if ((flvFile.TrueFrameRate != null) || (flvFile.AverageFrameRate != null)) {
						if (flvFile.TrueFrameRate != null) {
							Console.WriteLine("True Frame Rate: " + flvFile.TrueFrameRate.ToString());
						}
						if (flvFile.AverageFrameRate != null) {
							Console.WriteLine("Average Frame Rate: " + flvFile.AverageFrameRate.ToString());
						}
						Console.WriteLine();
					}
					if (flvFile.Warnings.Length != 0) {
						foreach (string warning in flvFile.Warnings) {
							Console.WriteLine("Warning: " + warning);
						}
						Console.WriteLine();
					}
				}
			}
			catch (Exception ex) {
				Console.WriteLine("Error: " + ex.Message);
				return 1;
			}

			Console.WriteLine("Finished.");
			return 0;
		}

		private static bool PromptOverwrite(string path) {
			if (_autoOverwrite) return true;
			bool? overwrite = null;
			Console.Write("Output file \"" + Path.GetFileName(path) + "\" already exists, overwrite? (y/n): ");
			while (overwrite == null) {
				char c = Console.ReadKey(true).KeyChar;
				if (c == 'y') overwrite = true;
				if (c == 'n') overwrite = false;
			}
			Console.WriteLine(overwrite.Value ? "y" : "n");
			Console.WriteLine();
			return overwrite.Value;
		}
	}
}
