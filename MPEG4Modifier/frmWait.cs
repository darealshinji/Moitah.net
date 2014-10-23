using System;
using System.Threading;
using System.Windows.Forms;

namespace JDP {
	partial class frmWait : Form {
		private string _title;
		private Thread _workThread;
		private bool _stop;

		public frmWait(string title, string message, Thread workThread) {
			InitializeComponent();
			Program.SetFontAndScaling(this);

			// There's a bug in .NET that messes up the size of a form if its ControlBox
			// property is set to false in the designer and its location is changed
			// programmatically before it's shown
			ControlBox = false;

			_title = "% - " + title;
			lblMessage.Text = message;
			_workThread = workThread;
			_stop = false;
		}

		public bool SetProgress(double progress) {
			if ((progress < 0.0) || Double.IsInfinity(progress) || Double.IsNaN(progress)) {
				progress = 0.0;
			}
			else if (progress > 1.0) {
				progress = 1.0;
			}

			BeginInvoke((MethodInvoker)delegate() {
				prgWait.Value = (int)(progress * (double)prgWait.Maximum);
				Text = Convert.ToString((int)(progress * 100.0)) + _title;
			});

			return _stop;
		}

		public void FinishedWork() {
			BeginInvoke((MethodInvoker)delegate() {
				Close();
			});
		}

		private void frmWait_Load(object sender, EventArgs e) {
			SetProgress(0);
			_workThread.Start();
		}

		private void btnCancel_Click(object sender, EventArgs e) {
			_stop = true;
		}
	}
}
