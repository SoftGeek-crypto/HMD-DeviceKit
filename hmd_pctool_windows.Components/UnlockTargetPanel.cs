using System;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace hmd_pctool_windows.Components;

public class UnlockTargetPanel : TargetPanel
{
	private string targetSku = string.Empty;

	private StringBuilder logStringBuilder;

	private bool isAutoUnlock;

	private bool isPresent = true;

	private IContainer components = null;

	private Label lblDataCable;

	private Label lblIMEI;

	private Label lblReason;

	private Label lblStatus;

	public bool IsBusy => lblStatus.Text.Equals("Unlocking in progress");

	public UnlockTargetPanel()
	{
		InitializeComponent();
	}

	public UnlockTargetPanel(Device device, int index, bool isAutoUnlock = false)
		: base(device, index)
	{
		InitializeComponent();
		logStringBuilder = new StringBuilder();
		this.isAutoUnlock = isAutoUnlock;
	}

	protected override void TargetPanelEventHandler(object sender, DeviceEventArgs args)
	{
		DeviceEventType eventType = args.EventType;
		Console.WriteLine($"Type={eventType}, what={args.What}, int={args.IntArg}");
		switch (eventType)
		{
		case DeviceEventType.OnReadItem:
		{
			DeviceItemType deviceItemType = (DeviceItemType)args.What;
			Invoke((MethodInvoker)delegate
			{
				if (deviceItemType == DeviceItemType.ReadOnlyImei)
				{
					lblIMEI.Text = args.StringArg;
				}
				else if (NeedToUnlock(args.StringArg))
				{
					device.UnlockFrp();
				}
				else
				{
					lblStatus.Text = "Pass";
				}
			});
			break;
		}
		case DeviceEventType.OnCommandFail:
		case DeviceEventType.OnCommandSuccess:
			lblReason.Text = args.StringArg;
			break;
		case DeviceEventType.UpdateLog:
			break;
		case DeviceEventType.OnUnlockFrpStatus:
			switch ((UnlockStatus)args.IntArg)
			{
			case UnlockStatus.ConnectingInProgress:
				lblStatus.Text = "Connecting in progress";
				break;
			case UnlockStatus.UnlockInProgress:
				lblStatus.Text = "Unlocking in progress";
				break;
			default:
				lblStatus.Text = ((UnlockStatus)args.IntArg).ToString();
				break;
			}
			break;
		case DeviceEventType.OnGetVar:
			break;
		case DeviceEventType.OnFlashStatus:
		case DeviceEventType.OnWriteItem:
			break;
		}
	}

	public void SetAutoUnlock(bool enable)
	{
		isAutoUnlock = enable;
		if (isPresent && enable)
		{
			lblReason.Text = "";
			device.ReadItem(DeviceItemType.AntiTheftStatus);
		}
	}

	public void SetPresent(bool status)
	{
		isPresent = status;
		lblStatus.Invoke((MethodInvoker)delegate
		{
			if (status)
			{
				lblStatus.Text = "Connecting in progress";
				SetAutoUnlock(isAutoUnlock);
			}
			else
			{
				if (lblStatus.Text.Equals("Unlocking in progress"))
				{
					lblStatus.Text = "Fail";
					lblReason.Text = "UE not present";
				}
				else
				{
					lblStatus.Text = "UE not present";
				}
				device.CancelWork();
				device.ExitFastbootProcess();
			}
		});
	}

	private bool NeedToUnlock(string status)
	{
		if (status.Equals(AntiTheftStatus.Enabled.ToString()) || status.Equals(AntiTheftStatus.Triggered.ToString()))
		{
			return true;
		}
		return false;
	}

	private void UnlockTargetPanel_Load(object sender, EventArgs e)
	{
		if (device != null && device is HmdDevice)
		{
			lblIMEI.Text = "-";
			lblStatus.Text = "Connecting in progress";
			lblReason.Text = "";
			lblDataCable.Text = dataCable;
			device.ReadItem(DeviceItemType.ReadOnlyImei);
			if (isAutoUnlock)
			{
				device.ReadItem(DeviceItemType.AntiTheftStatus);
			}
		}
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
		this.lblDataCable = new System.Windows.Forms.Label();
		this.lblIMEI = new System.Windows.Forms.Label();
		this.lblReason = new System.Windows.Forms.Label();
		this.lblStatus = new System.Windows.Forms.Label();
		base.SuspendLayout();
		this.lblDataCable.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.lblDataCable.Location = new System.Drawing.Point(159, 7);
		this.lblDataCable.Margin = new System.Windows.Forms.Padding(5, 5, 0, 0);
		this.lblDataCable.Name = "lblDataCable";
		this.lblDataCable.Size = new System.Drawing.Size(120, 16);
		this.lblDataCable.TabIndex = 4;
		this.lblDataCable.Text = "Device 16";
		this.lblDataCable.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.lblIMEI.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.lblIMEI.Location = new System.Drawing.Point(1, 7);
		this.lblIMEI.Margin = new System.Windows.Forms.Padding(5, 5, 0, 0);
		this.lblIMEI.Name = "lblIMEI";
		this.lblIMEI.Size = new System.Drawing.Size(163, 16);
		this.lblIMEI.TabIndex = 6;
		this.lblIMEI.Text = "9900000XXXXXX7";
		this.lblIMEI.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.lblReason.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.lblReason.Location = new System.Drawing.Point(435, 7);
		this.lblReason.Margin = new System.Windows.Forms.Padding(5, 5, 0, 0);
		this.lblReason.Name = "lblReason";
		this.lblReason.Size = new System.Drawing.Size(235, 15);
		this.lblReason.TabIndex = 7;
		this.lblReason.Text = "Fail to read IMEI";
		this.lblReason.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.lblStatus.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.lblStatus.Location = new System.Drawing.Point(297, 7);
		this.lblStatus.Margin = new System.Windows.Forms.Padding(5, 5, 0, 0);
		this.lblStatus.Name = "lblStatus";
		this.lblStatus.Size = new System.Drawing.Size(135, 15);
		this.lblStatus.TabIndex = 9;
		this.lblStatus.Text = "Connecting in progress";
		this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 12f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.Color.White;
		base.Controls.Add(this.lblStatus);
		base.Controls.Add(this.lblReason);
		base.Controls.Add(this.lblIMEI);
		base.Controls.Add(this.lblDataCable);
		base.Margin = new System.Windows.Forms.Padding(0);
		base.Name = "UnlockTargetPanel";
		base.Size = new System.Drawing.Size(670, 30);
		base.Load += new System.EventHandler(UnlockTargetPanel_Load);
		base.ResumeLayout(false);
	}
}
