using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using hmd_pctool_windows.Components;
using hmd_pctool_windows.Utils;

namespace hmd_pctool_windows;

public class SimControlForm : BorderlessForm
{
	private enum OperationMode
	{
		Lock,
		Unlock
	}

	private Device device = null;

	private OperationMode opMode = OperationMode.Lock;

	private IContainer components = null;

	private Label slblSeletFile;

	private YelloButton btnExit;

	private Label slblSIM2;

	private TextBox txtBoxPIN2;

	private Label slblWaitingMsg;

	private TextBox txtBoxFilePath;

	private YelloButton btnSelectFile;

	private Label slblSIM1;

	private TextBox txtBoxPIN1;

	private Panel panelAfter;

	private YelloButton btnStart;

	private Label slblLock;

	private Label slblUnlock;

	private LinkLabel lkLblUserGuide;

	public SimControlForm()
	{
		InitializeComponent();
	}

	private void Device_DeviceEventHandler(object sender, DeviceEventArgs e)
	{
		CommandType what = (CommandType)e.What;
		switch (e.EventType)
		{
		case DeviceEventType.OnCommandSuccess:
			setWaitingMessage(enable: false);
			switch (what)
			{
			case CommandType.SimLock:
				MessageBox.Show("OK" + Environment.NewLine + e.StringArg);
				break;
			case CommandType.SimUnlock:
				MessageBox.Show("OK");
				break;
			}
			break;
		case DeviceEventType.OnCommandFail:
			setWaitingMessage(enable: false);
			if (what == CommandType.StartSimlock || what == CommandType.StopPhoneEditSimlock)
			{
				Invoke((MethodInvoker)delegate
				{
					MessageBox.Show("Fail to get simlock permission\nReason : " + e.StringArg);
					exit();
				});
				break;
			}
			switch (what)
			{
			case CommandType.SimLock:
				Invoke((MethodInvoker)delegate
				{
					MessageBox.Show("Fail " + Environment.NewLine + e.StringArg);
				});
				break;
			case CommandType.SimUnlock:
				Invoke((MethodInvoker)delegate
				{
					MessageBox.Show("Fail " + Environment.NewLine + e.StringArg);
				});
				break;
			}
			break;
		}
	}

	private void btnExit_Click(object sender, EventArgs e)
	{
		exit();
	}

	private void exit()
	{
		if (device != null)
		{
			device.StopPhoneEditSimLock();
			device.DeviceEventHandler -= Device_DeviceEventHandler;
			device = null;
		}
		Program.functionSelectForm.Show();
		Close();
	}

	private void SimControlForm_Load(object sender, EventArgs e)
	{
		device.StartSimLock();
		setWaitingMessage(enable: true);
	}

	private bool isAdbConnected()
	{
		Process process = new Process();
		process.StartInfo.FileName = "adb.exe";
		process.StartInfo.Arguments = "devices";
		process.StartInfo.UseShellExecute = false;
		process.StartInfo.RedirectStandardOutput = true;
		process.StartInfo.CreateNoWindow = true;
		process.StartInfo.RedirectStandardError = true;
		process.Start();
		string text = process.StandardOutput.ReadToEnd();
		if (text.EndsWith("List of devices attached"))
		{
			return false;
		}
		return true;
	}

	private void setWaitingMessage(bool enable)
	{
		panelAfter.Visible = !enable;
		btnStart.Visible = !enable;
		slblLock.Visible = !enable;
		slblUnlock.Visible = !enable;
		slblLock_Click(null, null);
		slblWaitingMsg.Visible = enable;
	}

	private void txtBoxValue_KeyPress(object sender, KeyPressEventArgs e)
	{
		if ((e.KeyChar >= '0' && e.KeyChar <= '9') || e.KeyChar == '\b')
		{
			e.Handled = false;
		}
		else
		{
			e.Handled = true;
		}
	}

	private void simUnlock()
	{
		if (string.IsNullOrEmpty(txtBoxPIN1.Text) && string.IsNullOrEmpty(txtBoxPIN2.Text))
		{
			MessageBox.Show("Please fill in at least one of the PIN");
			return;
		}
		string key = (string.IsNullOrEmpty(txtBoxPIN1.Text) ? null : txtBoxPIN1.Text);
		string key2 = (string.IsNullOrEmpty(txtBoxPIN2.Text) ? null : txtBoxPIN2.Text);
		UnLockKeys unLockKeys = new UnLockKeys
		{
			key1 = key,
			key2 = key2
		};
		device.SimUnlock(unLockKeys);
	}

	private void btnSelectFile_Click(object sender, EventArgs e)
	{
		OpenFileDialog openFileDialog = new OpenFileDialog();
		openFileDialog.Title = "Select simlock file";
		openFileDialog.InitialDirectory = ".\\";
		openFileDialog.Filter = "simlock files (*.bin;*.xml)|*.bin;*.xml|All files (*.*)|*.*";
		if (openFileDialog.ShowDialog() == DialogResult.OK)
		{
			txtBoxFilePath.Text = openFileDialog.FileName;
		}
	}

	private void simLock()
	{
		if (string.IsNullOrEmpty(txtBoxFilePath.Text) || !File.Exists(txtBoxFilePath.Text))
		{
			MessageBox.Show("Please select simlock file");
		}
		else
		{
			device.SimLock(txtBoxFilePath.Text);
		}
	}

	private void slblLock_Click(object sender, EventArgs e)
	{
		slblLock.ForeColor = Color.FromArgb(253, 192, 3);
		slblUnlock.ForeColor = Color.White;
		setMode(OperationMode.Lock);
	}

	private void setMode(OperationMode mode)
	{
		slblSIM1.Visible = ((mode != 0) ? true : false);
		slblSIM2.Visible = ((mode != 0) ? true : false);
		txtBoxPIN1.Visible = ((mode != 0) ? true : false);
		txtBoxPIN2.Visible = ((mode != 0) ? true : false);
		txtBoxFilePath.Visible = ((mode == OperationMode.Lock) ? true : false);
		slblSeletFile.Visible = ((mode == OperationMode.Lock) ? true : false);
		btnSelectFile.Visible = ((mode == OperationMode.Lock) ? true : false);
		opMode = mode;
	}

	private void slblUnlock_Click(object sender, EventArgs e)
	{
		slblLock.ForeColor = Color.White;
		slblUnlock.ForeColor = Color.FromArgb(253, 192, 3);
		setMode(OperationMode.Unlock);
	}

	private void btnStart_Click(object sender, EventArgs e)
	{
		setButtons(enable: false);
		new Thread((ThreadStart)delegate
		{
			if (opMode == OperationMode.Lock)
			{
				simLock();
			}
			else
			{
				simUnlock();
			}
			Invoke((MethodInvoker)delegate
			{
				setButtons(enable: true);
			});
		}).Start();
	}

	private void setButtons(bool enable)
	{
		btnStart.Enabled = enable;
		btnExit.Enabled = enable;
	}

	public void SetTargetDevice(Device _device)
	{
		device = _device;
		device.DeviceEventHandler += Device_DeviceEventHandler;
	}

	private async void lkLblUserGuide_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
	{
		string file = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\HMD_Devicekit\\UG_Simlock.pdf";
		if (!FileUtility.IsFileExists(file))
		{
			await HttpClientDownloadWithProgress.startDownload("UG_Simlock", ".pdf");
			return;
		}
		DialogResult result = MessageBox.Show("Do you want to open the file?", "Already downloaded!", MessageBoxButtons.YesNo);
		if (result == DialogResult.Yes)
		{
			Process.Start(file);
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
		this.slblWaitingMsg = new System.Windows.Forms.Label();
		this.panelAfter = new System.Windows.Forms.Panel();
		this.slblSIM2 = new System.Windows.Forms.Label();
		this.txtBoxPIN1 = new System.Windows.Forms.TextBox();
		this.slblSeletFile = new System.Windows.Forms.Label();
		this.slblSIM1 = new System.Windows.Forms.Label();
		this.txtBoxPIN2 = new System.Windows.Forms.TextBox();
		this.btnSelectFile = new hmd_pctool_windows.Components.YelloButton();
		this.txtBoxFilePath = new System.Windows.Forms.TextBox();
		this.slblLock = new System.Windows.Forms.Label();
		this.slblUnlock = new System.Windows.Forms.Label();
		this.btnStart = new hmd_pctool_windows.Components.YelloButton();
		this.btnExit = new hmd_pctool_windows.Components.YelloButton();
		this.lkLblUserGuide = new System.Windows.Forms.LinkLabel();
		this.panelAfter.SuspendLayout();
		base.SuspendLayout();
		this.slblWaitingMsg.AutoSize = true;
		this.slblWaitingMsg.BackColor = System.Drawing.Color.Transparent;
		this.slblWaitingMsg.Font = new System.Drawing.Font("Calibri", 11.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.slblWaitingMsg.ForeColor = System.Drawing.Color.White;
		this.slblWaitingMsg.Location = new System.Drawing.Point(69, 107);
		this.slblWaitingMsg.Name = "slblWaitingMsg";
		this.slblWaitingMsg.Size = new System.Drawing.Size(338, 18);
		this.slblWaitingMsg.TabIndex = 10;
		this.slblWaitingMsg.Text = "Please wait till the device enters Sim Control mode ...";
		this.panelAfter.Controls.Add(this.slblSIM2);
		this.panelAfter.Controls.Add(this.txtBoxPIN1);
		this.panelAfter.Controls.Add(this.slblSeletFile);
		this.panelAfter.Controls.Add(this.slblSIM1);
		this.panelAfter.Controls.Add(this.txtBoxPIN2);
		this.panelAfter.Controls.Add(this.btnSelectFile);
		this.panelAfter.Controls.Add(this.txtBoxFilePath);
		this.panelAfter.Location = new System.Drawing.Point(51, 60);
		this.panelAfter.Name = "panelAfter";
		this.panelAfter.Size = new System.Drawing.Size(329, 132);
		this.panelAfter.TabIndex = 15;
		this.slblSIM2.AutoSize = true;
		this.slblSIM2.BackColor = System.Drawing.Color.Transparent;
		this.slblSIM2.Font = new System.Drawing.Font("Calibri", 11.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.slblSIM2.ForeColor = System.Drawing.Color.White;
		this.slblSIM2.Location = new System.Drawing.Point(3, 56);
		this.slblSIM2.Name = "slblSIM2";
		this.slblSIM2.Size = new System.Drawing.Size(63, 18);
		this.slblSIM2.TabIndex = 8;
		this.slblSIM2.Text = "SIM2 PIN";
		this.txtBoxPIN1.BackColor = System.Drawing.Color.FromArgb(19, 36, 53);
		this.txtBoxPIN1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.txtBoxPIN1.ForeColor = System.Drawing.Color.FromArgb(65, 214, 171);
		this.txtBoxPIN1.Location = new System.Drawing.Point(83, 29);
		this.txtBoxPIN1.Name = "txtBoxPIN1";
		this.txtBoxPIN1.Size = new System.Drawing.Size(186, 20);
		this.txtBoxPIN1.TabIndex = 14;
		this.slblSeletFile.AutoSize = true;
		this.slblSeletFile.BackColor = System.Drawing.Color.Transparent;
		this.slblSeletFile.Font = new System.Drawing.Font("Calibri", 11.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.slblSeletFile.ForeColor = System.Drawing.Color.White;
		this.slblSeletFile.Location = new System.Drawing.Point(-3, 3);
		this.slblSeletFile.Name = "slblSeletFile";
		this.slblSeletFile.Size = new System.Drawing.Size(162, 18);
		this.slblSeletFile.TabIndex = 5;
		this.slblSeletFile.Text = "Please select simlock file";
		this.slblSIM1.AutoSize = true;
		this.slblSIM1.BackColor = System.Drawing.Color.Transparent;
		this.slblSIM1.Font = new System.Drawing.Font("Calibri", 11.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.slblSIM1.ForeColor = System.Drawing.Color.White;
		this.slblSIM1.Location = new System.Drawing.Point(3, 8);
		this.slblSIM1.Name = "slblSIM1";
		this.slblSIM1.Size = new System.Drawing.Size(63, 18);
		this.slblSIM1.TabIndex = 13;
		this.slblSIM1.Text = "SIM1 PIN";
		this.txtBoxPIN2.BackColor = System.Drawing.Color.FromArgb(19, 36, 53);
		this.txtBoxPIN2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.txtBoxPIN2.ForeColor = System.Drawing.Color.FromArgb(65, 214, 171);
		this.txtBoxPIN2.Location = new System.Drawing.Point(83, 79);
		this.txtBoxPIN2.Name = "txtBoxPIN2";
		this.txtBoxPIN2.Size = new System.Drawing.Size(186, 20);
		this.txtBoxPIN2.TabIndex = 9;
		this.txtBoxPIN2.KeyPress += new System.Windows.Forms.KeyPressEventHandler(txtBoxValue_KeyPress);
		this.btnSelectFile.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnSelectFile.Font = new System.Drawing.Font("Calibri", 9f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnSelectFile.ForeColor = System.Drawing.Color.White;
		this.btnSelectFile.Location = new System.Drawing.Point(275, 26);
		this.btnSelectFile.Name = "btnSelectFile";
		this.btnSelectFile.Size = new System.Drawing.Size(43, 24);
		this.btnSelectFile.TabIndex = 12;
		this.btnSelectFile.TabStop = false;
		this.btnSelectFile.Text = "...";
		this.btnSelectFile.UseVisualStyleBackColor = true;
		this.btnSelectFile.Click += new System.EventHandler(btnSelectFile_Click);
		this.txtBoxFilePath.BackColor = System.Drawing.Color.FromArgb(19, 36, 53);
		this.txtBoxFilePath.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.txtBoxFilePath.ForeColor = System.Drawing.Color.FromArgb(65, 214, 171);
		this.txtBoxFilePath.Location = new System.Drawing.Point(83, 26);
		this.txtBoxFilePath.Name = "txtBoxFilePath";
		this.txtBoxFilePath.Size = new System.Drawing.Size(186, 20);
		this.txtBoxFilePath.TabIndex = 11;
		this.slblLock.AutoSize = true;
		this.slblLock.BackColor = System.Drawing.Color.Transparent;
		this.slblLock.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.slblLock.Font = new System.Drawing.Font("Calibri", 12f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.slblLock.ForeColor = System.Drawing.Color.White;
		this.slblLock.Location = new System.Drawing.Point(40, 15);
		this.slblLock.Name = "slblLock";
		this.slblLock.Size = new System.Drawing.Size(40, 21);
		this.slblLock.TabIndex = 15;
		this.slblLock.Text = "Lock";
		this.slblLock.Click += new System.EventHandler(slblLock_Click);
		this.slblUnlock.AutoSize = true;
		this.slblUnlock.BackColor = System.Drawing.Color.Transparent;
		this.slblUnlock.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.slblUnlock.Font = new System.Drawing.Font("Calibri", 12f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.slblUnlock.ForeColor = System.Drawing.Color.White;
		this.slblUnlock.Location = new System.Drawing.Point(79, 15);
		this.slblUnlock.Name = "slblUnlock";
		this.slblUnlock.Size = new System.Drawing.Size(58, 21);
		this.slblUnlock.TabIndex = 17;
		this.slblUnlock.Text = "UnLock";
		this.slblUnlock.Click += new System.EventHandler(slblUnlock_Click);
		this.btnStart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnStart.Font = new System.Drawing.Font("Calibri", 9f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnStart.ForeColor = System.Drawing.Color.White;
		this.btnStart.Location = new System.Drawing.Point(180, 216);
		this.btnStart.Name = "btnStart";
		this.btnStart.Size = new System.Drawing.Size(87, 26);
		this.btnStart.TabIndex = 18;
		this.btnStart.TabStop = false;
		this.btnStart.Text = "Start";
		this.btnStart.UseVisualStyleBackColor = true;
		this.btnStart.Visible = false;
		this.btnStart.Click += new System.EventHandler(btnStart_Click);
		this.btnExit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnExit.Font = new System.Drawing.Font("Calibri", 9f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnExit.ForeColor = System.Drawing.Color.White;
		this.btnExit.Location = new System.Drawing.Point(273, 216);
		this.btnExit.Name = "btnExit";
		this.btnExit.Size = new System.Drawing.Size(87, 26);
		this.btnExit.TabIndex = 7;
		this.btnExit.TabStop = false;
		this.btnExit.Text = "Exit";
		this.btnExit.UseVisualStyleBackColor = true;
		this.btnExit.Click += new System.EventHandler(btnExit_Click);
		this.lkLblUserGuide.AutoSize = true;
		this.lkLblUserGuide.Location = new System.Drawing.Point(347, 19);
		this.lkLblUserGuide.Name = "lkLblUserGuide";
		this.lkLblUserGuide.Size = new System.Drawing.Size(60, 13);
		this.lkLblUserGuide.TabIndex = 30;
		this.lkLblUserGuide.TabStop = true;
		this.lkLblUserGuide.Text = "User Guide";
		this.lkLblUserGuide.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(lkLblUserGuide_LinkClicked);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.Color.FromArgb(19, 36, 53);
		base.ClientSize = new System.Drawing.Size(421, 271);
		base.Controls.Add(this.lkLblUserGuide);
		base.Controls.Add(this.btnStart);
		base.Controls.Add(this.slblUnlock);
		base.Controls.Add(this.slblLock);
		base.Controls.Add(this.slblWaitingMsg);
		base.Controls.Add(this.panelAfter);
		base.Controls.Add(this.btnExit);
		base.Name = "SimControlForm";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		this.Text = "PhoneEditForm";
		base.Load += new System.EventHandler(SimControlForm_Load);
		this.panelAfter.ResumeLayout(false);
		this.panelAfter.PerformLayout();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
