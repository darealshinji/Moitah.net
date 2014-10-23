// ****************************************************************************
// 
// VDTimer
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
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace VDTimer {
	public static class Win32 {
		[DllImport("user32.dll")]
		public static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern int GetWindowText(IntPtr hWnd, [Out] StringBuilder lpString, int nMaxCount);

		[DllImport("user32.dll", SetLastError=true, CharSet=CharSet.Auto)]
		public static extern int GetWindowTextLength(IntPtr hWnd);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, IntPtr windowTitle);

		[DllImport("user32.dll")]
		public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, IntPtr dwExtraInfo);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

		public const int KEYEVENTF_KEYUP = 2;

		public const UInt32 VK_F6 = 0x75;
		public const UInt32 VK_ESCAPE = 0x1B;

		public const UInt32 WM_KEYDOWN = 0x100;
		public const UInt32 WM_KEYUP = 0x101;
	}

	public static class VDCapture {
		private static IntPtr _hWnd = IntPtr.Zero;

		private static string GetTitleBarText(IntPtr hWnd) {
			int len = Win32.GetWindowTextLength(hWnd);
			StringBuilder sb = new StringBuilder(len + 1);

			Win32.GetWindowText(hWnd, sb, sb.Capacity);

			return sb.ToString();
		}

		private static IntPtr[] FindVirtualDubWindows() {
			List<IntPtr> windows = new List<IntPtr>();
			IntPtr hWnd = IntPtr.Zero;

			while (true) {
				hWnd = Win32.FindWindowEx(IntPtr.Zero, hWnd, "VirtualDub", IntPtr.Zero);
				if (hWnd == IntPtr.Zero) {
					break;
				}
				else {
					windows.Add(hWnd);
				}
			}

			return windows.ToArray();
		}

		private static bool IsInCaptureMode(IntPtr hWnd) {
			return (GetTitleBarText(hWnd).IndexOf(" - capture mode [") != -1);
		}

		public static bool FindCaptureWindow() {
			IntPtr[] windows = FindVirtualDubWindows();
			IntPtr found = IntPtr.Zero;

			foreach (IntPtr w in windows) {
				if (IsInCaptureMode(w) && (GetCapturePath(w).Length != 0)) {
					if (found == IntPtr.Zero) {
						found = w;
					}
					else {
						// Multiple capture windows found
						found = IntPtr.Zero;
						break;
					}
				}
			}

			_hWnd = found;

			return (_hWnd != IntPtr.Zero);
		}

		private static void VerifyCaptureWindow() {
			if (IsInCaptureMode(_hWnd) == false) {
				throw new Exception("Cannot find the capture window.");
			}
		}

		private static void FocusWindow(IntPtr hWnd) {
			if (Win32.GetForegroundWindow() != hWnd) {
				DateTime timeout = DateTime.Now.AddSeconds(5.0);

				do {
					if (DateTime.Now >= timeout) {
						throw new Exception("Unable to focus window.");
					}
					Win32.SwitchToThisWindow(hWnd, true);
					Thread.Sleep(100);
				} while (Win32.GetForegroundWindow() != hWnd);
			}
		}

		private static void SendF6(IntPtr hWnd) {
			Win32.PostMessage(hWnd, Win32.WM_KEYDOWN, new IntPtr(Win32.VK_F6), new IntPtr(0));
			Win32.PostMessage(hWnd, Win32.WM_KEYUP, new IntPtr(Win32.VK_F6), new IntPtr(0));
		}

		private static void SendEsc(IntPtr hWnd) {
			Win32.PostMessage(hWnd, Win32.WM_KEYDOWN, new IntPtr(Win32.VK_ESCAPE), new IntPtr(0));
			Win32.PostMessage(hWnd, Win32.WM_KEYUP, new IntPtr(Win32.VK_ESCAPE), new IntPtr(0));
		}

		private static void KeyEsc() {
			Win32.keybd_event((byte)Win32.VK_ESCAPE, 0, 0, IntPtr.Zero);
			Win32.keybd_event((byte)Win32.VK_ESCAPE, 0, Win32.KEYEVENTF_KEYUP, IntPtr.Zero);
		}

		public static void StartCapture() {
			DateTime timeout;

			FocusWindow(_hWnd);

			timeout = DateTime.Now.AddSeconds(10.0);
			while (IsCapturing() == false) {
				if (DateTime.Now >= timeout) {
					throw new Exception("Unable to start video capture.");
				}
				SendF6(_hWnd);
				Thread.Sleep(250);
			};
		}

		public static void StopCapture() {
			DateTime timeout;
			string capPath = GetCapturePath();

			timeout = DateTime.Now.AddSeconds(10.0);
			while (IsCapturing() == true) {
				if (DateTime.Now >= timeout) {
					throw new Exception("Unable to stop video capture.");
				}
				FocusWindow(_hWnd);
				KeyEsc();
				Thread.Sleep(250);
			}

			timeout = DateTime.Now.AddSeconds(10.0);
			while (IsFileLocked(capPath) == true) {
				if (DateTime.Now >= timeout) {
					throw new Exception("The lock on the capture file is not being released.");
				}
				Thread.Sleep(250);
			}
		}

		public static bool IsCapturing() {
			VerifyCaptureWindow();

			return GetTitleBarText(_hWnd).EndsWith(" [capture in progress]");
		}

		private static bool IsFileLocked(string path) {
			FileStream fs = null;
			bool locked;
			
			try {
				fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
				locked = false;
			}
			catch (IOException) {
				locked = true;
			}
			catch {
				throw new Exception("Unable to check if file is locked.");
			}
			finally {
				if (fs != null) {
					fs.Close();
				}
			}

			return locked;
		}

		private static string GetCapturePath(IntPtr hWnd) {
			const string pathPrefix = " - capture mode [";
			string title = GetTitleBarText(hWnd);
			int posStart, posEnd;

			posStart = title.IndexOf(pathPrefix);
			if (posStart != -1) {
				posStart += pathPrefix.Length;

				posEnd = title.IndexOf("]", posStart);
				if (posEnd != -1) {
					return title.Substring(posStart, posEnd - posStart);
				}
			}

			return String.Empty;
		}

		public static string GetCapturePath() {
			VerifyCaptureWindow();

			string path = GetCapturePath(_hWnd);

			if (path.Length == 0) {
				throw new Exception("Unable to find the path of the capture file.");
			}

			return path;
		}
	}
}