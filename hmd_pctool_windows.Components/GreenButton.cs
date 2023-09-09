using System;
using System.Drawing;
using System.Windows.Forms;

namespace hmd_pctool_windows.Components;

internal class GreenButton : NoFocusCueButton
{
	public GreenButton()
	{
		base.FlatAppearance.BorderColor = Color.FromArgb(65, 214, 171);
		base.FlatAppearance.BorderSize = 2;
		base.FlatAppearance.MouseDownBackColor = Color.White;
		base.FlatAppearance.MouseOverBackColor = Color.FromArgb(65, 214, 171);
		base.FlatStyle = FlatStyle.Flat;
		Font = new Font("Calibri", 9.75f, FontStyle.Bold, GraphicsUnit.Point, 0);
		base.UseVisualStyleBackColor = true;
	}

	protected override void OnMouseHover(EventArgs e)
	{
		base.OnMouseHover(e);
		BackColor = Color.FromArgb(65, 214, 171);
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);
		BackColor = Color.White;
	}

	protected override void OnEnabledChanged(EventArgs e)
	{
		base.OnEnabledChanged(e);
		base.FlatAppearance.BorderColor = (base.Enabled ? Color.FromArgb(65, 214, 171) : Color.Gray);
	}
}
