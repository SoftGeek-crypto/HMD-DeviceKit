using System.Windows.Forms;

namespace hmd_pctool_windows.Components;

public class NoFocusCueButton : Button
{
	public NoFocusCueButton()
	{
		SetStyle(ControlStyles.Selectable, value: false);
	}
}
