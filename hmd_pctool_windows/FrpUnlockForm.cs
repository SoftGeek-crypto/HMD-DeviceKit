using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using hmd_pctool_windows.Components;

namespace hmd_pctool_windows;

public class FrpUnlockForm : BorderlessForm
{
	private enum Mode
	{
		UnlockMode,
		InspectionMode
	}

	private delegate void UpdateUI(Control control, object o);

	private List<TargetPanel> targetPanels = new List<TargetPanel>();

	private DeviceManager deviceManager;

	private int dataCableIndex = 0;

	private bool isAutoUnlock = false;

	private Thread unlockStatusChecker;

	private Mode mode = Mode.UnlockMode;

	private IContainer components = null;

	private GreenButton btnClose;

	private PictureBox picBoxLogo;

	private Panel panelUnlock;

	private FlowLayoutPanel unlockLayoutPanel;

	private Label lblIMEITitle;

	private Label lblDataCableTitle;

	private Label lblStatusTitle;

	private Label lblReasonTitle;

	private TabControl tabCtrl;

	private TabPage tabPageUnlock;

	private TabPage tabPageInspection;

	private Label label2;

	private Label label1;

	private Label lblAntiTheft;

	private Label label3;

	private Label label4;

	private Panel panelInspection;

	private FlowLayoutPanel inspectLayoutPanel;

	private TargetPanel inspectTargetPanel1;

	private GreenButton btnOpenLogs;

	private CheckBox chkBoxEnableAuto;

	private Label lblUsrMode;

	public FrpUnlockForm()
	{
		deviceManager = DeviceManager.Instance;
		InitializeComponent();
	}

	private void FrpUnlockForm_Load(object sender, EventArgs e)
	{
		unlockLayoutPanel.VerticalScroll.Visible = true;
		InitForm();
		lblUsrMode.Text = Program.UserMode.ToString() + " Mode";
		StartMonitorDevice();
	}

	private void InitForm()
	{
		chkBoxEnableAuto.Checked = false;
		dataCableIndex = 0;
		CleanPanels();
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
			Device[] devices2 = args.Devices;
			foreach (Device device2 in devices2)
			{
				TargetPanel targetPanel2 = targetPanels.Find((TargetPanel o) => o.PanelDevice.SN.Equals(device2.SN));
				if (targetPanel2 == null)
				{
					AddTargetPanel(device2);
				}
				else if (targetPanel2 is UnlockTargetPanel)
				{
					(targetPanel2 as UnlockTargetPanel).SetPresent(status: true);
				}
			}
			break;
		}
		case DeviceManager.DmEventType.DevicesRemoved:
		{
			if (args.Devices == null)
			{
				break;
			}
			Device[] devices = args.Devices;
			foreach (Device device in devices)
			{
				TargetPanel targetPanel = targetPanels.Find((TargetPanel o) => o.PanelDevice.SN.Equals(device.SN));
				if (targetPanel != null)
				{
					if (mode == Mode.UnlockMode)
					{
						(targetPanel as UnlockTargetPanel).SetPresent(status: false);
						continue;
					}
					targetPanel.Exit();
					RemoveTargetPanel(inspectLayoutPanel, targetPanel);
				}
			}
			break;
		}
		case DeviceManager.DmEventType.MonitoringStarted:
			LogUtility.D("MultiFirmwareUpdateForm", "MonitoringStarted");
			break;
		case DeviceManager.DmEventType.MonitoringStopped:
			LogUtility.D("MultiFirmwareUpdateForm", "MonitoringStopped");
			break;
		}
	}

	private void AddTargetPanel(Device device)
	{
		if (mode == Mode.UnlockMode)
		{
			UnlockTargetPanel unlockTargetPanel = new UnlockTargetPanel(device, ++dataCableIndex, isAutoUnlock);
			targetPanels.Add(unlockTargetPanel);
			AddTargetPanel(unlockLayoutPanel, unlockTargetPanel);
		}
		else if (mode == Mode.InspectionMode)
		{
			InspectTargetPanel inspectTargetPanel = new InspectTargetPanel(device, ++dataCableIndex);
			targetPanels.Add(inspectTargetPanel);
			AddTargetPanel(inspectLayoutPanel, inspectTargetPanel);
		}
	}

	private async void StartMonitorDevice()
	{
		if (deviceManager != null)
		{
			deviceManager.EventHandler += OnDeviceChanged;
			try
			{
				await deviceManager.StartMonitoringAsync();
			}
			catch (Exception ex2)
			{
				Exception ex = ex2;
				LogUtility.E("FrpUnlockForm", ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}
	}

	private void StopMonitorDevice()
	{
		if (deviceManager != null)
		{
			deviceManager.StopMonitoring();
			deviceManager.EventHandler -= OnDeviceChanged;
		}
	}

	private void CleanPanels()
	{
		if (targetPanels != null)
		{
			foreach (TargetPanel targetPanel in targetPanels)
			{
				targetPanel.Exit();
			}
		}
		targetPanels.Clear();
		unlockLayoutPanel.Controls.Clear();
		inspectLayoutPanel.Controls.Clear();
	}

	private void BtnClose_Click(object sender, EventArgs e)
	{
		Close();
	}

	private void AddTargetPanel(Control control, object o)
	{
		if (o != null)
		{
			if (control.InvokeRequired)
			{
				UpdateUI method = AddTargetPanel;
				control.Invoke(method, control, o);
			}
			else
			{
				(control as FlowLayoutPanel).Controls.Add(o as TargetPanel);
			}
		}
	}

	private void RemoveTargetPanel(Control control, object o)
	{
		if (o != null)
		{
			if (control.InvokeRequired)
			{
				UpdateUI method = RemoveTargetPanel;
				control.Invoke(method, control, o);
			}
			else
			{
				targetPanels.Remove(o as TargetPanel);
				(control as FlowLayoutPanel).Controls.Remove(o as TargetPanel);
			}
		}
	}

	private void tabCtrl_SelectedIndexChanged(object sender, EventArgs e)
	{
		mode = (Mode)tabCtrl.SelectedIndex;
		InitForm();
		UpdateExistedDevices();
		if (mode == Mode.UnlockMode)
		{
			chkBoxEnableAuto.Visible = true;
		}
		else
		{
			chkBoxEnableAuto.Visible = false;
		}
	}

	private void UpdateExistedDevices()
	{
		Device[] deviceList = deviceManager.GetDeviceList();
		Device[] array = deviceList;
		foreach (Device device in array)
		{
			if (device.DeviceStatus != DeviceStatus.Offline)
			{
				AddTargetPanel(device);
			}
		}
	}

	private void chkBoxEnableAuto_CheckedChanged(object sender, EventArgs e)
	{
		if (chkBoxEnableAuto.Checked)
		{
			EnableUnlock();
		}
		else
		{
			DisableUnlock();
		}
	}

	private void DisableUnlock()
	{
		SetTargetsAutoUnlock(enable: false);
		AbortUnlockStatusChecker();
		InitForm();
		UpdateExistedDevices();
	}

	private void SetTargetsAutoUnlock(bool enable)
	{
		isAutoUnlock = enable;
		if (targetPanels == null)
		{
			return;
		}
		foreach (TargetPanel targetPanel in targetPanels)
		{
			targetPanel.PanelDevice.CancelWork();
			(targetPanel as UnlockTargetPanel).SetAutoUnlock(enable);
		}
	}

	private void AbortUnlockStatusChecker()
	{
		if (unlockStatusChecker != null)
		{
			unlockStatusChecker.Abort();
		}
	}

	private void EnableUnlock()
	{
		base.Enabled = false;
		SetTargetsAutoUnlock(enable: true);
		unlockStatusChecker = new Thread(CheckUnlockStatus);
		unlockStatusChecker.Start();
	}

	private void CheckUnlockStatus()
	{
		Thread.Sleep(1000);
		while (true)
		{
			bool flag = false;
			lock (targetPanels)
			{
				foreach (TargetPanel targetPanel in targetPanels)
				{
					if (targetPanel is InspectTargetPanel)
					{
						return;
					}
					flag |= (targetPanel as UnlockTargetPanel).IsBusy;
				}
			}
			bool expectEnableStatus = !flag;
			Invoke((MethodInvoker)delegate
			{
				if (base.Enabled != expectEnableStatus)
				{
					base.Enabled = expectEnableStatus;
				}
			});
			Thread.Sleep(300);
		}
	}

	private void FrpUnlockForm_FormClosing(object sender, FormClosingEventArgs e)
	{
		AbortUnlockStatusChecker();
		CleanPanels();
		StopMonitorDevice();
		Program.functionSelectForm.Show();
	}

	private void btnOpenLogs_Click(object sender, EventArgs e)
	{
		string text = null;
		text = ((mode != Mode.InspectionMode) ? OutputXml.UnlockLogPath : OutputXml.InspectLogPath);
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		Process.Start(text);
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(hmd_pctool_windows.FrpUnlockForm));
		this.lblReasonTitle = new System.Windows.Forms.Label();
		this.lblStatusTitle = new System.Windows.Forms.Label();
		this.lblDataCableTitle = new System.Windows.Forms.Label();
		this.lblIMEITitle = new System.Windows.Forms.Label();
		this.panelUnlock = new System.Windows.Forms.Panel();
		this.unlockLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
		this.picBoxLogo = new System.Windows.Forms.PictureBox();
		this.btnClose = new hmd_pctool_windows.Components.GreenButton();
		this.tabCtrl = new System.Windows.Forms.TabControl();
		this.tabPageUnlock = new System.Windows.Forms.TabPage();
		this.tabPageInspection = new System.Windows.Forms.TabPage();
		this.panelInspection = new System.Windows.Forms.Panel();
		this.inspectLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
		this.inspectTargetPanel1 = new hmd_pctool_windows.Components.TargetPanel();
		this.label2 = new System.Windows.Forms.Label();
		this.label1 = new System.Windows.Forms.Label();
		this.lblAntiTheft = new System.Windows.Forms.Label();
		this.label3 = new System.Windows.Forms.Label();
		this.label4 = new System.Windows.Forms.Label();
		this.btnOpenLogs = new hmd_pctool_windows.Components.GreenButton();
		this.chkBoxEnableAuto = new System.Windows.Forms.CheckBox();
		this.lblUsrMode = new System.Windows.Forms.Label();
		this.panelUnlock.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.picBoxLogo).BeginInit();
		this.tabCtrl.SuspendLayout();
		this.tabPageUnlock.SuspendLayout();
		this.tabPageInspection.SuspendLayout();
		this.panelInspection.SuspendLayout();
		this.inspectLayoutPanel.SuspendLayout();
		base.SuspendLayout();
		this.lblReasonTitle.AutoSize = true;
		this.lblReasonTitle.Font = new System.Drawing.Font("Calibri", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblReasonTitle.Location = new System.Drawing.Point(517, 2);
		this.lblReasonTitle.Name = "lblReasonTitle";
		this.lblReasonTitle.Size = new System.Drawing.Size(58, 19);
		this.lblReasonTitle.TabIndex = 22;
		this.lblReasonTitle.Text = "Reason";
		this.lblStatusTitle.AutoSize = true;
		this.lblStatusTitle.Font = new System.Drawing.Font("Calibri", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblStatusTitle.Location = new System.Drawing.Point(342, 2);
		this.lblStatusTitle.Name = "lblStatusTitle";
		this.lblStatusTitle.Size = new System.Drawing.Size(52, 19);
		this.lblStatusTitle.TabIndex = 21;
		this.lblStatusTitle.Text = "Status";
		this.lblDataCableTitle.AutoSize = true;
		this.lblDataCableTitle.Font = new System.Drawing.Font("Calibri", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblDataCableTitle.Location = new System.Drawing.Point(184, 2);
		this.lblDataCableTitle.Name = "lblDataCableTitle";
		this.lblDataCableTitle.Size = new System.Drawing.Size(82, 19);
		this.lblDataCableTitle.TabIndex = 20;
		this.lblDataCableTitle.Text = "Data Cable";
		this.lblIMEITitle.AutoSize = true;
		this.lblIMEITitle.Font = new System.Drawing.Font("Calibri", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblIMEITitle.Location = new System.Drawing.Point(62, 2);
		this.lblIMEITitle.Name = "lblIMEITitle";
		this.lblIMEITitle.Size = new System.Drawing.Size(39, 19);
		this.lblIMEITitle.TabIndex = 19;
		this.lblIMEITitle.Text = "IMEI";
		this.panelUnlock.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.panelUnlock.Controls.Add(this.unlockLayoutPanel);
		this.panelUnlock.Location = new System.Drawing.Point(2, 24);
		this.panelUnlock.Name = "panelUnlock";
		this.panelUnlock.Size = new System.Drawing.Size(679, 306);
		this.panelUnlock.TabIndex = 17;
		this.unlockLayoutPanel.AutoScroll = true;
		this.unlockLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
		this.unlockLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
		this.unlockLayoutPanel.Location = new System.Drawing.Point(0, 0);
		this.unlockLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
		this.unlockLayoutPanel.Name = "unlockLayoutPanel";
		this.unlockLayoutPanel.Size = new System.Drawing.Size(677, 304);
		this.unlockLayoutPanel.TabIndex = 0;
		this.picBoxLogo.Image = (System.Drawing.Image)resources.GetObject("picBoxLogo.Image");
		this.picBoxLogo.Location = new System.Drawing.Point(60, 30);
		this.picBoxLogo.Name = "picBoxLogo";
		this.picBoxLogo.Size = new System.Drawing.Size(112, 50);
		this.picBoxLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
		this.picBoxLogo.TabIndex = 12;
		this.picBoxLogo.TabStop = false;
		this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnClose.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnClose.Location = new System.Drawing.Point(647, 449);
		this.btnClose.Name = "btnClose";
		this.btnClose.Size = new System.Drawing.Size(105, 27);
		this.btnClose.TabIndex = 11;
		this.btnClose.Text = "Close";
		this.btnClose.UseVisualStyleBackColor = true;
		this.btnClose.Click += new System.EventHandler(BtnClose_Click);
		this.tabCtrl.Controls.Add(this.tabPageUnlock);
		this.tabCtrl.Controls.Add(this.tabPageInspection);
		this.tabCtrl.Location = new System.Drawing.Point(60, 86);
		this.tabCtrl.Name = "tabCtrl";
		this.tabCtrl.SelectedIndex = 0;
		this.tabCtrl.Size = new System.Drawing.Size(694, 360);
		this.tabCtrl.TabIndex = 23;
		this.tabCtrl.SelectedIndexChanged += new System.EventHandler(tabCtrl_SelectedIndexChanged);
		this.tabPageUnlock.Controls.Add(this.lblReasonTitle);
		this.tabPageUnlock.Controls.Add(this.lblStatusTitle);
		this.tabPageUnlock.Controls.Add(this.lblDataCableTitle);
		this.tabPageUnlock.Controls.Add(this.lblIMEITitle);
		this.tabPageUnlock.Controls.Add(this.panelUnlock);
		this.tabPageUnlock.Location = new System.Drawing.Point(4, 23);
		this.tabPageUnlock.Name = "tabPageUnlock";
		this.tabPageUnlock.Padding = new System.Windows.Forms.Padding(3);
		this.tabPageUnlock.Size = new System.Drawing.Size(686, 333);
		this.tabPageUnlock.TabIndex = 0;
		this.tabPageUnlock.Text = "Unlock mode";
		this.tabPageUnlock.UseVisualStyleBackColor = true;
		this.tabPageInspection.Controls.Add(this.panelInspection);
		this.tabPageInspection.Controls.Add(this.label2);
		this.tabPageInspection.Controls.Add(this.label1);
		this.tabPageInspection.Controls.Add(this.lblAntiTheft);
		this.tabPageInspection.Controls.Add(this.label3);
		this.tabPageInspection.Controls.Add(this.label4);
		this.tabPageInspection.Location = new System.Drawing.Point(4, 23);
		this.tabPageInspection.Name = "tabPageInspection";
		this.tabPageInspection.Padding = new System.Windows.Forms.Padding(3);
		this.tabPageInspection.Size = new System.Drawing.Size(686, 333);
		this.tabPageInspection.TabIndex = 1;
		this.tabPageInspection.Text = "Inspection mode";
		this.tabPageInspection.UseVisualStyleBackColor = true;
		this.panelInspection.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.panelInspection.Controls.Add(this.inspectLayoutPanel);
		this.panelInspection.Location = new System.Drawing.Point(2, 24);
		this.panelInspection.Name = "panelInspection";
		this.panelInspection.Size = new System.Drawing.Size(679, 306);
		this.panelInspection.TabIndex = 28;
		this.inspectLayoutPanel.AutoScroll = true;
		this.inspectLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
		this.inspectLayoutPanel.Controls.Add(this.inspectTargetPanel1);
		this.inspectLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
		this.inspectLayoutPanel.Location = new System.Drawing.Point(0, 0);
		this.inspectLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
		this.inspectLayoutPanel.Name = "inspectLayoutPanel";
		this.inspectLayoutPanel.Size = new System.Drawing.Size(677, 304);
		this.inspectLayoutPanel.TabIndex = 0;
		this.inspectTargetPanel1.BackColor = System.Drawing.Color.White;
		this.inspectTargetPanel1.Location = new System.Drawing.Point(0, 0);
		this.inspectTargetPanel1.Margin = new System.Windows.Forms.Padding(0);
		this.inspectTargetPanel1.Name = "inspectTargetPanel1";
		this.inspectTargetPanel1.Size = new System.Drawing.Size(670, 30);
		this.inspectTargetPanel1.TabIndex = 0;
		this.label2.AutoSize = true;
		this.label2.Font = new System.Drawing.Font("Calibri", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.label2.Location = new System.Drawing.Point(565, 3);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(42, 19);
		this.label2.TabIndex = 27;
		this.label2.Text = "OEM";
		this.label1.AutoSize = true;
		this.label1.Font = new System.Drawing.Font("Calibri", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.label1.Location = new System.Drawing.Point(436, 3);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(53, 19);
		this.label1.TabIndex = 26;
		this.label1.Text = "Model";
		this.lblAntiTheft.AutoSize = true;
		this.lblAntiTheft.Font = new System.Drawing.Font("Calibri", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblAntiTheft.Location = new System.Drawing.Point(280, 2);
		this.lblAntiTheft.Name = "lblAntiTheft";
		this.lblAntiTheft.Size = new System.Drawing.Size(119, 19);
		this.lblAntiTheft.TabIndex = 25;
		this.lblAntiTheft.Text = "AntiTheft Status";
		this.label3.AutoSize = true;
		this.label3.Font = new System.Drawing.Font("Calibri", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.label3.Location = new System.Drawing.Point(184, 2);
		this.label3.Name = "label3";
		this.label3.Size = new System.Drawing.Size(82, 19);
		this.label3.TabIndex = 24;
		this.label3.Text = "Data Cable";
		this.label4.AutoSize = true;
		this.label4.Font = new System.Drawing.Font("Calibri", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.label4.Location = new System.Drawing.Point(62, 2);
		this.label4.Name = "label4";
		this.label4.Size = new System.Drawing.Size(39, 19);
		this.label4.TabIndex = 23;
		this.label4.Text = "IMEI";
		this.btnOpenLogs.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnOpenLogs.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnOpenLogs.Location = new System.Drawing.Point(534, 449);
		this.btnOpenLogs.Name = "btnOpenLogs";
		this.btnOpenLogs.Size = new System.Drawing.Size(105, 27);
		this.btnOpenLogs.TabIndex = 25;
		this.btnOpenLogs.Text = "Open Log Dir";
		this.btnOpenLogs.UseVisualStyleBackColor = true;
		this.btnOpenLogs.Click += new System.EventHandler(btnOpenLogs_Click);
		this.chkBoxEnableAuto.AutoSize = true;
		this.chkBoxEnableAuto.Location = new System.Drawing.Point(66, 458);
		this.chkBoxEnableAuto.Name = "chkBoxEnableAuto";
		this.chkBoxEnableAuto.Size = new System.Drawing.Size(91, 18);
		this.chkBoxEnableAuto.TabIndex = 27;
		this.chkBoxEnableAuto.Text = "Auto Unlock";
		this.chkBoxEnableAuto.UseVisualStyleBackColor = true;
		this.chkBoxEnableAuto.CheckedChanged += new System.EventHandler(chkBoxEnableAuto_CheckedChanged);
		this.lblUsrMode.Font = new System.Drawing.Font("Calibri", 12f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.lblUsrMode.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
		this.lblUsrMode.Location = new System.Drawing.Point(599, 62);
		this.lblUsrMode.Name = "lblUsrMode";
		this.lblUsrMode.Size = new System.Drawing.Size(153, 18);
		this.lblUsrMode.TabIndex = 28;
		this.lblUsrMode.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 14f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.Color.White;
		base.ClientSize = new System.Drawing.Size(801, 509);
		base.Controls.Add(this.lblUsrMode);
		base.Controls.Add(this.chkBoxEnableAuto);
		base.Controls.Add(this.btnOpenLogs);
		base.Controls.Add(this.tabCtrl);
		base.Controls.Add(this.picBoxLogo);
		base.Controls.Add(this.btnClose);
		this.Font = new System.Drawing.Font("Calibri", 9f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		base.Name = "FrpUnlockForm";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		this.Text = "Unlock FRP";
		base.FormClosing += new System.Windows.Forms.FormClosingEventHandler(FrpUnlockForm_FormClosing);
		base.Load += new System.EventHandler(FrpUnlockForm_Load);
		this.panelUnlock.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.picBoxLogo).EndInit();
		this.tabCtrl.ResumeLayout(false);
		this.tabPageUnlock.ResumeLayout(false);
		this.tabPageUnlock.PerformLayout();
		this.tabPageInspection.ResumeLayout(false);
		this.tabPageInspection.PerformLayout();
		this.panelInspection.ResumeLayout(false);
		this.inspectLayoutPanel.ResumeLayout(false);
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
