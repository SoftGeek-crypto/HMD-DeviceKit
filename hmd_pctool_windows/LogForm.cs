using System;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace hmd_pctool_windows;

public class LogForm : Form
{
	private int index = -1;

	private StringBuilder logStringBuilder;

	private readonly string tag;

	private IContainer components = null;

	private TextBox textBoxLog;

	public LogForm()
	{
		InitializeComponent();
	}

	public LogForm(string title, StringBuilder sb)
		: this(title, sb, -1)
	{
	}

	public LogForm(string title, StringBuilder sb, int index)
		: this()
	{
		Text = title;
		logStringBuilder = sb;
		this.index = index;
		if (string.IsNullOrEmpty(Text))
		{
			tag = "LogForm";
		}
		else
		{
			tag = "LogForm-" + Text;
		}
	}

	private void LogForm_Load(object sender, EventArgs e)
	{
		textBoxLog.AppendText(logStringBuilder.ToString());
	}

	public void UpdateLog(string log)
	{
		if (!string.IsNullOrEmpty(log))
		{
			if (!log.EndsWith(Environment.NewLine))
			{
				log += Environment.NewLine;
			}
			textBoxLog.AppendText(log);
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
		this.textBoxLog = new System.Windows.Forms.TextBox();
		base.SuspendLayout();
		this.textBoxLog.Font = new System.Drawing.Font("Calibri", 9f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.textBoxLog.Location = new System.Drawing.Point(12, 12);
		this.textBoxLog.Multiline = true;
		this.textBoxLog.Name = "textBoxLog";
		this.textBoxLog.ReadOnly = true;
		this.textBoxLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
		this.textBoxLog.Size = new System.Drawing.Size(440, 437);
		this.textBoxLog.TabIndex = 0;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 12f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(464, 461);
		base.Controls.Add(this.textBoxLog);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
		base.MaximizeBox = false;
		base.Name = "LogForm";
		this.Text = "LogForm";
		base.Load += new System.EventHandler(LogForm_Load);
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
