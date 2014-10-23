using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;

namespace RotateCalc {
	public class frmRotateCalc : System.Windows.Forms.Form {
		private System.Windows.Forms.TextBox txtX1;
		private System.Windows.Forms.Button btnCalculate;
		private System.Windows.Forms.TextBox txtY1;
		private System.Windows.Forms.TextBox txtY2;
		private System.Windows.Forms.TextBox txtX2;
		private System.Windows.Forms.TextBox txtAngle;
		private System.Windows.Forms.RadioButton rbHorizontal;
		private System.Windows.Forms.RadioButton rbVertical;
		private System.ComponentModel.Container components = null;

		public frmRotateCalc() {
			// Required for Windows Form Designer support
			InitializeComponent();

			// TODO: Add any constructor code here
		}

		protected override void Dispose( bool disposing ) {
			if( disposing ) {
				if (components != null) {
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.txtX1 = new System.Windows.Forms.TextBox();
			this.btnCalculate = new System.Windows.Forms.Button();
			this.txtY1 = new System.Windows.Forms.TextBox();
			this.txtY2 = new System.Windows.Forms.TextBox();
			this.txtX2 = new System.Windows.Forms.TextBox();
			this.txtAngle = new System.Windows.Forms.TextBox();
			this.rbHorizontal = new System.Windows.Forms.RadioButton();
			this.rbVertical = new System.Windows.Forms.RadioButton();
			this.SuspendLayout();
			// 
			// txtX1
			// 
			this.txtX1.Location = new System.Drawing.Point(8, 8);
			this.txtX1.Name = "txtX1";
			this.txtX1.Size = new System.Drawing.Size(44, 21);
			this.txtX1.TabIndex = 0;
			this.txtX1.Text = "";
			// 
			// btnCalculate
			// 
			this.btnCalculate.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnCalculate.Location = new System.Drawing.Point(196, 20);
			this.btnCalculate.Name = "btnCalculate";
			this.btnCalculate.Size = new System.Drawing.Size(72, 23);
			this.btnCalculate.TabIndex = 6;
			this.btnCalculate.Text = "Calculate";
			this.btnCalculate.Click += new System.EventHandler(this.btnCalculate_Click);
			// 
			// txtY1
			// 
			this.txtY1.Location = new System.Drawing.Point(56, 8);
			this.txtY1.Name = "txtY1";
			this.txtY1.Size = new System.Drawing.Size(44, 21);
			this.txtY1.TabIndex = 1;
			this.txtY1.Text = "";
			// 
			// txtY2
			// 
			this.txtY2.Location = new System.Drawing.Point(56, 32);
			this.txtY2.Name = "txtY2";
			this.txtY2.Size = new System.Drawing.Size(44, 21);
			this.txtY2.TabIndex = 3;
			this.txtY2.Text = "";
			// 
			// txtX2
			// 
			this.txtX2.Location = new System.Drawing.Point(8, 32);
			this.txtX2.Name = "txtX2";
			this.txtX2.Size = new System.Drawing.Size(44, 21);
			this.txtX2.TabIndex = 2;
			this.txtX2.Text = "";
			// 
			// txtAngle
			// 
			this.txtAngle.Location = new System.Drawing.Point(8, 68);
			this.txtAngle.Name = "txtAngle";
			this.txtAngle.ReadOnly = true;
			this.txtAngle.Size = new System.Drawing.Size(260, 21);
			this.txtAngle.TabIndex = 7;
			this.txtAngle.Text = "";
			this.txtAngle.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			// 
			// rbHorizontal
			// 
			this.rbHorizontal.Checked = true;
			this.rbHorizontal.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.rbHorizontal.Location = new System.Drawing.Point(112, 12);
			this.rbHorizontal.Name = "rbHorizontal";
			this.rbHorizontal.Size = new System.Drawing.Size(76, 20);
			this.rbHorizontal.TabIndex = 4;
			this.rbHorizontal.TabStop = true;
			this.rbHorizontal.Text = "Horizontal";
			// 
			// rbVertical
			// 
			this.rbVertical.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.rbVertical.Location = new System.Drawing.Point(112, 32);
			this.rbVertical.Name = "rbVertical";
			this.rbVertical.Size = new System.Drawing.Size(64, 20);
			this.rbVertical.TabIndex = 5;
			this.rbVertical.Text = "Vertical";
			// 
			// frmRotateCalc
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
			this.ClientSize = new System.Drawing.Size(276, 97);
			this.Controls.Add(this.rbVertical);
			this.Controls.Add(this.rbHorizontal);
			this.Controls.Add(this.txtAngle);
			this.Controls.Add(this.txtY2);
			this.Controls.Add(this.txtX2);
			this.Controls.Add(this.txtY1);
			this.Controls.Add(this.btnCalculate);
			this.Controls.Add(this.txtX1);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.Name = "frmRotateCalc";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Rotate Calculator";
			this.ResumeLayout(false);

		}
		#endregion

		[STAThread]
		static void Main() {
			Application.Run(new frmRotateCalc());
		}

		private double deg_per_rad = 180 / Math.PI;

		private void btnCalculate_Click(object sender, System.EventArgs e) {
			try {
				double x1, y1, x2, y2, dx, dy;
				double angle;
				bool neg, vert;

				vert = rbVertical.Checked;
				x1 = Double.Parse(txtX1.Text);
				y1 = Double.Parse(txtY1.Text);
				x2 = Double.Parse(txtX2.Text);
				y2 = Double.Parse(txtY2.Text);
				dx = x2 - x1;
				dy = y2 - y1;

				angle = Math.Atan(vert ? (dx / dy) : (dy / dx)) * deg_per_rad;
				neg = (angle < 0);
				if (neg) angle = -angle;

				txtAngle.Text = String.Format("{0} degrees to the {1}", angle, neg ^ vert ? "right" : "left");
			}
			catch (Exception ex) {
				MessageBox.Show("An error has occurred:" + Environment.NewLine + ex.Message,
					"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
	}
}