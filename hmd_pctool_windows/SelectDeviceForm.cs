using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using hmd_pctool_windows.Components;

namespace hmd_pctool_windows;

public class SelectDeviceForm : Form
{
	public string SN = "";

	private string result;

	private IContainer components = null;

	private YelloButton btnOK;

	private YelloButton btnCancel;

	private Label label1;

	private ComboBox cmboxDevices;

	public static string getSerialNo(string result)
	{
		SelectDeviceForm selectDeviceForm = new SelectDeviceForm(result);
		DialogResult dialogResult = selectDeviceForm.ShowDialog();
		if (dialogResult != DialogResult.OK)
		{
			return "";
		}
		return selectDeviceForm.SN;
	}

	public SelectDeviceForm(string result)
	{
		InitializeComponent();
		this.result = result;
	}

	private void SelectDeviceForm_Load(object sender, EventArgs e)
	{
		string[] array = result.ToString().Split(',');
		if (array.Length == 1)
		{
			SN = array[0];
			base.DialogResult = DialogResult.OK;
			Close();
		}
		ComboBox.ObjectCollection items3 = cmboxDevices.Items;
		object[] items2 = array;
		items3.AddRange(items2);
		cmboxDevices.SelectedIndex = 0;
	}

	private void btnOK_Click(object sender, EventArgs e)
	{
		base.DialogResult = DialogResult.OK;
		SN = cmboxDevices.SelectedItem.ToString();
		Close();
	}

	private void btnCancel_Click(object sender, EventArgs e)
	{
		base.DialogResult = DialogResult.Cancel;
		Close();
	}

	private void btnCancel_MouseHover(object sender, EventArgs e)
	{
		btnCancel.BackColor = Color.FromArgb(253, 192, 3);
	}

	private void btnCancel_MouseLeave(object sender, EventArgs e)
	{
		btnCancel.BackColor = Color.Transparent;
	}

	private void btnOK_MouseHover(object sender, EventArgs e)
	{
		btnOK.BackColor = Color.FromArgb(253, 192, 3);
	}

	private void btnOK_MouseLeave(object sender, EventArgs e)
	{
		btnOK.BackColor = Color.Transparent;
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
		this.btnOK = new hmd_pctool_windows.Components.YelloButton();
		this.btnCancel = new hmd_pctool_windows.Components.YelloButton();
		this.label1 = new System.Windows.Forms.Label();
		this.cmboxDevices = new System.Windows.Forms.ComboBox();
		base.SuspendLayout();
		this.btnOK.Location = new System.Drawing.Point(81, 133);
		this.btnOK.Name = "btnOK";
		this.btnOK.Size = new System.Drawing.Size(87, 24);
		this.btnOK.TabIndex = 0;
		this.btnOK.TabStop = false;
		this.btnOK.Text = "OK";
		this.btnOK.Click += new System.EventHandler(btnOK_Click);
		this.btnCancel.Location = new System.Drawing.Point(228, 133);
		this.btnCancel.Name = "btnCancel";
		this.btnCancel.Size = new System.Drawing.Size(87, 24);
		this.btnCancel.TabIndex = 3;
		this.btnCancel.TabStop = false;
		this.btnCancel.Text = "Cancel";
		this.btnCancel.Click += new System.EventHandler(btnCancel_Click);
		this.label1.AutoSize = true;
		this.label1.BackColor = System.Drawing.Color.Transparent;
		this.label1.Font = new System.Drawing.Font("Calibri", 11.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label1.ForeColor = System.Drawing.Color.White;
		this.label1.Location = new System.Drawing.Point(38, 27);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(134, 18);
		this.label1.TabIndex = 4;
		this.label1.Text = "Please select Device";
		this.cmboxDevices.Font = new System.Drawing.Font("Calibri", 9f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.cmboxDevices.FormattingEnabled = true;
		this.cmboxDevices.Location = new System.Drawing.Point(81, 62);
		this.cmboxDevices.Name = "cmboxDevices";
		this.cmboxDevices.Size = new System.Drawing.Size(241, 22);
		this.cmboxDevices.TabIndex = 5;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 12f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.Color.FromArgb(19, 36, 53);
		base.ClientSize = new System.Drawing.Size(396, 211);
		base.Controls.Add(this.cmboxDevices);
		base.Controls.Add(this.label1);
		base.Controls.Add(this.btnCancel);
		base.Controls.Add(this.btnOK);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
		base.Name = "SelectDeviceForm";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		this.Text = "Please select SKUID";
		base.Load += new System.EventHandler(SelectDeviceForm_Load);
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
