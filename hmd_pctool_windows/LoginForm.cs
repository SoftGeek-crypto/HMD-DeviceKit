using System;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using hmd_pctool_windows.Components;

namespace hmd_pctool_windows;

public class LoginForm : BorderlessForm
{
	private IContainer components = null;

	private TextBox txtBoxAccount;

	private TextBox txtBoxPassword;

	private Label lblsAccount;

	private Label lblsPassword;

	private GreenButton btnOK;

	private GreenButton btnCancel;

	private PictureBox picBoxClose;

	private PictureBox picBoxLogo;

	private Label lblVersion;

	public LoginForm()
	{
		InitializeComponent();
	}

	private void Form1_Load(object sender, EventArgs e)
	{
		lblVersion.Text = $"v{Assembly.GetExecutingAssembly().GetName().Version.ToString()}";
	}

	private async void btnOK_Click(object sender, EventArgs e)
	{
		base.Enabled = false;
		LoginAzureForm loginAzureForm = new LoginAzureForm();
		loginAzureForm.Show();
		string errorMsg = await AzureNativeClient.Instance.Login(txtBoxAccount.Text, txtBoxPassword.Text);
		if (errorMsg.Equals("OK"))
		{
			loginAzureForm.Close();
			base.Enabled = true;
			Hide();
			if (Program.isOtpEnable())
			{
				OTPForm otpForm = new OTPForm();
				otpForm.Show();
			}
			else
			{
				Program.functionSelectForm.Show();
			}
		}
		else
		{
			MessageBox.Show(errorMsg);
			loginAzureForm.Close();
			base.Enabled = true;
			BringToFront();
		}
	}

	private void btnCancel_Click(object sender, EventArgs e)
	{
		Close();
	}

	private void picBoxClose_MouseHover(object sender, EventArgs e)
	{
		picBoxClose.BackColor = Color.FromArgb(65, 214, 171);
	}

	private void picBoxClose_MouseLeave(object sender, EventArgs e)
	{
		picBoxClose.BackColor = Color.White;
	}

	private void picBoxClose_Click(object sender, EventArgs e)
	{
		Application.Exit();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(hmd_pctool_windows.LoginForm));
		this.txtBoxAccount = new System.Windows.Forms.TextBox();
		this.txtBoxPassword = new System.Windows.Forms.TextBox();
		this.lblsAccount = new System.Windows.Forms.Label();
		this.lblsPassword = new System.Windows.Forms.Label();
		this.btnOK = new hmd_pctool_windows.Components.GreenButton();
		this.btnCancel = new hmd_pctool_windows.Components.GreenButton();
		this.picBoxClose = new System.Windows.Forms.PictureBox();
		this.picBoxLogo = new System.Windows.Forms.PictureBox();
		this.lblVersion = new System.Windows.Forms.Label();
		((System.ComponentModel.ISupportInitialize)this.picBoxClose).BeginInit();
		((System.ComponentModel.ISupportInitialize)this.picBoxLogo).BeginInit();
		base.SuspendLayout();
		this.txtBoxAccount.Location = new System.Drawing.Point(181, 123);
		this.txtBoxAccount.Name = "txtBoxAccount";
		this.txtBoxAccount.Size = new System.Drawing.Size(175, 22);
		this.txtBoxAccount.TabIndex = 0;
		this.txtBoxPassword.Location = new System.Drawing.Point(181, 171);
		this.txtBoxPassword.Name = "txtBoxPassword";
		this.txtBoxPassword.PasswordChar = '*';
		this.txtBoxPassword.Size = new System.Drawing.Size(175, 22);
		this.txtBoxPassword.TabIndex = 1;
		this.lblsAccount.AutoSize = true;
		this.lblsAccount.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.lblsAccount.Location = new System.Drawing.Point(122, 126);
		this.lblsAccount.Name = "lblsAccount";
		this.lblsAccount.Size = new System.Drawing.Size(51, 15);
		this.lblsAccount.TabIndex = 3;
		this.lblsAccount.Text = "Account";
		this.lblsPassword.AutoSize = true;
		this.lblsPassword.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.lblsPassword.Location = new System.Drawing.Point(112, 173);
		this.lblsPassword.Name = "lblsPassword";
		this.lblsPassword.Size = new System.Drawing.Size(61, 15);
		this.lblsPassword.TabIndex = 4;
		this.lblsPassword.Text = "Password";
		this.btnOK.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(65, 214, 171);
		this.btnOK.FlatAppearance.BorderSize = 2;
		this.btnOK.FlatAppearance.MouseDownBackColor = System.Drawing.Color.White;
		this.btnOK.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(65, 214, 171);
		this.btnOK.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnOK.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnOK.Location = new System.Drawing.Point(132, 253);
		this.btnOK.Name = "btnOK";
		this.btnOK.Size = new System.Drawing.Size(75, 27);
		this.btnOK.TabIndex = 6;
		this.btnOK.TabStop = false;
		this.btnOK.Text = "OK";
		this.btnOK.UseVisualStyleBackColor = true;
		this.btnOK.Click += new System.EventHandler(btnOK_Click);
		this.btnCancel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(65, 214, 171);
		this.btnCancel.FlatAppearance.BorderSize = 2;
		this.btnCancel.FlatAppearance.MouseDownBackColor = System.Drawing.Color.White;
		this.btnCancel.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(65, 214, 171);
		this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnCancel.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnCancel.Location = new System.Drawing.Point(298, 253);
		this.btnCancel.Name = "btnCancel";
		this.btnCancel.Size = new System.Drawing.Size(75, 27);
		this.btnCancel.TabIndex = 7;
		this.btnCancel.TabStop = false;
		this.btnCancel.Text = "Cancel";
		this.btnCancel.UseVisualStyleBackColor = true;
		this.btnCancel.Click += new System.EventHandler(btnCancel_Click);
		this.picBoxClose.BackColor = System.Drawing.Color.White;
		this.picBoxClose.Image = (System.Drawing.Image)resources.GetObject("picBoxClose.Image");
		this.picBoxClose.Location = new System.Drawing.Point(454, 10);
		this.picBoxClose.Name = "picBoxClose";
		this.picBoxClose.Size = new System.Drawing.Size(20, 20);
		this.picBoxClose.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
		this.picBoxClose.TabIndex = 8;
		this.picBoxClose.TabStop = false;
		this.picBoxClose.Click += new System.EventHandler(picBoxClose_Click);
		this.picBoxClose.MouseLeave += new System.EventHandler(picBoxClose_MouseLeave);
		this.picBoxClose.MouseHover += new System.EventHandler(picBoxClose_MouseHover);
		this.picBoxLogo.Image = (System.Drawing.Image)resources.GetObject("picBoxLogo.Image");
		this.picBoxLogo.Location = new System.Drawing.Point(30, 10);
		this.picBoxLogo.Name = "picBoxLogo";
		this.picBoxLogo.Size = new System.Drawing.Size(112, 50);
		this.picBoxLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
		this.picBoxLogo.TabIndex = 9;
		this.picBoxLogo.TabStop = false;
		this.lblVersion.AutoSize = true;
		this.lblVersion.Location = new System.Drawing.Point(431, 300);
		this.lblVersion.Name = "lblVersion";
		this.lblVersion.Size = new System.Drawing.Size(0, 14);
		this.lblVersion.TabIndex = 10;
		this.lblVersion.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 14f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.Color.White;
		base.ClientSize = new System.Drawing.Size(484, 316);
		base.Controls.Add(this.lblVersion);
		base.Controls.Add(this.picBoxLogo);
		base.Controls.Add(this.picBoxClose);
		base.Controls.Add(this.btnCancel);
		base.Controls.Add(this.btnOK);
		base.Controls.Add(this.lblsPassword);
		base.Controls.Add(this.lblsAccount);
		base.Controls.Add(this.txtBoxPassword);
		base.Controls.Add(this.txtBoxAccount);
		this.Font = new System.Drawing.Font("Calibri", 9f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		base.Name = "LoginForm";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		this.Text = "Login";
		base.Load += new System.EventHandler(Form1_Load);
		((System.ComponentModel.ISupportInitialize)this.picBoxClose).EndInit();
		((System.ComponentModel.ISupportInitialize)this.picBoxLogo).EndInit();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
