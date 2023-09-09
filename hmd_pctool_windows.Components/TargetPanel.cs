using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace hmd_pctool_windows.Components;

public class TargetPanel : UserControl
{
	protected Device device;

	protected string dataCable;

	private IContainer components = null;

	public Device PanelDevice => device;

	public TargetPanel()
	{
		InitializeComponent();
	}

	public TargetPanel(Device device, int index)
	{
		this.device = device;
		if (this.device != null)
		{
			this.device.DeviceEventHandler += TargetPanelEventHandler;
			if (this.device is HmdDevice)
			{
				(this.device as HmdDevice).DataCable = index;
			}
			dataCable = $"Device {index}";
		}
	}

	protected virtual void TargetPanelEventHandler(object sender, DeviceEventArgs args)
	{
	}

	public void Exit()
	{
		UnRegisterEvent();
		device.CancelWork();
	}

	public void UnRegisterEvent()
	{
		if (device != null)
		{
			device.DeviceEventHandler -= TargetPanelEventHandler;
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
		base.SuspendLayout();
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 12f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.Color.White;
		base.Margin = new System.Windows.Forms.Padding(0);
		base.Name = "TargetPanel";
		base.Size = new System.Drawing.Size(670, 30);
		base.ResumeLayout(false);
	}
}
