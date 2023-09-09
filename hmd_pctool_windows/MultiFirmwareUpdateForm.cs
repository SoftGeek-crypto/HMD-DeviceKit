using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using hmd_pctool_windows.Components;
using hmd_pctool_windows.Properties;
using hmd_pctool_windows.Utils;

namespace hmd_pctool_windows;

public class MultiFirmwareUpdateForm : BorderlessForm
{
	private delegate void UpdateUI(Control control, object o);

	private List<DevicePanel> devicePanelList = new List<DevicePanel>();

	private DeviceManager deviceManager;

	private readonly bool isTestMode = false;

	private IContainer components = null;

	private GreenButton btnCanceltAll;

	private GreenButton btnClose;

	private PictureBox picBoxLogo;

	private Panel panelTest;

	private NumericUpDown numericUpDownTestTimes;

	private Button btnRunTest;

	private GreenButton btnSelectForAll;

	private Panel panelLog;

	private Label lblLogPath;

	private Button btnSetLogPath;

	private Label lblsLogPath;

	private LinkLabel lkLblUserGuide;

	private Panel panelCenter;

	private FlowLayoutPanel flowLayoutPanelDeviceList;

	private CheckBox chkBxCFlash;

	private Label labelNoDevice;

	public MultiFirmwareUpdateForm()
	{
		deviceManager = DeviceManager.Instance;
		deviceManager.EventHandler += OnDeviceChanged;
		InitializeComponent();
		if (isTestMode)
		{
			panelTest.Enabled = true;
			panelTest.Visible = true;
		}
		else
		{
			panelTest.Enabled = false;
			panelTest.Visible = false;
		}
		lblLogPath.Text = OutputXml.OutPath;
	}

	private void OnDeviceChanged(object sender, DmEventArgs args)
	{
		switch (args.EventType)
		{
		case DeviceManager.DmEventType.DevicesAdded:
		{
			if (args.Devices == null)
			{
				break;
			}
			Device[] devices = args.Devices;
			foreach (Device device in devices)
			{
				if (device == null || device.SN.Contains("?"))
				{
					continue;
				}
				DevicePanel devicePanel = devicePanelList.Find((DevicePanel o) => o.PanelDevice.SN.Equals(device.SN));
				if (devicePanel == null)
				{
					if (device is HmdDevice)
					{
						(device as HmdDevice).DataCable = devicePanelList.Count + 1;
					}
					DevicePanel o2 = new DevicePanel(device);
					AddDevice(flowLayoutPanelDeviceList, o2);
				}
			}
			break;
		}
		case DeviceManager.DmEventType.DevicesRemoved:
			break;
		case DeviceManager.DmEventType.MonitoringStarted:
			break;
		case DeviceManager.DmEventType.MonitoringStopped:
			break;
		}
	}

	private void MultiFirmwareUpdateForm_Load(object sender, EventArgs e)
	{
		flowLayoutPanelDeviceList.VerticalScroll.Visible = true;
		StartMonitoringAsync();
		InitCflashVar();
	}

	private async void StartMonitoringAsync()
	{
		try
		{
			await deviceManager.StartMonitoringAsync();
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			LogUtility.E("MultiFirmwareUpdateForm", ex.Message + Environment.NewLine + ex.StackTrace);
		}
	}

	private void BtnCanceltAll_Click(object sender, EventArgs e)
	{
		foreach (DevicePanel devicePanel in devicePanelList)
		{
			if (devicePanel.IsFlashing)
			{
				devicePanel.CancelFlash();
			}
		}
	}

	protected override void OnClosed(EventArgs e)
	{
		if (devicePanelList != null)
		{
			foreach (DevicePanel devicePanel in devicePanelList)
			{
				devicePanel.Exit();
			}
		}
		devicePanelList.Clear();
		if (deviceManager != null)
		{
			deviceManager.EventHandler -= OnDeviceChanged;
			deviceManager.StopMonitoring();
		}
	}

	private void BtnClose_Click(object sender, EventArgs e)
	{
		foreach (DevicePanel devicePanel in devicePanelList)
		{
			devicePanel.CloseLogForm();
		}
		Program.functionSelectForm.Show();
		InitCflashVar();
		ClearCFlashFiles();
		Close();
	}

	private void AddDevice(Control control, object o)
	{
		if (o == null)
		{
			return;
		}
		if (control.InvokeRequired)
		{
			UpdateUI method = AddDevice;
			control.Invoke(method, control, o);
			return;
		}
		if (devicePanelList.Count == 0)
		{
			labelNoDevice.Visible = false;
		}
		devicePanelList.Add(o as DevicePanel);
		(control as FlowLayoutPanel).Controls.Add(o as DevicePanel);
	}

	private void RemoveDevice(Control control, object o)
	{
		if (o == null)
		{
			return;
		}
		if (control.InvokeRequired)
		{
			UpdateUI method = RemoveDevice;
			control.Invoke(method, control, o);
			return;
		}
		devicePanelList.Remove(o as DevicePanel);
		if (devicePanelList.Count == 0)
		{
			labelNoDevice.Visible = true;
		}
		(control as FlowLayoutPanel).Controls.Remove(o as DevicePanel);
	}

	private void BtnRunTest_Click(object sender, EventArgs e)
	{
		if (!isTestMode || !panelTest.Enabled)
		{
			return;
		}
		int times = decimal.ToInt32(numericUpDownTestTimes.Value);
		foreach (DevicePanel devicePanel in devicePanelList)
		{
			devicePanel.LaunchTestMode(times);
		}
	}

	private void btnSelectForAll_Click(object sender, EventArgs e)
	{
		if (HmdDevice.IsCFlashEnabled && !string.IsNullOrEmpty(DevicePanel.path))
		{
			foreach (DevicePanel devicePanel in devicePanelList)
			{
				devicePanel.ApplyRom(DevicePanel.path);
			}
			MessageBox.Show("Continuous flashing is enabled..." + Environment.NewLine + "Same file will be used to flash" + Environment.NewLine + "FilePath: " + DevicePanel.path);
			return;
		}
		OpenFileDialog openFileDialog = new OpenFileDialog();
		openFileDialog.Title = "Select ROM file";
		openFileDialog.InitialDirectory = ".\\";
		openFileDialog.Filter = "zip files (*.*)|*.zip";
		if (openFileDialog.ShowDialog() != DialogResult.OK)
		{
			return;
		}
		foreach (DevicePanel devicePanel2 in devicePanelList)
		{
			devicePanel2.ApplyRom(openFileDialog.FileName);
		}
		DevicePanel.path = openFileDialog.FileName;
	}

	private void btnSetLogPath_Click(object sender, EventArgs e)
	{
		FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
		if (folderBrowserDialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
		{
			Settings.Default.logPath = folderBrowserDialog.SelectedPath;
			Settings.Default.Save();
			lblLogPath.Text = folderBrowserDialog.SelectedPath;
		}
	}

	private void chkBxCFlash_CheckedChanged(object sender, EventArgs e)
	{
		if (chkBxCFlash.Checked)
		{
			HmdDevice.IsCFlashEnabled = true;
			chkBxCFlash.Text = "Continuous Flash Enabled";
			MessageBox.Show("Please Connect same model devices... \nFlash devices one by one...");
			chkBxCFlash.BackColor = Color.OrangeRed;
		}
		else
		{
			HmdDevice.IsCFlashEnabled = false;
			chkBxCFlash.Text = "Continuous Flash Disabled";
			chkBxCFlash.BackColor = Color.LightCoral;
			ClearCFlashFiles();
			InitCflashVar();
		}
	}

	private void InitCflashVar()
	{
		HmdDevice.IsCFlashEnabled = false;
		HmdDevice.IsExtractedOnce = false;
		DevicePanel.path = string.Empty;
	}

	private void ClearCFlashFiles()
	{
		try
		{
			if (!string.IsNullOrEmpty(HmdDevice.cFlash.cfBasePath))
			{
				string text = (HmdDevice.cFlash.cfBasePath.EndsWith("\\") ? HmdDevice.cFlash.cfBasePath.Remove(HmdDevice.cFlash.cfBasePath.Length - 1, 1) : HmdDevice.cFlash.cfBasePath);
				text += ".tmp";
				Directory.Move(HmdDevice.cFlash.cfBasePath, text);
				Directory.Delete(text, recursive: true);
			}
		}
		catch
		{
		}
	}

	private async void lkLblUserGuide_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
	{
		string file = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\HMD_Devicekit\\UG_FirmWare.pdf";
		if (!FileUtility.IsFileExists(file))
		{
			await HttpClientDownloadWithProgress.startDownload("UG_FirmWare", ".pdf");
			return;
		}
		DialogResult result = MessageBox.Show("Do you want to open the file?", "Already downloaded!", MessageBoxButtons.YesNo);
		if (result == DialogResult.Yes)
		{
			Process.Start(file);
		}
	}

	private void MultiFirmwareUpdateForm_FormClosed(object sender, FormClosedEventArgs e)
	{
		ClearCFlashFiles();
		InitCflashVar();
		Hide();
		Program.functionSelectForm.Show();
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(hmd_pctool_windows.MultiFirmwareUpdateForm));
		this.btnCanceltAll = new hmd_pctool_windows.Components.GreenButton();
		this.btnClose = new hmd_pctool_windows.Components.GreenButton();
		this.picBoxLogo = new System.Windows.Forms.PictureBox();
		this.panelTest = new System.Windows.Forms.Panel();
		this.btnRunTest = new System.Windows.Forms.Button();
		this.numericUpDownTestTimes = new System.Windows.Forms.NumericUpDown();
		this.btnSelectForAll = new hmd_pctool_windows.Components.GreenButton();
		this.panelLog = new System.Windows.Forms.Panel();
		this.btnSetLogPath = new System.Windows.Forms.Button();
		this.lblsLogPath = new System.Windows.Forms.Label();
		this.lblLogPath = new System.Windows.Forms.Label();
		this.lkLblUserGuide = new System.Windows.Forms.LinkLabel();
		this.panelCenter = new System.Windows.Forms.Panel();
		this.flowLayoutPanelDeviceList = new System.Windows.Forms.FlowLayoutPanel();
		this.chkBxCFlash = new System.Windows.Forms.CheckBox();
		this.labelNoDevice = new System.Windows.Forms.Label();
		((System.ComponentModel.ISupportInitialize)this.picBoxLogo).BeginInit();
		this.panelTest.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.numericUpDownTestTimes).BeginInit();
		this.panelLog.SuspendLayout();
		this.panelCenter.SuspendLayout();
		this.flowLayoutPanelDeviceList.SuspendLayout();
		base.SuspendLayout();
		this.btnCanceltAll.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnCanceltAll.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnCanceltAll.Location = new System.Drawing.Point(501, 514);
		this.btnCanceltAll.Name = "btnCanceltAll";
		this.btnCanceltAll.Size = new System.Drawing.Size(105, 27);
		this.btnCanceltAll.TabIndex = 4;
		this.btnCanceltAll.Text = "Cancel All";
		this.btnCanceltAll.UseVisualStyleBackColor = true;
		this.btnCanceltAll.Click += new System.EventHandler(BtnCanceltAll_Click);
		this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnClose.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnClose.Location = new System.Drawing.Point(631, 514);
		this.btnClose.Name = "btnClose";
		this.btnClose.Size = new System.Drawing.Size(105, 27);
		this.btnClose.TabIndex = 11;
		this.btnClose.Text = "Close";
		this.btnClose.UseVisualStyleBackColor = true;
		this.btnClose.Click += new System.EventHandler(BtnClose_Click);
		this.picBoxLogo.Image = (System.Drawing.Image)resources.GetObject("picBoxLogo.Image");
		this.picBoxLogo.Location = new System.Drawing.Point(60, 22);
		this.picBoxLogo.Name = "picBoxLogo";
		this.picBoxLogo.Size = new System.Drawing.Size(112, 50);
		this.picBoxLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
		this.picBoxLogo.TabIndex = 12;
		this.picBoxLogo.TabStop = false;
		this.panelTest.BackColor = System.Drawing.Color.Red;
		this.panelTest.Controls.Add(this.btnRunTest);
		this.panelTest.Controls.Add(this.numericUpDownTestTimes);
		this.panelTest.Location = new System.Drawing.Point(578, 53);
		this.panelTest.Name = "panelTest";
		this.panelTest.Size = new System.Drawing.Size(162, 28);
		this.panelTest.TabIndex = 18;
		this.btnRunTest.Location = new System.Drawing.Point(56, 1);
		this.btnRunTest.Margin = new System.Windows.Forms.Padding(0);
		this.btnRunTest.Name = "btnRunTest";
		this.btnRunTest.Size = new System.Drawing.Size(105, 26);
		this.btnRunTest.TabIndex = 1;
		this.btnRunTest.Text = "Run Test";
		this.btnRunTest.UseVisualStyleBackColor = true;
		this.btnRunTest.Click += new System.EventHandler(BtnRunTest_Click);
		this.numericUpDownTestTimes.Location = new System.Drawing.Point(3, 3);
		this.numericUpDownTestTimes.Margin = new System.Windows.Forms.Padding(0);
		this.numericUpDownTestTimes.Maximum = new decimal(new int[4] { 999, 0, 0, 0 });
		this.numericUpDownTestTimes.Minimum = new decimal(new int[4] { 1, 0, 0, 0 });
		this.numericUpDownTestTimes.Name = "numericUpDownTestTimes";
		this.numericUpDownTestTimes.Size = new System.Drawing.Size(50, 22);
		this.numericUpDownTestTimes.TabIndex = 0;
		this.numericUpDownTestTimes.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
		this.numericUpDownTestTimes.Value = new decimal(new int[4] { 1, 0, 0, 0 });
		this.btnSelectForAll.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnSelectForAll.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnSelectForAll.Location = new System.Drawing.Point(355, 514);
		this.btnSelectForAll.Name = "btnSelectForAll";
		this.btnSelectForAll.Size = new System.Drawing.Size(125, 27);
		this.btnSelectForAll.TabIndex = 19;
		this.btnSelectForAll.Text = "Select ROM For All";
		this.btnSelectForAll.UseVisualStyleBackColor = true;
		this.btnSelectForAll.Click += new System.EventHandler(btnSelectForAll_Click);
		this.panelLog.BackColor = System.Drawing.Color.White;
		this.panelLog.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.panelLog.Controls.Add(this.btnSetLogPath);
		this.panelLog.Controls.Add(this.lblsLogPath);
		this.panelLog.Controls.Add(this.lblLogPath);
		this.panelLog.Location = new System.Drawing.Point(57, 514);
		this.panelLog.Name = "panelLog";
		this.panelLog.Size = new System.Drawing.Size(276, 27);
		this.panelLog.TabIndex = 21;
		this.btnSetLogPath.Location = new System.Drawing.Point(237, 2);
		this.btnSetLogPath.Margin = new System.Windows.Forms.Padding(0);
		this.btnSetLogPath.Name = "btnSetLogPath";
		this.btnSetLogPath.Size = new System.Drawing.Size(35, 21);
		this.btnSetLogPath.TabIndex = 22;
		this.btnSetLogPath.Text = "...";
		this.btnSetLogPath.UseVisualStyleBackColor = true;
		this.btnSetLogPath.Click += new System.EventHandler(btnSetLogPath_Click);
		this.lblsLogPath.AutoSize = true;
		this.lblsLogPath.Font = new System.Drawing.Font("Calibri", 11.25f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblsLogPath.Location = new System.Drawing.Point(3, 4);
		this.lblsLogPath.Name = "lblsLogPath";
		this.lblsLogPath.Size = new System.Drawing.Size(64, 18);
		this.lblsLogPath.TabIndex = 23;
		this.lblsLogPath.Text = "Log path:";
		this.lblLogPath.AutoEllipsis = true;
		this.lblLogPath.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.lblLogPath.Location = new System.Drawing.Point(64, 6);
		this.lblLogPath.Name = "lblLogPath";
		this.lblLogPath.Size = new System.Drawing.Size(167, 14);
		this.lblLogPath.TabIndex = 21;
		this.lblLogPath.Text = "C:\\Log";
		this.lkLblUserGuide.AutoSize = true;
		this.lkLblUserGuide.Location = new System.Drawing.Point(671, 30);
		this.lkLblUserGuide.Name = "lkLblUserGuide";
		this.lkLblUserGuide.Size = new System.Drawing.Size(68, 14);
		this.lkLblUserGuide.TabIndex = 30;
		this.lkLblUserGuide.TabStop = true;
		this.lkLblUserGuide.Text = "User Guide";
		this.lkLblUserGuide.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(lkLblUserGuide_LinkClicked);
		this.panelCenter.Controls.Add(this.flowLayoutPanelDeviceList);
		this.panelCenter.Location = new System.Drawing.Point(60, 87);
		this.panelCenter.Name = "panelCenter";
		this.panelCenter.Size = new System.Drawing.Size(679, 406);
		this.panelCenter.TabIndex = 17;
		this.flowLayoutPanelDeviceList.AutoScroll = true;
		this.flowLayoutPanelDeviceList.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
		this.flowLayoutPanelDeviceList.BackColor = System.Drawing.Color.Transparent;
		this.flowLayoutPanelDeviceList.Controls.Add(this.chkBxCFlash);
		this.flowLayoutPanelDeviceList.Controls.Add(this.labelNoDevice);
		this.flowLayoutPanelDeviceList.Dock = System.Windows.Forms.DockStyle.Fill;
		this.flowLayoutPanelDeviceList.Location = new System.Drawing.Point(0, 0);
		this.flowLayoutPanelDeviceList.Name = "flowLayoutPanelDeviceList";
		this.flowLayoutPanelDeviceList.Size = new System.Drawing.Size(679, 406);
		this.flowLayoutPanelDeviceList.TabIndex = 0;
		this.chkBxCFlash.AutoSize = true;
		this.chkBxCFlash.BackColor = System.Drawing.Color.LightCoral;
		this.chkBxCFlash.ForeColor = System.Drawing.Color.White;
		this.chkBxCFlash.Location = new System.Drawing.Point(3, 3);
		this.chkBxCFlash.Name = "chkBxCFlash";
		this.chkBxCFlash.Size = new System.Drawing.Size(166, 18);
		this.chkBxCFlash.TabIndex = 100;
		this.chkBxCFlash.Text = "Continous Flash Disabled";
		this.chkBxCFlash.UseVisualStyleBackColor = false;
		this.chkBxCFlash.CheckedChanged += new System.EventHandler(chkBxCFlash_CheckedChanged);
		this.labelNoDevice.BackColor = System.Drawing.Color.Transparent;
		this.labelNoDevice.Location = new System.Drawing.Point(3, 27);
		this.labelNoDevice.Margin = new System.Windows.Forms.Padding(3);
		this.labelNoDevice.Name = "labelNoDevice";
		this.labelNoDevice.Size = new System.Drawing.Size(673, 376);
		this.labelNoDevice.TabIndex = 0;
		this.labelNoDevice.Text = "Please connect device!";
		this.labelNoDevice.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 14f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.Color.White;
		base.ClientSize = new System.Drawing.Size(801, 567);
		base.Controls.Add(this.lkLblUserGuide);
		base.Controls.Add(this.panelLog);
		base.Controls.Add(this.btnSelectForAll);
		base.Controls.Add(this.panelTest);
		base.Controls.Add(this.panelCenter);
		base.Controls.Add(this.picBoxLogo);
		base.Controls.Add(this.btnClose);
		base.Controls.Add(this.btnCanceltAll);
		this.Font = new System.Drawing.Font("Calibri", 9f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		base.MaximizeBox = false;
		base.Name = "MultiFirmwareUpdateForm";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		this.Text = "Firmware Update";
		base.FormClosed += new System.Windows.Forms.FormClosedEventHandler(MultiFirmwareUpdateForm_FormClosed);
		base.Load += new System.EventHandler(MultiFirmwareUpdateForm_Load);
		((System.ComponentModel.ISupportInitialize)this.picBoxLogo).EndInit();
		this.panelTest.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.numericUpDownTestTimes).EndInit();
		this.panelLog.ResumeLayout(false);
		this.panelLog.PerformLayout();
		this.panelCenter.ResumeLayout(false);
		this.flowLayoutPanelDeviceList.ResumeLayout(false);
		this.flowLayoutPanelDeviceList.PerformLayout();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
