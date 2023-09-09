using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using hmd_pctool_windows.Components;

namespace hmd_pctool_windows;

public class WallpaperedEditForm : BorderlessForm
{
	private Device device = null;

	private bool isReadItem = false;

	private IContainer components = null;

	private YelloButton btnExit;

	private YelloButton btnSave;

	private Label label2;

	private Label slblSeletItem;

	private ComboBox cmboxItems;

	private ComboBox cmboxValues;

	private Label lblInfo;

	public WallpaperedEditForm()
	{
		InitializeComponent();
	}

	private void btnExit_Click(object sender, EventArgs e)
	{
		exit();
	}

	private void exit()
	{
		device.DeviceEventHandler -= Device_DeviceEventHandler;
		Program.functionSelectForm.Show();
		Close();
	}

	private void btnSave_Click(object sender, EventArgs e)
	{
		isReadItem = false;
		SetItems(enable: false);
		if (cmboxItems.SelectedIndex == 0)
		{
			device.WriteItem(DeviceItemType.WallPaper, cmboxValues.SelectedItem.ToString());
		}
		else
		{
			device.WriteItem(DeviceItemType.SkuId, cmboxValues.SelectedItem.ToString());
		}
	}

	private void WallpaperedEditForm_Load(object sender, EventArgs e)
	{
		HmdOemProvider.RunScript("beforewallpaperedit.txt");
		cmboxItems.SelectedIndex = 0;
	}

	private void cmboxItems_SelectedIndexChanged(object sender, EventArgs e)
	{
		isReadItem = true;
		SetItems(enable: false);
		lblInfo.Visible = true;
		if (cmboxItems.SelectedIndex == 0)
		{
			cmboxValues.Items.Clear();
			cmboxValues.Items.AddRange(new object[15]
			{
				"0x1", "0x2", "0x3", "0x4", "0x5", "0x6", "0x7", "0x8", "0x9", "0xa",
				"0xb", "0xc", "0xd", "0xe", "0xf"
			});
			device.ReadItem(DeviceItemType.WallPaper);
		}
		else
		{
			cmboxValues.Items.Clear();
			ComboBox.ObjectCollection items = cmboxValues.Items;
			object[] sKUIDs = SelectSKUIDForm.GetSKUIDs();
			items.AddRange(sKUIDs);
			device.ReadItem(DeviceItemType.SkuId);
		}
	}

	public void SetTargetDevice(Device _device)
	{
		device = _device;
		device.DeviceEventHandler += Device_DeviceEventHandler;
	}

	private void Device_DeviceEventHandler(object sender, DeviceEventArgs e)
	{
		CommandType commandType = (CommandType)e.What;
		Console.WriteLine(e.EventType.ToString());
		Invoke((MethodInvoker)delegate
		{
			SetItems(enable: true);
			lblInfo.Visible = false;
		});
		switch (e.EventType)
		{
		case DeviceEventType.OnReadItem:
		{
			DeviceItemType what = (DeviceItemType)e.What;
			Invoke((MethodInvoker)delegate
			{
				cmboxValues.Text = e.StringArg;
			});
			break;
		}
		case DeviceEventType.OnWriteItem:
			Invoke((MethodInvoker)delegate
			{
				if (!isReadItem)
				{
					MessageBox.Show("OK");
				}
				else
				{
					SetItems(enable: false);
					lblInfo.Visible = true;
					device.ReadItem(DeviceItemType.WallPaper);
				}
			});
			break;
		case DeviceEventType.OnCommandFail:
			Invoke((MethodInvoker)delegate
			{
				if (commandType == CommandType.WriteItem)
				{
					MessageBox.Show("Fail to change value\nReason : " + e.StringArg);
				}
				else if (commandType == CommandType.ReadItem)
				{
					MessageBox.Show("Fail to read value\nReason : " + e.StringArg);
				}
			});
			break;
		}
	}

	private void SetItems(bool enable)
	{
		cmboxItems.Enabled = enable;
		cmboxValues.Enabled = enable;
		btnSave.Enabled = enable;
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
		this.label2 = new System.Windows.Forms.Label();
		this.btnExit = new hmd_pctool_windows.Components.YelloButton();
		this.btnSave = new hmd_pctool_windows.Components.YelloButton();
		this.slblSeletItem = new System.Windows.Forms.Label();
		this.cmboxItems = new System.Windows.Forms.ComboBox();
		this.cmboxValues = new System.Windows.Forms.ComboBox();
		this.lblInfo = new System.Windows.Forms.Label();
		base.SuspendLayout();
		this.label2.AutoSize = true;
		this.label2.BackColor = System.Drawing.Color.Transparent;
		this.label2.Font = new System.Drawing.Font("Calibri", 11.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label2.ForeColor = System.Drawing.Color.White;
		this.label2.Location = new System.Drawing.Point(196, 9);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(43, 18);
		this.label2.TabIndex = 8;
		this.label2.Text = "Value";
		this.btnExit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnExit.Font = new System.Drawing.Font("Calibri", 9f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnExit.ForeColor = System.Drawing.Color.White;
		this.btnExit.Location = new System.Drawing.Point(252, 186);
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
		this.btnSave.Location = new System.Drawing.Point(52, 186);
		this.btnSave.Name = "btnSave";
		this.btnSave.Size = new System.Drawing.Size(87, 26);
		this.btnSave.TabIndex = 6;
		this.btnSave.TabStop = false;
		this.btnSave.Text = "Save";
		this.btnSave.UseVisualStyleBackColor = true;
		this.btnSave.Click += new System.EventHandler(btnSave_Click);
		this.slblSeletItem.AutoSize = true;
		this.slblSeletItem.BackColor = System.Drawing.Color.Transparent;
		this.slblSeletItem.Font = new System.Drawing.Font("Calibri", 11.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.slblSeletItem.ForeColor = System.Drawing.Color.White;
		this.slblSeletItem.Location = new System.Drawing.Point(12, 9);
		this.slblSeletItem.Name = "slblSeletItem";
		this.slblSeletItem.Size = new System.Drawing.Size(145, 18);
		this.slblSeletItem.TabIndex = 11;
		this.slblSeletItem.Text = "Please select the item";
		this.cmboxItems.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.cmboxItems.FormattingEnabled = true;
		this.cmboxItems.Items.AddRange(new object[2] { "Wallpaper ID", "SKU ID" });
		this.cmboxItems.Location = new System.Drawing.Point(15, 30);
		this.cmboxItems.Name = "cmboxItems";
		this.cmboxItems.Size = new System.Drawing.Size(178, 21);
		this.cmboxItems.TabIndex = 10;
		this.cmboxItems.SelectedIndexChanged += new System.EventHandler(cmboxItems_SelectedIndexChanged);
		this.cmboxValues.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.cmboxValues.FormattingEnabled = true;
		this.cmboxValues.Location = new System.Drawing.Point(199, 30);
		this.cmboxValues.Name = "cmboxValues";
		this.cmboxValues.Size = new System.Drawing.Size(181, 21);
		this.cmboxValues.TabIndex = 12;
		this.lblInfo.AutoSize = true;
		this.lblInfo.BackColor = System.Drawing.Color.Transparent;
		this.lblInfo.Font = new System.Drawing.Font("Calibri", 11.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.lblInfo.ForeColor = System.Drawing.Color.White;
		this.lblInfo.Location = new System.Drawing.Point(155, 111);
		this.lblInfo.Name = "lblInfo";
		this.lblInfo.Size = new System.Drawing.Size(71, 18);
		this.lblInfo.TabIndex = 13;
		this.lblInfo.Text = "Loading ...";
		this.lblInfo.Visible = false;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.Color.FromArgb(19, 36, 53);
		base.ClientSize = new System.Drawing.Size(392, 272);
		base.Controls.Add(this.lblInfo);
		base.Controls.Add(this.cmboxValues);
		base.Controls.Add(this.slblSeletItem);
		base.Controls.Add(this.cmboxItems);
		base.Controls.Add(this.label2);
		base.Controls.Add(this.btnExit);
		base.Controls.Add(this.btnSave);
		base.Name = "WallpaperedEditForm";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		this.Text = "WallpaperedEditForm";
		base.Load += new System.EventHandler(WallpaperedEditForm_Load);
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
