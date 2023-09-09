using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using hmd_pctool_windows.Components;
using hmd_pctool_windows.Utils;

namespace hmd_pctool_windows;

public class PhoneEditForm : BorderlessForm
{
	private Device device = null;

	private IContainer components = null;

	private ComboBox cmboxRepairItems;

	private Label slblSeletItem;

	private YelloButton btnExit;

	private YelloButton btnSave;

	private Label slblValue;

	private TextBox txtBoxValue;

	private Label slblWaitingMsg;

	private Label lblInfo;

	private LinkLabel lkLblUserGuide;

	public PhoneEditForm()
	{
		InitializeComponent();
	}

	private void Device_DeviceEventHandler(object sender, DeviceEventArgs e)
	{
		switch (e.EventType)
		{
		case DeviceEventType.OnCommandSuccess:
		{
			CommandType what2 = (CommandType)e.What;
			setWaitingMessage(enable: false);
			if (what2 == CommandType.StartPhoneEdit)
			{
				Invoke((MethodInvoker)delegate
				{
					cmboxRepairItems.SelectedIndex = 0;
				});
			}
			break;
		}
		case DeviceEventType.OnCommandFail:
		{
			CommandType what2 = (CommandType)e.What;
			setWaitingMessage(enable: false);
			if (what2 == CommandType.StartPhoneEdit || what2 == CommandType.StopPhoneEditSimlock)
			{
				Invoke((MethodInvoker)delegate
				{
					MessageBox.Show("Fail to get repair permission\nReason : " + e.StringArg);
					exit();
				});
				break;
			}
			switch (what2)
			{
			case CommandType.ReadItem:
				Invoke((MethodInvoker)delegate
				{
					MessageBox.Show("Fail to get phone data\nReason : " + e.StringArg);
					setUI(enable: true);
				});
				break;
			case CommandType.WriteItem:
				Invoke((MethodInvoker)delegate
				{
					MessageBox.Show("Fail to save phone data\nReason : " + e.StringArg);
					setUI(enable: true);
				});
				break;
			}
			break;
		}
		case DeviceEventType.OnReadItem:
		{
			DeviceItemType what = (DeviceItemType)e.What;
			Invoke((MethodInvoker)delegate
			{
				txtBoxValue.Text = e.StringArg;
				setUI(enable: true);
			});
			break;
		}
		case DeviceEventType.OnWriteItem:
		{
			DeviceItemType what = (DeviceItemType)e.What;
			Invoke((MethodInvoker)delegate
			{
				MessageBox.Show("OK");
				setUI(enable: true);
			});
			break;
		}
		case DeviceEventType.OnFlashStatus:
		case DeviceEventType.OnUnlockFrpStatus:
		case DeviceEventType.OnGetVar:
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

	private void setUI(bool enable)
	{
		lblInfo.Visible = !enable;
		btnSave.Enabled = enable;
		txtBoxValue.Enabled = enable;
		cmboxRepairItems.Enabled = enable;
	}

	private void btnSave_Click(object sender, EventArgs e)
	{
		setUI(enable: false);
		RepairType selectedIndex = (RepairType)cmboxRepairItems.SelectedIndex;
		string value = txtBoxValue.Text;
		try
		{
			setRepairValue(selectedIndex, value);
		}
		catch (Exception ex)
		{
			MessageBox.Show("Fail to save phone data\nReason : " + ex.Message);
		}
	}

	private void cmboxRepairItems_SelectedIndexChanged(object sender, EventArgs e)
	{
		setUI(enable: false);
		RepairType selectedIndex = (RepairType)cmboxRepairItems.SelectedIndex;
		try
		{
			setValueLength(selectedIndex);
			getRepairValue(selectedIndex);
		}
		catch (Exception ex)
		{
			MessageBox.Show("Fail to get phone data\nReason : " + ex.Message);
		}
	}

	private void setValueLength(RepairType type)
	{
		switch (type)
		{
		case RepairType.psn:
			txtBoxValue.MaxLength = 19;
			break;
		case RepairType.imei:
		case RepairType.imei2:
			txtBoxValue.MaxLength = 530;
			break;
		case RepairType.meid:
			txtBoxValue.MaxLength = 15;
			break;
		case RepairType.wifiaddr:
		case RepairType.btaddr:
			txtBoxValue.MaxLength = 12;
			break;
		default:
			txtBoxValue.MaxLength = 16;
			break;
		}
	}

	private void setRepairValue(RepairType type, string value)
	{
		try
		{
			switch (type)
			{
			case RepairType.psn:
				device.WriteItem(DeviceItemType.Psn, value);
				break;
			case RepairType.imei:
				device.WriteItem(DeviceItemType.Imei, value);
				break;
			case RepairType.imei2:
				device.WriteItem(DeviceItemType.Imei2, value);
				break;
			case RepairType.meid:
				device.WriteItem(DeviceItemType.Meid, value);
				break;
			case RepairType.wifiaddr:
				device.WriteItem(DeviceItemType.WifiAddr, value);
				break;
			case RepairType.btaddr:
				device.WriteItem(DeviceItemType.BtAddress, value);
				break;
			default:
				MessageBox.Show("unknow type : " + type);
				break;
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show("Fail to save phone data\nReason : " + ex.Message);
		}
	}

	private void getRepairValue(RepairType type)
	{
		try
		{
			switch (type)
			{
			case RepairType.psn:
				device.ReadItem(DeviceItemType.Psn);
				break;
			case RepairType.imei:
				device.ReadItem(DeviceItemType.Imei);
				break;
			case RepairType.imei2:
				device.ReadItem(DeviceItemType.Imei2);
				break;
			case RepairType.meid:
				device.ReadItem(DeviceItemType.Meid);
				break;
			case RepairType.wifiaddr:
				device.ReadItem(DeviceItemType.WifiAddr);
				break;
			case RepairType.btaddr:
				device.ReadItem(DeviceItemType.BtAddress);
				break;
			default:
				MessageBox.Show("unknow type : " + type);
				break;
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show("Fail to get phone data\nReason : " + ex.Message);
		}
	}

	private void PhoneEditForm_Load(object sender, EventArgs e)
	{
		device.StartPhoneEdit();
		setWaitingMessage(enable: true);
	}

	private void setWaitingMessage(bool enable)
	{
		btnSave.Visible = !enable;
		txtBoxValue.Visible = !enable;
		cmboxRepairItems.Visible = !enable;
		slblSeletItem.Visible = !enable;
		slblValue.Visible = !enable;
		slblWaitingMsg.Visible = enable;
	}

	private void txtBoxValue_KeyPress(object sender, KeyPressEventArgs e)
	{
		if (cmboxRepairItems.SelectedIndex == 0)
		{
			if ((e.KeyChar >= '0' && e.KeyChar <= '9') || (e.KeyChar >= 'A' && e.KeyChar <= 'Z') || (e.KeyChar >= 'a' && e.KeyChar <= 'z') || e.KeyChar == '\b' || e.KeyChar == '\u0003' || e.KeyChar == '\u0016')
			{
				e.Handled = false;
			}
			else
			{
				e.Handled = true;
			}
		}
		else if (cmboxRepairItems.SelectedIndex == 1 || cmboxRepairItems.SelectedIndex == 2)
		{
			if ((e.KeyChar >= '0' && e.KeyChar <= '9') || (e.KeyChar >= 'A' && e.KeyChar <= 'Z') || (e.KeyChar >= 'a' && e.KeyChar <= 'z') || e.KeyChar == '\b' || e.KeyChar == ':' || e.KeyChar == '\u0003' || e.KeyChar == '\u0016')
			{
				e.Handled = false;
			}
			else
			{
				e.Handled = true;
			}
		}
		else if ((e.KeyChar >= '0' && e.KeyChar <= '9') || (e.KeyChar >= 'A' && e.KeyChar <= 'F') || (e.KeyChar >= 'a' && e.KeyChar <= 'f') || e.KeyChar == '\b' || e.KeyChar == '\u0003' || e.KeyChar == '\u0016')
		{
			e.Handled = false;
		}
		else
		{
			e.Handled = true;
		}
	}

	public void SetTargetDevice(Device _device)
	{
		device = _device;
		device.DeviceEventHandler += Device_DeviceEventHandler;
	}

	private async void lkLblUserGuide_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
	{
		string file = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\HMD_Devicekit\\UG_PhoneEdit.pdf";
		if (!FileUtility.IsFileExists(file))
		{
			await HttpClientDownloadWithProgress.startDownload("UG_PhoneEdit", ".pdf");
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
		this.cmboxRepairItems = new System.Windows.Forms.ComboBox();
		this.slblSeletItem = new System.Windows.Forms.Label();
		this.btnExit = new hmd_pctool_windows.Components.YelloButton();
		this.btnSave = new hmd_pctool_windows.Components.YelloButton();
		this.slblValue = new System.Windows.Forms.Label();
		this.txtBoxValue = new System.Windows.Forms.TextBox();
		this.slblWaitingMsg = new System.Windows.Forms.Label();
		this.lblInfo = new System.Windows.Forms.Label();
		this.lkLblUserGuide = new System.Windows.Forms.LinkLabel();
		base.SuspendLayout();
		this.cmboxRepairItems.FormattingEnabled = true;
		this.cmboxRepairItems.Items.AddRange(new object[6] { " PSN", " IMEI", " IMEI2", " MEID", " WiFi address", " BT address" });
		this.cmboxRepairItems.Location = new System.Drawing.Point(119, 86);
		this.cmboxRepairItems.Name = "cmboxRepairItems";
		this.cmboxRepairItems.Size = new System.Drawing.Size(186, 21);
		this.cmboxRepairItems.TabIndex = 1;
		this.cmboxRepairItems.Visible = false;
		this.cmboxRepairItems.SelectedIndexChanged += new System.EventHandler(cmboxRepairItems_SelectedIndexChanged);
		this.slblSeletItem.AutoSize = true;
		this.slblSeletItem.BackColor = System.Drawing.Color.Transparent;
		this.slblSeletItem.Font = new System.Drawing.Font("Calibri", 11.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.slblSeletItem.ForeColor = System.Drawing.Color.White;
		this.slblSeletItem.Location = new System.Drawing.Point(51, 46);
		this.slblSeletItem.Name = "slblSeletItem";
		this.slblSeletItem.Size = new System.Drawing.Size(145, 18);
		this.slblSeletItem.TabIndex = 5;
		this.slblSeletItem.Text = "Please select the item";
		this.slblSeletItem.Visible = false;
		this.btnExit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnExit.Font = new System.Drawing.Font("Calibri", 9f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnExit.ForeColor = System.Drawing.Color.White;
		this.btnExit.Location = new System.Drawing.Point(242, 217);
		this.btnExit.Name = "btnExit";
		this.btnExit.Size = new System.Drawing.Size(87, 26);
		this.btnExit.TabIndex = 7;
		this.btnExit.TabStop = false;
		this.btnExit.Text = "Exit";
		this.btnExit.UseVisualStyleBackColor = true;
		this.btnExit.Click += new System.EventHandler(btnExit_Click);
		this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnSave.Font = new System.Drawing.Font("Calibri", 9f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnSave.ForeColor = System.Drawing.Color.White;
		this.btnSave.Location = new System.Drawing.Point(95, 217);
		this.btnSave.Name = "btnSave";
		this.btnSave.Size = new System.Drawing.Size(87, 26);
		this.btnSave.TabIndex = 6;
		this.btnSave.TabStop = false;
		this.btnSave.Text = "Save";
		this.btnSave.UseVisualStyleBackColor = true;
		this.btnSave.Visible = false;
		this.btnSave.Click += new System.EventHandler(btnSave_Click);
		this.slblValue.AutoSize = true;
		this.slblValue.BackColor = System.Drawing.Color.Transparent;
		this.slblValue.Font = new System.Drawing.Font("Calibri", 11.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.slblValue.ForeColor = System.Drawing.Color.White;
		this.slblValue.Location = new System.Drawing.Point(51, 144);
		this.slblValue.Name = "slblValue";
		this.slblValue.Size = new System.Drawing.Size(43, 18);
		this.slblValue.TabIndex = 8;
		this.slblValue.Text = "Value";
		this.slblValue.Visible = false;
		this.txtBoxValue.BackColor = System.Drawing.Color.FromArgb(19, 36, 53);
		this.txtBoxValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.txtBoxValue.ForeColor = System.Drawing.Color.FromArgb(65, 214, 171);
		this.txtBoxValue.Location = new System.Drawing.Point(119, 160);
		this.txtBoxValue.Multiline = true;
		this.txtBoxValue.Name = "txtBoxValue";
		this.txtBoxValue.Size = new System.Drawing.Size(186, 20);
		this.txtBoxValue.TabIndex = 9;
		this.txtBoxValue.Visible = false;
		this.txtBoxValue.KeyPress += new System.Windows.Forms.KeyPressEventHandler(txtBoxValue_KeyPress);
		this.slblWaitingMsg.AutoSize = true;
		this.slblWaitingMsg.BackColor = System.Drawing.Color.Transparent;
		this.slblWaitingMsg.Font = new System.Drawing.Font("Calibri", 11.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.slblWaitingMsg.ForeColor = System.Drawing.Color.White;
		this.slblWaitingMsg.Location = new System.Drawing.Point(50, 119);
		this.slblWaitingMsg.Name = "slblWaitingMsg";
		this.slblWaitingMsg.Size = new System.Drawing.Size(333, 18);
		this.slblWaitingMsg.TabIndex = 10;
		this.slblWaitingMsg.Text = "Please wait till the device enters Phone Edit mode ...";
		this.lblInfo.AutoSize = true;
		this.lblInfo.BackColor = System.Drawing.Color.Transparent;
		this.lblInfo.Font = new System.Drawing.Font("Calibri", 11.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.lblInfo.ForeColor = System.Drawing.Color.White;
		this.lblInfo.Location = new System.Drawing.Point(182, 187);
		this.lblInfo.Name = "lblInfo";
		this.lblInfo.Size = new System.Drawing.Size(71, 18);
		this.lblInfo.TabIndex = 11;
		this.lblInfo.Text = "Loading ...";
		this.lblInfo.Visible = false;
		this.lkLblUserGuide.AutoSize = true;
		this.lkLblUserGuide.Location = new System.Drawing.Point(307, 30);
		this.lkLblUserGuide.Name = "lkLblUserGuide";
		this.lkLblUserGuide.Size = new System.Drawing.Size(60, 13);
		this.lkLblUserGuide.TabIndex = 30;
		this.lkLblUserGuide.TabStop = true;
		this.lkLblUserGuide.Text = "User Guide";
		this.lkLblUserGuide.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(lkLblUserGuide_LinkClicked);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.Color.FromArgb(19, 36, 53);
		base.ClientSize = new System.Drawing.Size(392, 297);
		base.Controls.Add(this.lkLblUserGuide);
		base.Controls.Add(this.lblInfo);
		base.Controls.Add(this.slblWaitingMsg);
		base.Controls.Add(this.txtBoxValue);
		base.Controls.Add(this.slblValue);
		base.Controls.Add(this.btnExit);
		base.Controls.Add(this.btnSave);
		base.Controls.Add(this.slblSeletItem);
		base.Controls.Add(this.cmboxRepairItems);
		base.Name = "PhoneEditForm";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		this.Text = "PhoneEditForm";
		base.Load += new System.EventHandler(PhoneEditForm_Load);
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
