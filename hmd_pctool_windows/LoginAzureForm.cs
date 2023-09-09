using System;
using System.ComponentModel;
using System.Drawing;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using hmd_pctool_windows.Components;

namespace hmd_pctool_windows;

public class LoginAzureForm : BorderlessForm
{
	private enum ServerStatus
	{
		UNKNOWN,
		ONLINE,
		OFFLINE
	}

	private LoginAzureForm loginAzureForm;

	private Form loginAzureWebPage;

	private ServerStatus serverStatus = ServerStatus.UNKNOWN;

	private bool isActivate = true;

	private Thread thread;

	private IContainer components = null;

	private Label lblNotification;

	private GreenButton btnCancel;

	private PictureBox picBoxLogo;

	public LoginAzureForm()
	{
		InitializeComponent();
	}

	internal bool isConnectionExists()
	{
		try
		{
			TcpClient tcpClient = new TcpClient("login.microsoftonline.com", 80);
			tcpClient.Close();
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	private void LoginAzureForm_Load(object sender, EventArgs e)
	{
		loginAzureForm = this;
	}

	private void LoginAzureForm_Shown(object sender, EventArgs e)
	{
		thread = new Thread((ThreadStart)delegate
		{
			while (isActivate)
			{
				Thread.Sleep(1000);
				foreach (Form form in Application.OpenForms)
				{
					if (!(form is LoginAzureForm))
					{
						loginAzureWebPage = form;
						break;
					}
				}
				if (!isConnectionExists())
				{
					if (serverStatus != ServerStatus.OFFLINE)
					{
						serverStatus = ServerStatus.OFFLINE;
						ShowServerStatus(serverStatus);
						SetWebPageForm(enable: false);
					}
				}
				else if (serverStatus != ServerStatus.ONLINE)
				{
					serverStatus = ServerStatus.ONLINE;
					ShowServerStatus(serverStatus);
					SetWebPageForm(enable: true);
				}
			}
		});
		thread.Start();
	}

	private void SetWebPageForm(bool enable)
	{
		if (loginAzureWebPage != null)
		{
			InvokeAct(loginAzureWebPage, delegate
			{
				loginAzureWebPage.Enabled = enable;
			});
		}
		else if (enable)
		{
			StartLogin();
		}
	}

	private void InvokeAct(Control targetCtrl, Action action)
	{
		try
		{
			if (targetCtrl != null && targetCtrl.InvokeRequired)
			{
				targetCtrl.Invoke(action);
			}
			else
			{
				action();
			}
		}
		catch
		{
		}
	}

	private void ShowServerStatus(ServerStatus status)
	{
		switch (status)
		{
		case ServerStatus.ONLINE:
			InvokeAct(loginAzureForm, delegate
			{
				lblNotification.Text = "Server online, connecting to HMD azure server ...";
				btnCancel.Visible = false;
			});
			Thread.Sleep(3000);
			InvokeAct(loginAzureForm, delegate
			{
				loginAzureForm.Hide();
			});
			break;
		case ServerStatus.OFFLINE:
			InvokeAct(loginAzureForm, delegate
			{
				lblNotification.Text = "Server offline, re-connecting ...";
				btnCancel.Visible = true;
				loginAzureForm.TopMost = true;
				loginAzureForm.Show();
			});
			break;
		}
	}

	private void StartLogin()
	{
		loginAzureForm.Invoke((MethodInvoker)async delegate
		{
			string errorMsg = await AzureNativeClient.Instance.Login(null, null);
			if (errorMsg.Equals("OK"))
			{
				if (!(await AzureNativeClient.Instance.CheckAdGroup()))
				{
					MessageBox.Show("Please contact the DeviceKit team or your HMD contact person for support", "DeviceKit Denied", MessageBoxButtons.OK, MessageBoxIcon.Asterisk, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
					Close();
				}
				else
				{
					ServerResponse response = await AzureNativeClient.Instance.IsValidUser();
					if (!response.IsSuccessed)
					{
						MessageBox.Show(response.FailReason, "DeviceKit", MessageBoxButtons.OK, MessageBoxIcon.Asterisk, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
						Close();
					}
					else
					{
						Hide();
						StopThread();
						if (Program.isOtpEnable())
						{
							OTPForm otpForm = new OTPForm();
							otpForm.Show();
						}
						else
						{
							Program.functionSelectForm.Show();
						}
					}
				}
			}
			else if (!errorMsg.Contains("An error occurred while sending the request"))
			{
				StopThread();
				Close();
			}
		});
	}

	private void StopThread()
	{
		isActivate = false;
		thread.Abort();
	}

	private void LoginAzureForm_FormClosing(object sender, FormClosingEventArgs e)
	{
		StopThread();
	}

	private void btnCancel_Click(object sender, EventArgs e)
	{
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(hmd_pctool_windows.LoginAzureForm));
		this.btnCancel = new hmd_pctool_windows.Components.GreenButton();
		this.lblNotification = new System.Windows.Forms.Label();
		this.picBoxLogo = new System.Windows.Forms.PictureBox();
		((System.ComponentModel.ISupportInitialize)this.picBoxLogo).BeginInit();
		base.SuspendLayout();
		this.btnCancel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(65, 214, 171);
		this.btnCancel.FlatAppearance.BorderSize = 2;
		this.btnCancel.FlatAppearance.MouseDownBackColor = System.Drawing.Color.White;
		this.btnCancel.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(65, 214, 171);
		this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnCancel.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnCancel.Location = new System.Drawing.Point(156, 160);
		this.btnCancel.Name = "btnCancel";
		this.btnCancel.Size = new System.Drawing.Size(70, 27);
		this.btnCancel.TabIndex = 13;
		this.btnCancel.Text = "Cancel";
		this.btnCancel.UseVisualStyleBackColor = true;
		this.btnCancel.Visible = false;
		this.btnCancel.Click += new System.EventHandler(btnCancel_Click);
		this.lblNotification.BackColor = System.Drawing.Color.Transparent;
		this.lblNotification.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.lblNotification.ForeColor = System.Drawing.Color.Black;
		this.lblNotification.Location = new System.Drawing.Point(12, 105);
		this.lblNotification.Name = "lblNotification";
		this.lblNotification.Size = new System.Drawing.Size(359, 16);
		this.lblNotification.TabIndex = 5;
		this.lblNotification.Text = "Checking HMD azure server status ...";
		this.lblNotification.TextAlign = System.Drawing.ContentAlignment.TopCenter;
		this.picBoxLogo.Image = (System.Drawing.Image)resources.GetObject("picBoxLogo.Image");
		this.picBoxLogo.Location = new System.Drawing.Point(25, 12);
		this.picBoxLogo.Name = "picBoxLogo";
		this.picBoxLogo.Size = new System.Drawing.Size(112, 50);
		this.picBoxLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
		this.picBoxLogo.TabIndex = 14;
		this.picBoxLogo.TabStop = false;
		base.AutoScaleDimensions = new System.Drawing.SizeF(9f, 22f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.Color.White;
		base.ClientSize = new System.Drawing.Size(383, 202);
		base.Controls.Add(this.picBoxLogo);
		base.Controls.Add(this.btnCancel);
		base.Controls.Add(this.lblNotification);
		this.Font = new System.Drawing.Font("Calibri", 9f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		base.Name = "LoginAzureForm";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		this.Text = "Detect device";
		base.FormClosing += new System.Windows.Forms.FormClosingEventHandler(LoginAzureForm_FormClosing);
		base.Load += new System.EventHandler(LoginAzureForm_Load);
		base.Shown += new System.EventHandler(LoginAzureForm_Shown);
		((System.ComponentModel.ISupportInitialize)this.picBoxLogo).EndInit();
		base.ResumeLayout(false);
	}
}
