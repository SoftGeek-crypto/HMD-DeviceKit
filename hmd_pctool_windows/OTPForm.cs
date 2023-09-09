using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using hmd_pctool_windows.Components;

namespace hmd_pctool_windows;

public class OTPForm : BorderlessForm
{
	private IContainer components = null;

	private TextBox txtBoxPasscode;

	private Label lblspasscode;

	private NoFocusCueButton btnOK;

	private NoFocusCueButton btnSkip;

	private PictureBox picBoxLogo;

	private LinkLabel linklblOtp;

	public OTPForm()
	{
		InitializeComponent();
	}

	private async void btnOK_Click(object sender, EventArgs e)
	{
		base.Enabled = false;
		ServerResponse ret = await AzureNativeClient.Instance.CheckOTP(txtBoxPasscode.Text);
		if (ret.IsSuccessed)
		{
			base.Enabled = true;
			Hide();
			Program.functionSelectForm.Show();
		}
		else
		{
			base.Enabled = true;
			txtBoxPasscode.Text = "";
			MessageBox.Show(ret.FailReason);
		}
	}

	private void btnSkip_Click(object sender, EventArgs e)
	{
		base.Enabled = true;
		Hide();
		Program.functionSelectForm.Show();
	}

	private void picBoxClose_Click(object sender, EventArgs e)
	{
		Application.Exit();
	}

	private async void linklblOtp_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
	{
		ServerResponse ret = await AzureNativeClient.Instance.SendEmail();
		if (ret.IsSuccessed)
		{
			MessageBox.Show(ret.Message);
		}
		else
		{
			MessageBox.Show(ret.FailReason);
		}
	}

	private void txtBoxPasscode_KeyDown(object sender, KeyEventArgs e)
	{
		if (e.KeyCode == Keys.Return)
		{
			btnOK_Click(sender, e);
		}
	}

	private void OTPForm_FormClosed(object sender, FormClosedEventArgs e)
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(hmd_pctool_windows.OTPForm));
		this.txtBoxPasscode = new System.Windows.Forms.TextBox();
		this.lblspasscode = new System.Windows.Forms.Label();
		this.btnOK = new hmd_pctool_windows.Components.NoFocusCueButton();
		this.btnSkip = new hmd_pctool_windows.Components.NoFocusCueButton();
		this.picBoxLogo = new System.Windows.Forms.PictureBox();
		this.linklblOtp = new System.Windows.Forms.LinkLabel();
		((System.ComponentModel.ISupportInitialize)this.picBoxLogo).BeginInit();
		base.SuspendLayout();
		this.txtBoxPasscode.Location = new System.Drawing.Point(178, 111);
		this.txtBoxPasscode.MaxLength = 20;
		this.txtBoxPasscode.Name = "txtBoxPasscode";
		this.txtBoxPasscode.Size = new System.Drawing.Size(175, 22);
		this.txtBoxPasscode.TabIndex = 2;
		this.txtBoxPasscode.KeyDown += new System.Windows.Forms.KeyEventHandler(txtBoxPasscode_KeyDown);
		this.lblspasscode.AutoSize = true;
		this.lblspasscode.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.lblspasscode.Location = new System.Drawing.Point(48, 113);
		this.lblspasscode.Name = "lblspasscode";
		this.lblspasscode.Size = new System.Drawing.Size(114, 15);
		this.lblspasscode.TabIndex = 5;
		this.lblspasscode.Text = "One-time Passcode ";
		this.btnOK.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(65, 214, 171);
		this.btnOK.FlatAppearance.BorderSize = 2;
		this.btnOK.FlatAppearance.MouseDownBackColor = System.Drawing.Color.White;
		this.btnOK.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(65, 214, 171);
		this.btnOK.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnOK.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnOK.Location = new System.Drawing.Point(95, 195);
		this.btnOK.Name = "btnOK";
		this.btnOK.Size = new System.Drawing.Size(75, 27);
		this.btnOK.TabIndex = 6;
		this.btnOK.TabStop = false;
		this.btnOK.Text = "OK";
		this.btnOK.UseVisualStyleBackColor = true;
		this.btnOK.Click += new System.EventHandler(btnOK_Click);
		this.btnSkip.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(65, 214, 171);
		this.btnSkip.FlatAppearance.BorderSize = 2;
		this.btnSkip.FlatAppearance.MouseDownBackColor = System.Drawing.Color.White;
		this.btnSkip.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(65, 214, 171);
		this.btnSkip.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnSkip.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnSkip.Location = new System.Drawing.Point(269, 195);
		this.btnSkip.Name = "btnSkip";
		this.btnSkip.Size = new System.Drawing.Size(75, 27);
		this.btnSkip.TabIndex = 7;
		this.btnSkip.TabStop = false;
		this.btnSkip.Text = "Skip";
		this.btnSkip.UseVisualStyleBackColor = true;
		this.btnSkip.Click += new System.EventHandler(btnSkip_Click);
		this.picBoxLogo.Image = (System.Drawing.Image)resources.GetObject("picBoxLogo.Image");
		this.picBoxLogo.Location = new System.Drawing.Point(30, 10);
		this.picBoxLogo.Name = "picBoxLogo";
		this.picBoxLogo.Size = new System.Drawing.Size(112, 50);
		this.picBoxLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
		this.picBoxLogo.TabIndex = 9;
		this.picBoxLogo.TabStop = false;
		this.linklblOtp.AutoSize = true;
		this.linklblOtp.Location = new System.Drawing.Point(175, 146);
		this.linklblOtp.Name = "linklblOtp";
		this.linklblOtp.Size = new System.Drawing.Size(148, 14);
		this.linklblOtp.TabIndex = 10;
		this.linklblOtp.TabStop = true;
		this.linklblOtp.Text = "Don't know how to get it ?";
		this.linklblOtp.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(linklblOtp_LinkClicked);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 14f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.Color.White;
		base.ClientSize = new System.Drawing.Size(439, 258);
		base.Controls.Add(this.linklblOtp);
		base.Controls.Add(this.picBoxLogo);
		base.Controls.Add(this.btnSkip);
		base.Controls.Add(this.btnOK);
		base.Controls.Add(this.lblspasscode);
		base.Controls.Add(this.txtBoxPasscode);
		this.Font = new System.Drawing.Font("Calibri", 9f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		base.Name = "OTPForm";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		this.Text = "Login";
		base.FormClosed += new System.Windows.Forms.FormClosedEventHandler(OTPForm_FormClosed);
		((System.ComponentModel.ISupportInitialize)this.picBoxLogo).EndInit();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
