using System.Windows.Forms;

namespace JDP {
	internal partial class frmUserData : Form {
		public frmUserData(string action) {
			InitializeComponent();
			Program.SetFontAndScaling(this);
			ControlBox = false;
			Text = action + " User Data";
		}

		public string UserDataString {
			get {
				return txtUserData.Text;
			}
			set {
				txtUserData.Text = value;
			}
		}

		private void txtUserData_KeyDown(object sender, KeyEventArgs e) {
			if (e.KeyCode == Keys.Enter) {
				btnOK.PerformClick();
			}
		}
	}
}
