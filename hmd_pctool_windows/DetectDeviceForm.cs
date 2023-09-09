using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using hmd_pctool_windows.Components;

namespace hmd_pctool_windows;

public class DetectDeviceForm : BorderlessForm
{
	private Form nextForm = null;

	private FunctionSelectForm.CallBackToBeExecuted callBackToBeExecuted = null;

	private readonly DeviceManager deviceManager = DeviceManager.Instance;

	private Device device = null;

	private IContainer components = null;

	private GreenButton btnNext;

	private Label lblNotification;

	private GreenButton btnCancel;

	private PictureBox picBoxLogo;

	public DetectDeviceForm()
	{
		InitializeComponent();
	}

	private void DeviceManager_EventHandler(object sender, DmEventArgs e)
	{
		if (e.EventType != DeviceManager.DmEventType.DevicesAdded || e.Devices == null)
		{
			return;
		}
		Device[] devices = e.Devices;
		foreach (Device device in devices)
		{
			if (device.DeviceStatus == DeviceStatus.Online)
			{
				Console.WriteLine(device.SN);
				this.device = device;
			}
		}
		Invoke((MethodInvoker)delegate
		{
			lblNotification.Text = "Please click \"Next\" to continue...";
			btnNext.Enabled = true;
		});
		deviceManager.StopMonitoring();
	}

	public DetectDeviceForm(Form nextForm)
	{
		this.nextForm = nextForm;
		InitializeComponent();
	}

	public DetectDeviceForm(FunctionSelectForm.CallBackToBeExecuted _callBackToBeExecuted)
	{
		callBackToBeExecuted = _callBackToBeExecuted;
		InitializeComponent();
	}

	private void DetectDeviceForm_Load(object sender, EventArgs e)
	{
		btnNext.Enabled = false;
		deviceManager.EventHandler += DeviceManager_EventHandler;
		StartMonitoringAsync();
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
			LogUtility.E("DetectDeviceForm", ex.Message + Environment.NewLine + ex.StackTrace);
		}
	}

	private void finish()
	{
		deviceManager.StopMonitoring();
		deviceManager.EventHandler -= DeviceManager_EventHandler;
		Program.functionSelectForm.Show();
		Close();
	}

	private void btnCancel_Click(object sender, EventArgs e)
	{
		finish();
	}

	private void btnNext_Click(object sender, EventArgs e)
	{
		if (nextForm != null)
		{
			if (nextForm is PhoneEditForm)
			{
				((PhoneEditForm)nextForm).SetTargetDevice(device);
			}
			else if (nextForm is SimControlForm)
			{
				((SimControlForm)nextForm).SetTargetDevice(device);
			}
			else if (nextForm is WallpaperedEditForm)
			{
				((WallpaperedEditForm)nextForm).SetTargetDevice(device);
			}
			nextForm.Show();
		}
		if (callBackToBeExecuted != null)
		{
			callBackToBeExecuted.callback(device);
			Program.functionSelectForm.Show();
		}
		deviceManager.EventHandler -= DeviceManager_EventHandler;
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(hmd_pctool_windows.DetectDeviceForm));
		this.btnNext = new hmd_pctool_windows.Components.GreenButton();
		this.lblNotification = new System.Windows.Forms.Label();
		this.btnCancel = new hmd_pctool_windows.Components.GreenButton();
		this.picBoxLogo = new System.Windows.Forms.PictureBox();
		((System.ComponentModel.ISupportInitialize)this.picBoxLogo).BeginInit();
		base.SuspendLayout();
		this.btnNext.BackColor = System.Drawing.Color.White;
		this.btnNext.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnNext.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnNext.Location = new System.Drawing.Point(90, 158);
		this.btnNext.Name = "btnNext";
		this.btnNext.Size = new System.Drawing.Size(105, 27);
		this.btnNext.TabIndex = 4;
		this.btnNext.Text = "Next";
		this.btnNext.UseVisualStyleBackColor = false;
		this.btnNext.Click += new System.EventHandler(btnNext_Click);
		this.lblNotification.AutoSize = true;
		this.lblNotification.BackColor = System.Drawing.Color.Transparent;
		this.lblNotification.Font = new System.Drawing.Font("Arial", 9f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.lblNotification.ForeColor = System.Drawing.Color.Black;
		this.lblNotification.Location = new System.Drawing.Point(41, 94);
		this.lblNotification.Name = "lblNotification";
		this.lblNotification.Size = new System.Drawing.Size(168, 15);
		this.lblNotification.TabIndex = 5;
		this.lblNotification.Text = "Please connect your phone ...";
		this.lblNotification.TextAlign = System.Drawing.ContentAlignment.TopCenter;
		this.btnCancel.BackColor = System.Drawing.Color.White;
		this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
		this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnCancel.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnCancel.Location = new System.Drawing.Point(225, 158);
		this.btnCancel.Name = "btnCancel";
		this.btnCancel.Size = new System.Drawing.Size(105, 27);
		this.btnCancel.TabIndex = 11;
		this.btnCancel.Text = "Cancel";
		this.btnCancel.UseVisualStyleBackColor = false;
		this.btnCancel.Click += new System.EventHandler(btnCancel_Click);
		this.picBoxLogo.Image = (System.Drawing.Image)resources.GetObject("picBoxLogo.Image");
		this.picBoxLogo.Location = new System.Drawing.Point(21, 12);
		this.picBoxLogo.Name = "picBoxLogo";
		this.picBoxLogo.Size = new System.Drawing.Size(112, 50);
		this.picBoxLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
		this.picBoxLogo.TabIndex = 13;
		this.picBoxLogo.TabStop = false;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 14f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.Color.White;
		base.CancelButton = this.btnCancel;
		base.ClientSize = new System.Drawing.Size(403, 217);
		base.Controls.Add(this.picBoxLogo);
		base.Controls.Add(this.lblNotification);
		base.Controls.Add(this.btnCancel);
		base.Controls.Add(this.btnNext);
		this.Font = new System.Drawing.Font("Calibri", 9f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		base.Name = "DetectDeviceForm";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		this.Text = "Detect device";
		base.Load += new System.EventHandler(DetectDeviceForm_Load);
		((System.ComponentModel.ISupportInitialize)this.picBoxLogo).EndInit();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
