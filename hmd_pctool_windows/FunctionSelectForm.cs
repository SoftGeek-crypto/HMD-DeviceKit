using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Security.Principal;
using System.Windows.Forms;
using hmd_pctool_windows.Components;
using hmd_pctool_windows.Properties;
using hmd_pctool_windows.Utils;

namespace hmd_pctool_windows;

public class FunctionSelectForm : BorderlessForm
{
	public class CallBackToBeExecuted
	{
		private FunctionSelectForm functionSelectForm;

		private CommandType commandType;

		public CallBackToBeExecuted(FunctionSelectForm _functionSelectForm, CommandType _commandType)
		{
			functionSelectForm = _functionSelectForm;
			commandType = _commandType;
		}

		public void callback(Device device)
		{
			device.DeviceEventHandler += handler;
			if (commandType == CommandType.RebootEdl)
			{
				functionSelectForm.Enabled = false;
				device.RebootEdl();
			}
			else if (commandType == CommandType.FrpErase)
			{
				functionSelectForm.Enabled = false;
				device.FrpErase();
			}
			else if (commandType == CommandType.LockBootloader)
			{
				functionSelectForm.Enabled = false;
				device.LockBootloader();
			}
			else if (commandType == CommandType.BootToSystem)
			{
				functionSelectForm.Enabled = false;
				device.BootToSystem();
			}
			else if (commandType == CommandType.FactoryResets)
			{
				functionSelectForm.Enabled = false;
				device.FactoryReset();
			}
			void handler(object sender, DeviceEventArgs e)
			{
				CommandType what = (CommandType)e.What;
				switch (e.EventType)
				{
				case DeviceEventType.OnCommandSuccess:
					switch (what)
					{
					case CommandType.RebootEdl:
						functionSelectForm.Invoke((MethodInvoker)delegate
						{
							MessageBox.Show("Switch to EDL mode OK");
							functionSelectForm.Enabled = true;
						});
						break;
					case CommandType.FrpErase:
						functionSelectForm.Invoke((MethodInvoker)delegate
						{
							MessageBox.Show("Erase FRP OK");
							functionSelectForm.Enabled = true;
						});
						break;
					case CommandType.LockBootloader:
						functionSelectForm.Invoke((MethodInvoker)delegate
						{
							MessageBox.Show("Lock BootLoader: OK");
							functionSelectForm.Enabled = true;
						});
						break;
					case CommandType.BootToSystem:
						functionSelectForm.Invoke((MethodInvoker)delegate
						{
							MessageBox.Show("Boot to System: OK");
							functionSelectForm.Enabled = true;
						});
						break;
					case CommandType.FactoryResets:
						functionSelectForm.Invoke((MethodInvoker)delegate
						{
							MessageBox.Show("FactoryReset OK");
							functionSelectForm.Enabled = true;
						});
						break;
					}
					break;
				case DeviceEventType.OnCommandFail:
					switch (what)
					{
					case CommandType.RebootEdl:
						functionSelectForm.Invoke((MethodInvoker)delegate
						{
							MessageBox.Show("fastboot reboot-edl command fail" + Environment.NewLine + e.StringArg);
							functionSelectForm.Enabled = true;
						});
						break;
					case CommandType.FrpErase:
						functionSelectForm.Invoke((MethodInvoker)delegate
						{
							MessageBox.Show("Fail to erase FRP" + Environment.NewLine + e.StringArg);
							functionSelectForm.Enabled = true;
						});
						break;
					case CommandType.LockBootloader:
						functionSelectForm.Invoke((MethodInvoker)delegate
						{
							MessageBox.Show("Fail to Lock Bootloader" + Environment.NewLine + e.StringArg);
							functionSelectForm.Enabled = true;
						});
						break;
					case CommandType.BootToSystem:
						functionSelectForm.Invoke((MethodInvoker)delegate
						{
							MessageBox.Show("Fail: Boot to System" + Environment.NewLine + e.StringArg);
							functionSelectForm.Enabled = true;
						});
						break;
					case CommandType.FactoryResets:
						functionSelectForm.Invoke((MethodInvoker)delegate
						{
							MessageBox.Show("Fail to FactoryReset" + Environment.NewLine + e.StringArg);
							functionSelectForm.Enabled = true;
						});
						break;
					}
					break;
				}
				device.DeviceEventHandler -= handler;
			}
		}
	}

	private bool isFormFirstLoad = true;

	private IContainer components = null;

	private PictureBox picBoxLogo;

	private GreenButton btnEditPhoneData;

	private GreenButton btnEraseFRP;

	private GreenButton btnBootloader;

	private GreenButton btnFactoryReset;

	private GreenButton btnFwUpdate;

	private GreenButton btnEditWallpapered;

	private GreenButton btnFrpUnlock;

	private GreenButton btnRebootEDL;

	private GreenButton btnSimContrl;

	private ToolStrip toolStrip1;

	private ToolStripDropDownButton toolStripDropDownButton1;

	private ToolStripMenuItem downloadAppToolStripMenuItem;

	private ToolStripMenuItem aboutToolStripMenuItem1;

	private ToolStripButton toolStripBtnUpdate;

	private Label LblVersion;

	private ToolStripMenuItem authenticateFQCToolStripMenuItem;

	private GreenButton btnBootDevice;

	public FunctionSelectForm()
	{
		InitializeComponent();
	}

	private async void FunctionSelectForm_Load(object sender, EventArgs e)
	{
		LblVersion.Text = $"v{Assembly.GetExecutingAssembly().GetName().Version.ToString()}";
		Program.UserMode = await AuthenticationFactory.GetAuthenticationHandler().GetRrltUserMode();
		UpdateAvailable();
	}

	private void FunctionSelectForm_FormClosed(object sender, FormClosedEventArgs e)
	{
		Program.loginAzureForm.Close();
	}

	private void btnFwUpdate_Click(object sender, EventArgs e)
	{
		Program.MultiFirmwareUpdateForm = new MultiFirmwareUpdateForm();
		Program.MultiFirmwareUpdateForm.Show();
		Hide();
	}

	private void btnEditPhone_Click(object sender, EventArgs e)
	{
		WindowsPrincipal windowsPrincipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
		if (!windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator))
		{
			MessageBox.Show("Please restart DeviceKit with administrator permissions.");
			return;
		}
		Program.detectDeviceForm = new DetectDeviceForm(new PhoneEditForm());
		Program.detectDeviceForm.Show();
		Hide();
	}

	private void btnEditWallpapered_Click(object sender, EventArgs e)
	{
		Program.detectDeviceForm = new DetectDeviceForm(new WallpaperedEditForm());
		Program.detectDeviceForm.Show();
		Hide();
	}

	private void btnRebootEDL_Click(object sender, EventArgs e)
	{
		CallBackToBeExecuted callBackToBeExecuted = new CallBackToBeExecuted(this, CommandType.RebootEdl);
		Program.detectDeviceForm = new DetectDeviceForm(callBackToBeExecuted);
		Program.detectDeviceForm.Show();
		Hide();
	}

	private void btnEraseFRP_Click(object sender, EventArgs e)
	{
		CallBackToBeExecuted callBackToBeExecuted = new CallBackToBeExecuted(this, CommandType.FrpErase);
		Program.detectDeviceForm = new DetectDeviceForm(callBackToBeExecuted);
		Program.detectDeviceForm.Show();
		Hide();
	}

	private void btnSimContrl_Click(object sender, EventArgs e)
	{
		WindowsPrincipal windowsPrincipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
		if (!windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator))
		{
			MessageBox.Show("Please restart DeviceKit with administrator permissions.");
			return;
		}
		Program.detectDeviceForm = new DetectDeviceForm(new SimControlForm());
		Program.detectDeviceForm.Show();
		Hide();
	}

	private void btnFrpUnlock_Click(object sender, EventArgs e)
	{
		if (Program.UserMode == RrltUserMode.None)
		{
			MessageBox.Show("Your account does not meet the requirements to use this feature. \nPlease contact the HMD technical support.", "DeviceKit");
			return;
		}
		Program.FrpUnlockForm = new FrpUnlockForm();
		Program.FrpUnlockForm.Show();
		Hide();
	}

	private void BtnFactoryReset_Click(object sender, EventArgs e)
	{
		CallBackToBeExecuted callBackToBeExecuted = new CallBackToBeExecuted(this, CommandType.FactoryResets);
		Program.detectDeviceForm = new DetectDeviceForm(callBackToBeExecuted);
		Program.detectDeviceForm.Show();
		Hide();
	}

	private void btnBootloader_Click(object sender, EventArgs e)
	{
		CallBackToBeExecuted callBackToBeExecuted = new CallBackToBeExecuted(this, CommandType.LockBootloader);
		Program.detectDeviceForm = new DetectDeviceForm(callBackToBeExecuted);
		Program.detectDeviceForm.Show();
		Hide();
	}

	private void btnBootDevice_Click(object sender, EventArgs e)
	{
		CallBackToBeExecuted callBackToBeExecuted = new CallBackToBeExecuted(this, CommandType.BootToSystem);
		Program.detectDeviceForm = new DetectDeviceForm(callBackToBeExecuted);
		Program.detectDeviceForm.Show();
		Hide();
	}

	private async void downloadAppToolStripMenuItem_Click(object sender, EventArgs e)
	{
		string file = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\HMD_Devicekit\\FQC_PACKAGE.zip";
		if (!FileUtility.IsFileExists(file))
		{
			await HttpClientDownloadWithProgress.startDownload("FQC_PACKAGE", ".zip");
			return;
		}
		DialogResult result = MessageBox.Show("Do you want to open the file?", "Already downloaded!", MessageBoxButtons.YesNo);
		if (result == DialogResult.Yes)
		{
			Process.Start(file);
		}
	}

	private void aboutToolStripMenuItem1_Click(object sender, EventArgs e)
	{
		MessageBox.Show("HMD Device Diagnostic app will help to diagnose the different components of the HMD mobile phones", "HMD Device Diagnostic");
	}

	private async void UpdateAvailable()
	{
		ServerResponse res = await AzureNativeClient.Instance.IsUpdateAvailable(Assembly.GetExecutingAssembly().GetName().Version.ToString());
		if (isFormFirstLoad && res.IsSuccessed)
		{
			string args = Assembly.GetExecutingAssembly().GetName().Version.ToString() + " " + AzureNativeClient.token + " " + res.Message;
			DialogResult result = MessageBox.Show("Click ok to Check Details!!!", "Update Available", MessageBoxButtons.OKCancel);
			if (result == DialogResult.OK)
			{
				ProcessStartInfo startInfo = new ProcessStartInfo("UpdaterApp.exe")
				{
					WorkingDirectory = Application.StartupPath,
					Arguments = args
				};
				Process.Start(startInfo);
				isFormFirstLoad = false;
			}
			else
			{
				toolStripBtnUpdate.Visible = true;
			}
		}
		else
		{
			toolStripBtnUpdate.Visible = false;
		}
	}

	private void toolStripBtnUpdate_Click(object sender, EventArgs e)
	{
		UpdateAvailable();
	}

	private void LblVersion_Click(object sender, EventArgs e)
	{
		LogUtility.D("Debug", ": Clicked versionlabel");
	}

	private void authenticateFQCToolStripMenuItem_Click(object sender, EventArgs e)
	{
		FqcAuthForm fqcAuthForm = new FqcAuthForm();
		fqcAuthForm.Show();
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(hmd_pctool_windows.FunctionSelectForm));
		this.LblVersion = new System.Windows.Forms.Label();
		this.toolStrip1 = new System.Windows.Forms.ToolStrip();
		this.toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
		this.downloadAppToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.authenticateFQCToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.aboutToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripBtnUpdate = new System.Windows.Forms.ToolStripButton();
		this.picBoxLogo = new System.Windows.Forms.PictureBox();
		this.btnEditPhoneData = new hmd_pctool_windows.Components.GreenButton();
		this.btnEraseFRP = new hmd_pctool_windows.Components.GreenButton();
		this.btnBootloader = new hmd_pctool_windows.Components.GreenButton();
		this.btnFactoryReset = new hmd_pctool_windows.Components.GreenButton();
		this.btnFwUpdate = new hmd_pctool_windows.Components.GreenButton();
		this.btnEditWallpapered = new hmd_pctool_windows.Components.GreenButton();
		this.btnFrpUnlock = new hmd_pctool_windows.Components.GreenButton();
		this.btnRebootEDL = new hmd_pctool_windows.Components.GreenButton();
		this.btnSimContrl = new hmd_pctool_windows.Components.GreenButton();
		this.btnBootDevice = new hmd_pctool_windows.Components.GreenButton();
		this.toolStrip1.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.picBoxLogo).BeginInit();
		base.SuspendLayout();
		this.LblVersion.AutoSize = true;
		this.LblVersion.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.LblVersion.Location = new System.Drawing.Point(439, 419);
		this.LblVersion.Name = "LblVersion";
		this.LblVersion.Size = new System.Drawing.Size(84, 24);
		this.LblVersion.TabIndex = 26;
		this.LblVersion.Text = "v1.0.19.0";
		this.LblVersion.Click += new System.EventHandler(LblVersion_Click);
		this.toolStrip1.BackColor = System.Drawing.Color.FromArgb(65, 214, 171);
		this.toolStrip1.Dock = System.Windows.Forms.DockStyle.Bottom;
		this.toolStrip1.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Bold);
		this.toolStrip1.GripMargin = new System.Windows.Forms.Padding(0, 2, 0, 2);
		this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
		this.toolStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
		this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[2] { this.toolStripDropDownButton1, this.toolStripBtnUpdate });
		this.toolStrip1.Location = new System.Drawing.Point(0, 454);
		this.toolStrip1.Name = "toolStrip1";
		this.toolStrip1.Padding = new System.Windows.Forms.Padding(2, 0, 2, 0);
		this.toolStrip1.Size = new System.Drawing.Size(535, 33);
		this.toolStrip1.TabIndex = 25;
		this.toolStrip1.Text = "toolStrip1";
		this.toolStripDropDownButton1.AutoSize = false;
		this.toolStripDropDownButton1.BackColor = System.Drawing.Color.Transparent;
		this.toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[3] { this.downloadAppToolStripMenuItem, this.authenticateFQCToolStripMenuItem, this.aboutToolStripMenuItem1 });
		this.toolStripDropDownButton1.Font = new System.Drawing.Font("Calibri", 9.75f);
		this.toolStripDropDownButton1.Image = (System.Drawing.Image)resources.GetObject("toolStripDropDownButton1.Image");
		this.toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
		this.toolStripDropDownButton1.Name = "toolStripDropDownButton1";
		this.toolStripDropDownButton1.ShowDropDownArrow = false;
		this.toolStripDropDownButton1.Size = new System.Drawing.Size(176, 28);
		this.toolStripDropDownButton1.Text = "HMD Device Diag App";
		this.toolStripDropDownButton1.TextDirection = System.Windows.Forms.ToolStripTextDirection.Horizontal;
		this.toolStripDropDownButton1.ToolTipText = "Click to Download Diagnostic App";
		this.downloadAppToolStripMenuItem.Image = (System.Drawing.Image)resources.GetObject("downloadAppToolStripMenuItem.Image");
		this.downloadAppToolStripMenuItem.Name = "downloadAppToolStripMenuItem";
		this.downloadAppToolStripMenuItem.Size = new System.Drawing.Size(256, 34);
		this.downloadAppToolStripMenuItem.Text = "Download File";
		this.downloadAppToolStripMenuItem.Click += new System.EventHandler(downloadAppToolStripMenuItem_Click);
		this.authenticateFQCToolStripMenuItem.Image = hmd_pctool_windows.Properties.Resources.authenticate_icon;
		this.authenticateFQCToolStripMenuItem.Name = "authenticateFQCToolStripMenuItem";
		this.authenticateFQCToolStripMenuItem.Size = new System.Drawing.Size(256, 34);
		this.authenticateFQCToolStripMenuItem.Text = "Authenticate FQC";
		this.authenticateFQCToolStripMenuItem.Click += new System.EventHandler(authenticateFQCToolStripMenuItem_Click);
		this.aboutToolStripMenuItem1.Image = (System.Drawing.Image)resources.GetObject("aboutToolStripMenuItem1.Image");
		this.aboutToolStripMenuItem1.Name = "aboutToolStripMenuItem1";
		this.aboutToolStripMenuItem1.Size = new System.Drawing.Size(256, 34);
		this.aboutToolStripMenuItem1.Text = "About";
		this.aboutToolStripMenuItem1.Click += new System.EventHandler(aboutToolStripMenuItem1_Click);
		this.toolStripBtnUpdate.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
		this.toolStripBtnUpdate.AutoSize = false;
		this.toolStripBtnUpdate.Font = new System.Drawing.Font("Calibri", 9.75f);
		this.toolStripBtnUpdate.ForeColor = System.Drawing.SystemColors.ControlText;
		this.toolStripBtnUpdate.Image = (System.Drawing.Image)resources.GetObject("toolStripBtnUpdate.Image");
		this.toolStripBtnUpdate.ImageTransparentColor = System.Drawing.Color.Magenta;
		this.toolStripBtnUpdate.Name = "toolStripBtnUpdate";
		this.toolStripBtnUpdate.Size = new System.Drawing.Size(135, 28);
		this.toolStripBtnUpdate.Text = "Update Available";
		this.toolStripBtnUpdate.ToolTipText = "Click to get details";
		this.toolStripBtnUpdate.Visible = false;
		this.toolStripBtnUpdate.Click += new System.EventHandler(toolStripBtnUpdate_Click);
		this.picBoxLogo.Image = (System.Drawing.Image)resources.GetObject("picBoxLogo.Image");
		this.picBoxLogo.Location = new System.Drawing.Point(26, 29);
		this.picBoxLogo.Name = "picBoxLogo";
		this.picBoxLogo.Size = new System.Drawing.Size(112, 50);
		this.picBoxLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
		this.picBoxLogo.TabIndex = 24;
		this.picBoxLogo.TabStop = false;
		this.btnEditPhoneData.BackColor = System.Drawing.Color.White;
		this.btnEditPhoneData.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnEditPhoneData.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnEditPhoneData.Location = new System.Drawing.Point(360, 105);
		this.btnEditPhoneData.Name = "btnEditPhoneData";
		this.btnEditPhoneData.Size = new System.Drawing.Size(143, 55);
		this.btnEditPhoneData.TabIndex = 19;
		this.btnEditPhoneData.TabStop = false;
		this.btnEditPhoneData.Text = "Edit phone data";
		this.btnEditPhoneData.UseVisualStyleBackColor = false;
		this.btnEditPhoneData.Click += new System.EventHandler(btnEditPhone_Click);
		this.btnEraseFRP.BackColor = System.Drawing.Color.White;
		this.btnEraseFRP.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnEraseFRP.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnEraseFRP.Location = new System.Drawing.Point(194, 189);
		this.btnEraseFRP.Name = "btnEraseFRP";
		this.btnEraseFRP.Size = new System.Drawing.Size(143, 55);
		this.btnEraseFRP.TabIndex = 17;
		this.btnEraseFRP.TabStop = false;
		this.btnEraseFRP.Text = "FRP erase";
		this.btnEraseFRP.UseVisualStyleBackColor = false;
		this.btnEraseFRP.Click += new System.EventHandler(btnEraseFRP_Click);
		this.btnBootloader.BackColor = System.Drawing.Color.White;
		this.btnBootloader.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnBootloader.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnBootloader.Location = new System.Drawing.Point(360, 274);
		this.btnBootloader.Name = "btnBootloader";
		this.btnBootloader.Size = new System.Drawing.Size(143, 55);
		this.btnBootloader.TabIndex = 23;
		this.btnBootloader.TabStop = false;
		this.btnBootloader.Text = "Lock Bootloader";
		this.btnBootloader.UseVisualStyleBackColor = false;
		this.btnBootloader.Click += new System.EventHandler(btnBootloader_Click);
		this.btnFactoryReset.BackColor = System.Drawing.Color.White;
		this.btnFactoryReset.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnFactoryReset.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnFactoryReset.Location = new System.Drawing.Point(360, 189);
		this.btnFactoryReset.Name = "btnFactoryReset";
		this.btnFactoryReset.Size = new System.Drawing.Size(143, 55);
		this.btnFactoryReset.TabIndex = 21;
		this.btnFactoryReset.TabStop = false;
		this.btnFactoryReset.Text = "Factory reset";
		this.btnFactoryReset.UseVisualStyleBackColor = false;
		this.btnFactoryReset.Click += new System.EventHandler(BtnFactoryReset_Click);
		this.btnFwUpdate.BackColor = System.Drawing.Color.White;
		this.btnFwUpdate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnFwUpdate.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnFwUpdate.Location = new System.Drawing.Point(26, 105);
		this.btnFwUpdate.Name = "btnFwUpdate";
		this.btnFwUpdate.Size = new System.Drawing.Size(143, 55);
		this.btnFwUpdate.TabIndex = 14;
		this.btnFwUpdate.TabStop = false;
		this.btnFwUpdate.Text = "Firmware update";
		this.btnFwUpdate.UseVisualStyleBackColor = false;
		this.btnFwUpdate.Click += new System.EventHandler(btnFwUpdate_Click);
		this.btnEditWallpapered.BackColor = System.Drawing.Color.White;
		this.btnEditWallpapered.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnEditWallpapered.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnEditWallpapered.Location = new System.Drawing.Point(194, 105);
		this.btnEditWallpapered.Name = "btnEditWallpapered";
		this.btnEditWallpapered.Size = new System.Drawing.Size(143, 55);
		this.btnEditWallpapered.TabIndex = 18;
		this.btnEditWallpapered.TabStop = false;
		this.btnEditWallpapered.Text = "Edit wallpaper/sku ID";
		this.btnEditWallpapered.UseVisualStyleBackColor = false;
		this.btnEditWallpapered.Click += new System.EventHandler(btnEditWallpapered_Click);
		this.btnFrpUnlock.BackColor = System.Drawing.Color.White;
		this.btnFrpUnlock.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnFrpUnlock.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnFrpUnlock.Location = new System.Drawing.Point(194, 274);
		this.btnFrpUnlock.Name = "btnFrpUnlock";
		this.btnFrpUnlock.Size = new System.Drawing.Size(143, 55);
		this.btnFrpUnlock.TabIndex = 22;
		this.btnFrpUnlock.TabStop = false;
		this.btnFrpUnlock.Text = "UE unlock";
		this.btnFrpUnlock.UseVisualStyleBackColor = false;
		this.btnFrpUnlock.Click += new System.EventHandler(btnFrpUnlock_Click);
		this.btnRebootEDL.BackColor = System.Drawing.Color.White;
		this.btnRebootEDL.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnRebootEDL.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnRebootEDL.Location = new System.Drawing.Point(26, 189);
		this.btnRebootEDL.Name = "btnRebootEDL";
		this.btnRebootEDL.Size = new System.Drawing.Size(143, 55);
		this.btnRebootEDL.TabIndex = 16;
		this.btnRebootEDL.TabStop = false;
		this.btnRebootEDL.Text = "Reboot EDL mode";
		this.btnRebootEDL.UseVisualStyleBackColor = false;
		this.btnRebootEDL.Click += new System.EventHandler(btnRebootEDL_Click);
		this.btnSimContrl.BackColor = System.Drawing.Color.White;
		this.btnSimContrl.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnSimContrl.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnSimContrl.Location = new System.Drawing.Point(26, 274);
		this.btnSimContrl.Name = "btnSimContrl";
		this.btnSimContrl.Size = new System.Drawing.Size(143, 55);
		this.btnSimContrl.TabIndex = 15;
		this.btnSimContrl.TabStop = false;
		this.btnSimContrl.Text = "SIM lock/unlock";
		this.btnSimContrl.UseVisualStyleBackColor = false;
		this.btnSimContrl.Click += new System.EventHandler(btnSimContrl_Click);
		this.btnBootDevice.BackColor = System.Drawing.Color.White;
		this.btnBootDevice.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnBootDevice.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnBootDevice.Location = new System.Drawing.Point(26, 360);
		this.btnBootDevice.Name = "btnBootDevice";
		this.btnBootDevice.Size = new System.Drawing.Size(143, 55);
		this.btnBootDevice.TabIndex = 27;
		this.btnBootDevice.TabStop = false;
		this.btnBootDevice.Text = "Boot to System";
		this.btnBootDevice.UseVisualStyleBackColor = false;
		this.btnBootDevice.Click += new System.EventHandler(btnBootDevice_Click);
		base.AutoScaleDimensions = new System.Drawing.SizeF(9f, 22f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.Color.White;
		base.ClientSize = new System.Drawing.Size(535, 487);
		base.Controls.Add(this.btnBootDevice);
		base.Controls.Add(this.LblVersion);
		base.Controls.Add(this.toolStrip1);
		base.Controls.Add(this.picBoxLogo);
		base.Controls.Add(this.btnEditPhoneData);
		base.Controls.Add(this.btnEraseFRP);
		base.Controls.Add(this.btnBootloader);
		base.Controls.Add(this.btnFactoryReset);
		base.Controls.Add(this.btnFwUpdate);
		base.Controls.Add(this.btnEditWallpapered);
		base.Controls.Add(this.btnFrpUnlock);
		base.Controls.Add(this.btnRebootEDL);
		base.Controls.Add(this.btnSimContrl);
		this.Font = new System.Drawing.Font("Calibri", 9f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
		base.MaximizeBox = false;
		base.Name = "FunctionSelectForm";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		this.Text = "HMD DeviceKit ";
		base.FormClosed += new System.Windows.Forms.FormClosedEventHandler(FunctionSelectForm_FormClosed);
		base.Load += new System.EventHandler(FunctionSelectForm_Load);
		this.toolStrip1.ResumeLayout(false);
		this.toolStrip1.PerformLayout();
		((System.ComponentModel.ISupportInitialize)this.picBoxLogo).EndInit();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
