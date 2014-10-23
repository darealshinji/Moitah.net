// ****************************************************************************
// 
// LineDrop
// Copyright (C) 2005  Moitah (moitah@yahoo.com)
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

namespace Moitah {
	public class LineEndConverter {
		private LineEndConverter() {
		}

		public static void ConvertFile(string path, byte[] lineEnd, bool oneAtEnd, bool removeDuplicates) {
			string pathOut = MakePathOut(path);

			ConvertFile(path, pathOut, lineEnd, oneAtEnd, removeDuplicates);
			File.Delete(path);
			File.Move(pathOut, path);
		}

		public static void ConvertFile(string pathIn, string pathOut, byte[] lineEnd, bool oneAtEnd, bool removeDuplicates) {
			int duplicateCount = removeDuplicates ? CountDuplicates(pathIn) : 1;
			ConvertFile(pathIn, pathOut, lineEnd, oneAtEnd, duplicateCount);
		}

		private static void ConvertFile(string pathIn, string pathOut, byte[] lineEnd, bool oneAtEnd, int duplicateCount) {
			const int buffLen = 65536;
			const int window = 2;
			const int overlap = window - 1;

			FileStream fileIn, fileOut;
			long remaining, written, lastChar;
			int lineEndLen, readLen, i, skip, dupes;
			byte[] readBuff, b;

			try {
				fileIn = new FileStream(pathIn, FileMode.Open, FileAccess.Read, FileShare.Read);
				fileOut = new FileStream(pathOut, FileMode.Create, FileAccess.Write, FileShare.Read);
				readBuff = new byte[buffLen + overlap];
				b = new byte[window];
				lineEndLen = lineEnd.Length;
				written = 0;
				lastChar = 0;
				dupes = 0;
				remaining = fileIn.Length;
				skip = overlap;

				while (remaining != 0) {
					readLen = (int)Math.Min((long)buffLen, remaining);
					if (fileIn.Read(readBuff, 0, readLen) != readLen) {
						throw new Exception("FileStream.Read returned less bytes than expected.");
					}
					remaining -= readLen;

					if (remaining == 0) {
						// Flush remaining bytes
						readLen += overlap;
					}
					for (i = 0; i < readLen; i++) {
						b[0] = b[1];
						b[1] = readBuff[i];

						if (skip > 0) {
							skip--;
						}
						else {
							if ((b[0] == 10) || (b[0] == 13)) {
								if ((b[0] == 13) && (b[1] == 10)) {
									skip = 1;
								}
								if ((dupes % duplicateCount) == 0) {
									fileOut.Write(lineEnd, 0, lineEndLen);
									written += lineEndLen;
								}
								dupes++;
							}
							else {
								fileOut.WriteByte(b[0]);
								lastChar = ++written;
								dupes = 0;
							}
						}
					}
				}

				if (oneAtEnd) {
					fileOut.Seek(lastChar, SeekOrigin.Begin);
					fileOut.Write(lineEnd, 0, lineEndLen);
					fileOut.SetLength(lastChar + lineEndLen);
				}

				fileIn.Close();
				fileOut.Close();
			}
			catch {
				try {
					File.Delete(pathOut);
				}
				catch {}

				throw;
			}
		}

		private static int CountDuplicates(string pathIn) {
			const int buffLen = 65536;
			const int window = 2;
			const int overlap = window - 1;

			FileStream fileIn;
			long remaining;
			int readLen, i, skip, dupes, minDupes;
			byte[] readBuff, b;

			fileIn = new FileStream(pathIn, FileMode.Open, FileAccess.Read, FileShare.Read);
			readBuff = new byte[buffLen + overlap];
			b = new byte[window];
			dupes = 0;
			minDupes = Int32.MaxValue;
			remaining = fileIn.Length;
			skip = overlap;

			while (remaining != 0) {
				readLen = (int)Math.Min((long)buffLen, remaining);
				if (fileIn.Read(readBuff, 0, readLen) != readLen) {
					throw new Exception("FileStream.Read returned less bytes than expected.");
				}
				remaining -= readLen;

				if (remaining == 0) {
					// Flush remaining bytes
					readLen += overlap;
				}
				for (i = 0; i < readLen; i++) {
					b[0] = b[1];
					b[1] = readBuff[i];

					if (skip > 0) {
						skip--;
					}
					else {
						if ((b[0] == 10) || (b[0] == 13)) {
							if ((b[0] == 13) && (b[1] == 10)) {
								skip = 1;
							}
							dupes++;
						}
						else {
							if (dupes != 0) {
								minDupes = Math.Min(minDupes, dupes);
                                dupes = 0;
							}
						}
					}
				}
			}

			fileIn.Close();

			if (dupes != 0) {
				minDupes = Math.Min(minDupes, dupes);
			}

			return (minDupes == Int32.MaxValue) ? 1 : minDupes;
		}

		private static string MakePathOut(string pathIn) {
			const string suffix = ".le";
			string a, b, pathOut;
			Random rand = new Random();
			FileStream fs;

			a = Path.Combine(Path.GetDirectoryName(pathIn),
				Path.GetFileNameWithoutExtension(pathIn) + suffix);
			b = Path.GetExtension(pathIn);

			while (true) {
				pathOut = a + rand.Next(100000, 999999).ToString() + b;
				try {
					fs = new FileStream(pathOut, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
					fs.Close();
					break;
				}
				catch {
				}
			}

			return pathOut;
		}
	}
}