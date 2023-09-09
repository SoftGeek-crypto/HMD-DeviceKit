using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace hmd_pctool_windows;

public class DownloadForm : Form
{
	private string Url = "";

	private string fileName = "";

	private IContainer components = null;

	private Label label1;

	private ProgressBar progressBar1;

	private Button btnOpen;

	public DownloadForm(string url, string file, string fileFormat)
	{
		InitializeComponent();
		Url = url;
		fileName = file + fileFormat;
	}

	private async void DownloadForm_Load(object sender, EventArgs e)
	{
		string downloadFileUrl = Url;
		LogUtility.VerifyDir(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\HMD_Devicekit\\");
		string destinationFilePath = Environment.GetFolderPath(Environment.SpecialFolder.Personal).ToString() + "\\HMD_Devicekit\\" + fileName;
		using (HttpClientDownloadWithProgress client = new HttpClientDownloadWithProgress(downloadFileUrl, destinationFilePath))
		{
			client.ProgressChanged += delegate(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage)
			{
				label1.Text = "Downloading: " + (int)progressPercentage.Value + "%";
				progressBar1.Value = (int)progressPercentage.Value;
			};
			try
			{
				await client.StartDownload();
			}
			catch (Exception ex2)
			{
				Exception ex = ex2;
				MessageBox.Show("Failed:" + ex.Message);
				Close();
			}
		}
		btnOpen.Enabled = true;
	}

	private void btnOpen_Click(object sender, EventArgs e)
	{
		Process.Start("Explorer.exe", Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\HMD_Devicekit\\");
		Close();
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
		this.label1 = new System.Windows.Forms.Label();
		this.progressBar1 = new System.Windows.Forms.ProgressBar();
		this.btnOpen = new System.Windows.Forms.Button();
		base.SuspendLayout();
		this.label1.AutoSize = true;
		this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9f);
		this.label1.ForeColor = System.Drawing.Color.White;
		this.label1.Location = new System.Drawing.Point(34, 74);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(84, 15);
		this.label1.TabIndex = 4;
		this.label1.Text = "Download 0%";
		this.progressBar1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.progressBar1.BackColor = System.Drawing.Color.FromArgb(19, 36, 53);
		this.progressBar1.Location = new System.Drawing.Point(37, 24);
		this.progressBar1.Margin = new System.Windows.Forms.Padding(1);
		this.progressBar1.Name = "progressBar1";
		this.progressBar1.Size = new System.Drawing.Size(327, 23);
		this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
		this.progressBar1.TabIndex = 1;
		this.btnOpen.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
		this.btnOpen.BackColor = System.Drawing.Color.White;
		this.btnOpen.Enabled = false;
		this.btnOpen.Location = new System.Drawing.Point(289, 74);
		this.btnOpen.Name = "btnOpen";
		this.btnOpen.Size = new System.Drawing.Size(75, 23);
		this.btnOpen.TabIndex = 5;
		this.btnOpen.Text = "Open";
		this.btnOpen.UseVisualStyleBackColor = false;
		this.btnOpen.Click += new System.EventHandler(btnOpen_Click);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.Color.FromArgb(19, 36, 53);
		base.ClientSize = new System.Drawing.Size(406, 142);
		base.Controls.Add(this.btnOpen);
		base.Controls.Add(this.label1);
		base.Controls.Add(this.progressBar1);
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "DownloadForm";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
		this.Text = "Downloader";
		base.Load += new System.EventHandler(DownloadForm_Load);
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
