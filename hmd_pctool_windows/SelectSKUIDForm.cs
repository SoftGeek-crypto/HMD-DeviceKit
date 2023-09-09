using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using hmd_pctool_windows.Components;

namespace hmd_pctool_windows;

public class SelectSKUIDForm : BorderlessForm
{
	private IContainer components = null;

	private YelloButton btnOK;

	private YelloButton btnCancel;

	private Label label1;

	private ComboBox cmboxSKUs;

	public SelectSKUIDForm()
	{
		InitializeComponent();
	}

	public static string[] GetSKUIDs()
	{
		return new string[13]
		{
			"600WW", "600ID", "600RU", "600EEA", "600CLA", "600TEL", "600EEE", "600VPR", "600VPO", "600FD",
			"600IN", "600M0", "6000F"
		};
	}

	private void SelectSKUForm_Load(object sender, EventArgs e)
	{
		try
		{
			ComboBox.ObjectCollection items = cmboxSKUs.Items;
			object[] sKUIDs = GetSKUIDs();
			items.AddRange(sKUIDs);
			cmboxSKUs.Text = HmdPcToolApi.getSkuID();
		}
		catch (Exception)
		{
			btnOK.Enabled = false;
		}
	}

	private void btnOK_Click(object sender, EventArgs e)
	{
		base.DialogResult = DialogResult.OK;
		if (HmdPcToolApi.setSkuId(cmboxSKUs.SelectedItem.ToString()) == 0)
		{
			MessageBox.Show("OK");
			Close();
		}
	}

	private void btnCancel_Click(object sender, EventArgs e)
	{
		base.DialogResult = DialogResult.Cancel;
		Close();
	}

	private void cmboxSKUs_SelectedIndexChanged(object sender, EventArgs e)
	{
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
		this.cmboxSKUs = new System.Windows.Forms.ComboBox();
		this.label1 = new System.Windows.Forms.Label();
		this.btnCancel = new hmd_pctool_windows.Components.YelloButton();
		this.btnOK = new hmd_pctool_windows.Components.YelloButton();
		base.SuspendLayout();
		this.cmboxSKUs.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.cmboxSKUs.Font = new System.Drawing.Font("Calibri", 9f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.cmboxSKUs.FormattingEnabled = true;
		this.cmboxSKUs.Location = new System.Drawing.Point(81, 62);
		this.cmboxSKUs.Name = "cmboxSKUs";
		this.cmboxSKUs.Size = new System.Drawing.Size(241, 22);
		this.cmboxSKUs.TabIndex = 5;
		this.cmboxSKUs.SelectedIndexChanged += new System.EventHandler(cmboxSKUs_SelectedIndexChanged);
		this.label1.AutoSize = true;
		this.label1.Font = new System.Drawing.Font("Calibri", 12f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label1.ForeColor = System.Drawing.Color.White;
		this.label1.Location = new System.Drawing.Point(30, 27);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(138, 19);
		this.label1.TabIndex = 4;
		this.label1.Text = "Please select SKUID";
		this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnCancel.Font = new System.Drawing.Font("Calibri", 9f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnCancel.ForeColor = System.Drawing.Color.White;
		this.btnCancel.Location = new System.Drawing.Point(228, 142);
		this.btnCancel.Name = "btnCancel";
		this.btnCancel.Size = new System.Drawing.Size(87, 24);
		this.btnCancel.TabIndex = 3;
		this.btnCancel.TabStop = false;
		this.btnCancel.Text = "Cancel";
		this.btnCancel.UseVisualStyleBackColor = true;
		this.btnCancel.Click += new System.EventHandler(btnCancel_Click);
		this.btnOK.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnOK.Font = new System.Drawing.Font("Calibri", 9f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnOK.ForeColor = System.Drawing.Color.White;
		this.btnOK.Location = new System.Drawing.Point(81, 142);
		this.btnOK.Name = "btnOK";
		this.btnOK.Size = new System.Drawing.Size(87, 24);
		this.btnOK.TabIndex = 0;
		this.btnOK.TabStop = false;
		this.btnOK.Text = "OK";
		this.btnOK.UseVisualStyleBackColor = true;
		this.btnOK.Click += new System.EventHandler(btnOK_Click);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 12f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.Color.FromArgb(19, 36, 53);
		base.ClientSize = new System.Drawing.Size(396, 222);
		base.Controls.Add(this.cmboxSKUs);
		base.Controls.Add(this.label1);
		base.Controls.Add(this.btnCancel);
		base.Controls.Add(this.btnOK);
		base.Name = "SelectSKUIDForm";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		this.Text = "Please select SKUID";
		base.Load += new System.EventHandler(SelectSKUForm_Load);
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
