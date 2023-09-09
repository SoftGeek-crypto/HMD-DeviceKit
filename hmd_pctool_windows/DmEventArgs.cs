namespace hmd_pctool_windows;

internal class DmEventArgs : BaseEventArgs
{
	public DeviceManager.DmEventType EventType { get; set; }

	public Device[] Devices { get; set; }
}
