using System;
using System.Drawing;
using System.Windows.Forms;

namespace hmd_pctool_windows.Components;

internal class YelloButton : NoFocusCueButton
{
	public YelloButton()
	{
		base.FlatAppearance.BorderColor = Color.FromArgb(253, 192, 3);
		base.FlatAppearance.BorderSize = 2;
		base.FlatAppearance.MouseDownBackColor = Color.Transparent;
		base.FlatAppearance.MouseOverBackColor = Color.FromArgb(253, 192, 3);
		base.FlatStyle = FlatStyle.Flat;
		Font = new Font("Calibri", 9f, FontStyle.Bold, GraphicsUnit.Point, 0);
		ForeColor = Color.White;
		base.TabStop = false;
		base.UseVisualStyleBackColor = true;
	}

	protected override void OnMouseHover(EventArgs e)
	{
		base.OnMouseHover(e);
		BackColor = Color.FromArgb(253, 192, 3);
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);
		BackColor = Color.Transparent;
	}

	protected override void OnEnabledChanged(EventArgs e)
	{
		base.OnEnabledChanged(e);
		BackColor = (base.Enabled ? Color.Transparent : Color.Gray);
		base.FlatAppearance.BorderColor = (base.Enabled ? Color.FromArgb(253, 192, 3) : Color.Gray);
	}
}
