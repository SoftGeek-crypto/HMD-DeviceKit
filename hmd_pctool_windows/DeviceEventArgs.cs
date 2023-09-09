namespace hmd_pctool_windows;

public class DeviceEventArgs : BaseEventArgs
{
	public DeviceEventType EventType { get; set; }

	public int What { get; set; }
}
