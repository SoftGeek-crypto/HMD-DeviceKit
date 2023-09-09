using System;

namespace hmd_pctool_windows;

public class BaseEventArgs : EventArgs
{
	public bool BoolArg { get; set; }

	public int IntArg { get; set; }

	public string StringArg { get; set; }
}
