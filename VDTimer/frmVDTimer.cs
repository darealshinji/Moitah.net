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
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO;

namespace VDTimer {
	public partial class frmVDTimer : Form {
		private const string _timeFormat = "yyyy-MM-dd  HH:mm:ss";

		private Thread _timerThread = null;
		private volatile bool _timerEnabled;

		public frmVDTimer() {
			InitializeComponent();
		}

		private void frmVDTimer_Load(object sender, EventArgs e) {
			lvwTimes.ListViewItemSorter = new LVScheduleSort();
			LoadSchedule();
		}

		private void frmVDTimer_FormClosed(object sender, FormClosedEventArgs e) {
			if ((_timerThread != null) && (_timerThread.IsAlive)) {
				_timerEnabled = false;
				_timerThread.Join();
			}
			SaveSchedule();
		}

		private void btnAdd_Click(object sender, EventArgs e) {
			frmTimeInput ti = new frmTimeInput("Add Time");

			ti.StartTime = DateTime.Now.AddSeconds(10.0);
			ti.StopTime = DateTime.Now.AddSeconds(20.0);

			if (ti.ShowDialog() == DialogResult.OK) {
				if (ScheduleConflictsWith(ti.StartTime, ti.StopTime)) {
					ShowScheduleConflictError();
				}
				else {
					lvwTimes.Items.Add(CreateLVI(ti.StartTime, ti.StopTime, ti.Comment));
					SelectItem(ti.StartTime);
				}
			}
		}

		private void btnEdit_Click(object sender, EventArgs e) {
			if (lvwTimes.SelectedItems.Count == 1) {
				ListViewItem item = lvwTimes.SelectedItems[0];
				frmTimeInput ti = new frmTimeInput("Edit Time");

				ti.StartTime = (DateTime)item.SubItems[0].Tag;
				ti.StopTime = (DateTime)item.SubItems[1].Tag;
				ti.Comment = item.SubItems[2].Text;

				if (ti.ShowDialog() == DialogResult.OK) {
					int i = lvwTimes.SelectedIndices[0];
					if (ScheduleConflictsWith(ti.StartTime, ti.StopTime, i)) {
						ShowScheduleConflictError();
					}
					else {
						lvwTimes.Items[i] = CreateLVI(ti.StartTime, ti.StopTime, ti.Comment);
						SelectItem(ti.StartTime);
					}
				}
			}
		}

		private void btnRemove_Click(object sender, EventArgs e) {
			while (lvwTimes.SelectedItems.Count > 0) {
				lvwTimes.SelectedItems[0].Remove();
			}
		}

		private void btnClear_Click(object sender, EventArgs e) {
			lvwTimes.Items.Clear();
		}

		private void chkEnableTimer_CheckedChanged(object sender, EventArgs e) {
			bool enabled, threadRunning;

			enabled = chkEnableTimer.Checked;
			threadRunning = (_timerThread != null) && _timerThread.IsAlive;

			btnAdd.Enabled = !enabled;
			btnEdit.Enabled = !enabled;
			btnRemove.Enabled = !enabled;
			btnClear.Enabled = !enabled;
			lblCaptureWindowFoundDesc.Visible = enabled;
			lblCaptureWindowFound.Text = String.Empty;
			lblCaptureWindowFound.Visible = enabled;

			if (enabled) {
				if (threadRunning == false) {
					_timerEnabled = true;
					_timerThread = new Thread(new ThreadStart(RunTimer));
					_timerThread.Start();
				}
				else {
					chkEnableTimer.Checked = false;
					MessageBox.Show("Cannot start timer, the old thread is still running.",
						"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
			else {
				if (threadRunning) {
					_timerEnabled = false;
				}
			}
		}

		private bool TimesOverlap(DateTime start1, DateTime stop1, DateTime start2, DateTime stop2) {
			return !((stop2 <= start1) || (start2 >= stop1));
		}

		private bool ScheduleConflictsWith(DateTime startTime, DateTime stopTime, int ignoreIndex) {
			for (int i = 0; i < lvwTimes.Items.Count; i++) {
				if (i != ignoreIndex) {
					ListViewItem item = lvwTimes.Items[i];
					if (TimesOverlap((DateTime)item.SubItems[0].Tag, (DateTime)item.SubItems[1].Tag,
						startTime, stopTime))
					{
						return true;
					}
				}
			}

			return false;
		}

		private bool ScheduleConflictsWith(DateTime startTime, DateTime stopTime) {
			return ScheduleConflictsWith(startTime, stopTime, -1);
		}

		private void ShowScheduleConflictError() {
			MessageBox.Show("The specified time range overlaps another time range in the schedule.",
				"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		private void SelectItem(DateTime startTime) {
			lvwTimes.SelectedItems.Clear();

			foreach (ListViewItem item in lvwTimes.Items) {
				if ((DateTime)item.SubItems[0].Tag == startTime) {
					item.Selected = true;
					break;
				}
			}
		}

		private ListViewItem CreateLVI(DateTime startTime, DateTime stopTime, string comment) {
			ListViewItem item = new ListViewItem( new string[] {
				startTime.ToString(_timeFormat), stopTime.ToString(_timeFormat), comment } );

			item.SubItems[0].Tag = startTime;
			item.SubItems[1].Tag = stopTime;

			return item;
		}

		private void RunTimer() {
			int timeCount = 0;
			int i;
			DateTime[] startTime = null;
			DateTime[] stopTime = null;
			DateTime timeNow;
			string capturePath;

			Invoke((MethodInvoker)delegate() {
				timeCount = lvwTimes.Items.Count;
				startTime = new DateTime[timeCount];
				stopTime = new DateTime[timeCount];

				for (i = 0; i < timeCount; i++) {
					ListViewItem item = lvwTimes.Items[i];
					startTime[i] = (DateTime)item.SubItems[0].Tag;
					stopTime[i] = (DateTime)item.SubItems[1].Tag;
				}
			});

			while (_timerEnabled) {
				if (FindCaptureWindow()) {
					timeNow = DateTime.Now;
					for (i = 0; i < timeCount; i++) {
						if ((timeNow >= startTime[i]) && (timeNow < stopTime[i])) {
							capturePath = VDCapture.GetCapturePath();

							if (!VDCapture.IsCapturing()) {
								NumberFile(capturePath);
								VDCapture.StartCapture();
							}
							
							while (DateTime.Now < stopTime[i]) {
								if (!_timerEnabled) {
									return;
								}
								Thread.Sleep(100);
								FindCaptureWindow();
							}

							VDCapture.StopCapture();
							NumberFile(capturePath);
						}
					}
				}

				Thread.Sleep(100);
			}
		}

		private bool FindCaptureWindow() {
			bool found = VDCapture.FindCaptureWindow();

			BeginInvoke((MethodInvoker)delegate() {
				lblCaptureWindowFound.Text = found ? "Yes" : "No";
			});

			return found;
		}

		private void NumberFile(string path) {
			string prefix, suffix, newPath;

			if (File.Exists(path) == false) {
				return;
			}

			suffix = Path.GetExtension(path);
			prefix = path.Substring(0, path.Length - suffix.Length);

			for (int i = 0; i <= 999; i++) {
				newPath = String.Format("{0}{1:000}{2}", prefix, i, suffix);
				if (File.Exists(newPath) == false) {
					File.Move(path, newPath);
					return;
				}
			}

			throw new Exception("Cannot find an available filename to rename capture file.");
		}

		private string GetMyAppDataDir() {
			string appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string myAppDataDir = Path.Combine(appDataDir, "VDTimer");

			if (Directory.Exists(myAppDataDir) == false) {
				Directory.CreateDirectory(myAppDataDir);
			}

			return myAppDataDir;
		}

		private void LoadSchedule() {
			string path;
			StreamReader sr;
			string line, comment;
			string[] lineSplit;
			DateTime startTime, stopTime;

			try {
				path = Path.Combine(GetMyAppDataDir(), "schedule.txt");

				using (sr = new StreamReader(path, Encoding.UTF8)) {
					while ((line = sr.ReadLine()) != null) {
						lineSplit = line.Split(new char[] { ',' }, 3);
						startTime = new DateTime(Int64.Parse(lineSplit[0]));
						stopTime = new DateTime(Int64.Parse(lineSplit[1]));
						comment = lineSplit[2];

						if (ScheduleConflictsWith(startTime, stopTime) == false) {
							lvwTimes.Items.Add(CreateLVI(startTime, stopTime, comment));
						}
					}
				}
			}
			catch {
			}
		}

		private void SaveSchedule() {
			string path;
			StreamWriter sw;
			string line, comment;
			DateTime startTime, stopTime;

			try {
				path = Path.Combine(GetMyAppDataDir(), "schedule.txt");

				using (sw = new StreamWriter(path, false, Encoding.UTF8)) {
					foreach (ListViewItem item in lvwTimes.Items) {
						startTime = (DateTime)item.SubItems[0].Tag;
						stopTime = (DateTime)item.SubItems[1].Tag;
						comment = item.SubItems[2].Text;

						line = String.Format("{0},{1},{2}", startTime.Ticks, stopTime.Ticks, comment);
						sw.WriteLine(line);
					}
				}
			}
			catch {
			}
		}
	}

	public class LVScheduleSort : System.Collections.IComparer {
		public int Compare(object x, object y) {
			ListViewItem xItem = (ListViewItem)x;
			ListViewItem yItem = (ListViewItem)y;
			DateTime xDT = (DateTime)xItem.SubItems[0].Tag;
			DateTime yDT = (DateTime)yItem.SubItems[0].Tag;

			return DateTime.Compare(xDT, yDT);
		}
	}
}