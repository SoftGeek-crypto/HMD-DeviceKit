using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using hmd_pctool_windows.Components;
using QRCoder;
using static QRCoder.QRCodeGenerator;

namespace hmd_pctool_windows;

public class FqcAuthForm : BorderlessForm
{
	private static ServerResponse response;

	private static bool IsValid = true;

	private static bool IsOnline = true;

	private static bool IsOffline = true;

	private IContainer components = null;

	private Label lblDesc;

	private PictureBox ptBxQRcode;

	private GreenButton btnNewSecret;

	private Timer timerQR;

	private Label lblOnline;

	private Label lblOffline;

	public FqcAuthForm()
	{
		InitializeComponent();
	}

	private async void FqcAuthForm_Load(object sender, EventArgs e)
	{
		response = await AzureNativeClient.Instance.FqcAuthStart();
		showQR(response.ChiperResponse);
		timerQR.Start();
		lblOnline.BackColor = Color.FromArgb(65, 214, 171);
		btnNewSecret.Enabled = false;
	}

	private void timerQR_Tick(object sender, EventArgs e)
	{
		IsValid = false;
		if (IsOnline)
		{
			btnNewSecret.Enabled = true;
			btnNewSecret.Visible = true;
			ptBxQRcode.Image = null;
			lblDesc.Text = "QR's validity is over... \nClick 'New Secret' to create new one!!!";
			timerQR.Stop();
		}
	}

	private void lblOnline_Click(object sender, EventArgs e)
	{
		IsOnline = true;
		btnNewSecret.Visible = true;
		lblOnline.BackColor = Color.FromArgb(65, 214, 171);
		lblOffline.BackColor = Color.Transparent;
		if (IsValid)
		{
			showQR(response.ChiperResponse);
			lblDesc.Text = "Online QR code";
		}
		else
		{
			lblDesc.Text = "QR's validity is over... \nClick 'New Secret' to create new one!!!";
			ptBxQRcode.Image = null;
			btnNewSecret.Enabled = true;
		}
	}

	private async void lblOffline_Click(object sender, EventArgs e)
	{
		if (IsOffline)
		{
			response = await AzureNativeClient.Instance.logFqcOffline();
			IsOffline = false;
		}
		btnNewSecret.Visible = false;
		IsOnline = false;
		lblOffline.BackColor = Color.FromArgb(65, 214, 171);
		lblOnline.BackColor = Color.Transparent;
		lblDesc.Text = "Offline QR code";
		showQR("12345678:EX8LXl/9xUs0yuPIQ2OuBASWGgoPmvmk7WSjfHSMIb2ulsnJl692S3TI//GJp5n2r9GVobhQzBgrSwaByxufJpjE8EtVQ6bMRCkNV5giYdq668JwaBAY+1gCb6S1QCkI6YvA5Xt4CORfRVPxIYNCcnpqjCAdEuhwam+lZVmzJti7vyDZY4mlgzLGDCKChhfmtr60y3KR68DmSOHUEbLGoc1l+M1hoJGV+WCHNRLusOb2Mx//mRi8ezOx5+iKyco1LZoBTWGBYEvFLelj0sIgNfMR0W6LxYfuEarH0adAUzhkR0IP/SfvPPnN/gNwbTf0JiaC3NBGsX2LEM0P/6RLbw==");
	}

	private async void btnNewSecret_Click(object sender, EventArgs e)
	{
		response = await AzureNativeClient.Instance.FqcAuthStart();
		showQR(response.ChiperResponse);
		timerQR.Start();
		lblDesc.Text = "New QR code is generated successfully";
		btnNewSecret.Enabled = false;
		IsValid = true;
	}

	private void showQR(string text = null)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		QRCodeGenerator val = new QRCodeGenerator();
		string text2 = ((text == null) ? response.ChiperResponse : text);
		QRCodeData val2 = val.CreateQrCode(text2, (ECCLevel)2, false, false, (EciMode)0, -1);
		QRCode val3 = new QRCode(val2);
		Bitmap graphic = val3.GetGraphic(10);
		ptBxQRcode.Image = graphic;
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
		this.components = new System.ComponentModel.Container();
		this.lblDesc = new System.Windows.Forms.Label();
		this.ptBxQRcode = new System.Windows.Forms.PictureBox();
		this.btnNewSecret = new hmd_pctool_windows.Components.GreenButton();
		this.timerQR = new System.Windows.Forms.Timer(this.components);
		this.lblOnline = new System.Windows.Forms.Label();
		this.lblOffline = new System.Windows.Forms.Label();
		((System.ComponentModel.ISupportInitialize)this.ptBxQRcode).BeginInit();
		base.SuspendLayout();
		this.lblDesc.AutoSize = true;
		this.lblDesc.Font = new System.Drawing.Font("Calibri", 10f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblDesc.Location = new System.Drawing.Point(12, 25);
		this.lblDesc.Name = "lblDesc";
		this.lblDesc.Size = new System.Drawing.Size(280, 17);
		this.lblDesc.TabIndex = 0;
		this.lblDesc.Text = "Open the QR scanner in the FQC app and Scan";
		this.ptBxQRcode.Location = new System.Drawing.Point(12, 96);
		this.ptBxQRcode.Name = "ptBxQRcode";
		this.ptBxQRcode.Size = new System.Drawing.Size(290, 290);
		this.ptBxQRcode.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
		this.ptBxQRcode.TabIndex = 1;
		this.ptBxQRcode.TabStop = false;
		this.btnNewSecret.Enabled = false;
		this.btnNewSecret.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(65, 214, 171);
		this.btnNewSecret.FlatAppearance.BorderSize = 2;
		this.btnNewSecret.FlatAppearance.MouseDownBackColor = System.Drawing.Color.White;
		this.btnNewSecret.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(65, 214, 171);
		this.btnNewSecret.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnNewSecret.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnNewSecret.Location = new System.Drawing.Point(12, 392);
		this.btnNewSecret.Name = "btnNewSecret";
		this.btnNewSecret.Size = new System.Drawing.Size(290, 33);
		this.btnNewSecret.TabIndex = 2;
		this.btnNewSecret.Text = "Generate New Secret";
		this.btnNewSecret.UseVisualStyleBackColor = true;
		this.btnNewSecret.Click += new System.EventHandler(btnNewSecret_Click);
		this.timerQR.Enabled = true;
		this.timerQR.Interval = 3600000;
		this.timerQR.Tick += new System.EventHandler(timerQR_Tick);
		this.lblOnline.AutoSize = true;
		this.lblOnline.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.lblOnline.Font = new System.Drawing.Font("Calibri", 12f);
		this.lblOnline.Location = new System.Drawing.Point(12, 70);
		this.lblOnline.Name = "lblOnline";
		this.lblOnline.Size = new System.Drawing.Size(54, 21);
		this.lblOnline.TabIndex = 15;
		this.lblOnline.Text = "Online";
		this.lblOnline.Click += new System.EventHandler(lblOnline_Click);
		this.lblOffline.AutoSize = true;
		this.lblOffline.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.lblOffline.Font = new System.Drawing.Font("Calibri", 12f);
		this.lblOffline.Location = new System.Drawing.Point(63, 70);
		this.lblOffline.Name = "lblOffline";
		this.lblOffline.Size = new System.Drawing.Size(55, 21);
		this.lblOffline.TabIndex = 15;
		this.lblOffline.Text = "Offline";
		this.lblOffline.Click += new System.EventHandler(lblOffline_Click);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.Color.White;
		base.ClientSize = new System.Drawing.Size(315, 437);
		base.Controls.Add(this.lblOffline);
		base.Controls.Add(this.lblOnline);
		base.Controls.Add(this.btnNewSecret);
		base.Controls.Add(this.ptBxQRcode);
		base.Controls.Add(this.lblDesc);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
		base.MaximizeBox = false;
		base.Name = "FqcAuthForm";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		this.Text = "FQC Authentication";
		base.Load += new System.EventHandler(FqcAuthForm_Load);
		((System.ComponentModel.ISupportInitialize)this.ptBxQRcode).EndInit();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
