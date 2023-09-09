using System;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace hmd_pctool_windows.Components;

public class InspectTargetPanel : TargetPanel
{
	private string targetSku = string.Empty;

	private StringBuilder logStringBuilder;

	private IContainer components = null;

	private Label lblDataCable;

	private Label lblIMEI;

	private Label lblModel;

	private Label lblStatus;

	private Label lblOEM;

	public InspectTargetPanel()
	{
		InitializeComponent();
	}

	public InspectTargetPanel(Device device, int index)
		: base(device, index)
	{
		InitializeComponent();
		logStringBuilder = new StringBuilder();
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
				else if (deviceItemType == DeviceItemType.AntiTheftStatus)
				{
					lblStatus.Text = args.StringArg;
				}
				if (deviceItemType == DeviceItemType.Model)
				{
					lblModel.Text = args.StringArg;
				}
			});
			break;
		}
		case DeviceEventType.OnCommandFail:
		case DeviceEventType.OnCommandSuccess:
			break;
		case DeviceEventType.UpdateLog:
			break;
		case DeviceEventType.OnUnlockFrpStatus:
			break;
		case DeviceEventType.OnGetVar:
			break;
		case DeviceEventType.OnFlashStatus:
		case DeviceEventType.OnWriteItem:
			break;
		}
	}

	private void UnlockTargetPanel_Load(object sender, EventArgs e)
	{
		if (device != null && device is HmdDevice)
		{
			lblIMEI.Text = "-";
			lblStatus.Text = "-";
			lblModel.Text = "-";
			lblDataCable.Text = dataCable;
			device.ReadItem(DeviceItemType.ReadOnlyImei);
			device.ReadItem(DeviceItemType.AntiTheftStatus);
			device.ReadItem(DeviceItemType.Model);
			if (device is HmdDevice)
			{
				(device as HmdDevice).ExportInspectLog();
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
		this.lblModel = new System.Windows.Forms.Label();
		this.lblStatus = new System.Windows.Forms.Label();
		this.lblOEM = new System.Windows.Forms.Label();
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
		this.lblModel.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.lblModel.Location = new System.Drawing.Point(419, 7);
		this.lblModel.Margin = new System.Windows.Forms.Padding(5, 5, 0, 0);
		this.lblModel.Name = "lblModel";
		this.lblModel.Size = new System.Drawing.Size(86, 15);
		this.lblModel.TabIndex = 7;
		this.lblModel.Text = "Nokia 3 V";
		this.lblModel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.lblStatus.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.lblStatus.Location = new System.Drawing.Point(297, 7);
		this.lblStatus.Margin = new System.Windows.Forms.Padding(5, 5, 0, 0);
		this.lblStatus.Name = "lblStatus";
		this.lblStatus.Size = new System.Drawing.Size(69, 15);
		this.lblStatus.TabIndex = 9;
		this.lblStatus.Text = "Enabled";
		this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.lblOEM.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.lblOEM.Location = new System.Drawing.Point(544, 7);
		this.lblOEM.Margin = new System.Windows.Forms.Padding(5, 5, 0, 0);
		this.lblOEM.Name = "lblOEM";
		this.lblOEM.Size = new System.Drawing.Size(86, 15);
		this.lblOEM.TabIndex = 10;
		this.lblOEM.Text = "HMD Global";
		this.lblOEM.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 12f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.Color.White;
		base.Controls.Add(this.lblOEM);
		base.Controls.Add(this.lblStatus);
		base.Controls.Add(this.lblModel);
		base.Controls.Add(this.lblIMEI);
		base.Controls.Add(this.lblDataCable);
		base.Margin = new System.Windows.Forms.Padding(0);
		base.Name = "InspectTargetPanel";
		base.Size = new System.Drawing.Size(670, 30);
		base.Load += new System.EventHandler(UnlockTargetPanel_Load);
		base.ResumeLayout(false);
	}
}
