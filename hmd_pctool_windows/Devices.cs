using System;
using System.Collections.Generic;

namespace hmd_pctool_windows;

internal class Devices : List<Device>
{
	public Devices()
	{
	}

	public Devices(List<string> snList)
	{
		if (snList == null)
		{
			return;
		}
		foreach (string sn in snList)
		{
			Add(new HmdDevice(sn));
		}
	}

	public void AddRange(List<string> deviceList)
	{
		foreach (string device in deviceList)
		{
			Add(new HmdDevice(device));
		}
	}

	public new void Add(Device device)
	{
		if (!Exists((Device e) => e.SN.Equals(device.SN)))
		{
			base.Add(device);
		}
	}

	public new void Remove(Device device)
	{
		if (Exists((Device e) => e.SN.Equals(device.SN)))
		{
			base.Remove(device);
		}
	}

	public void PrintDevices()
	{
		using Enumerator enumerator = GetEnumerator();
		while (enumerator.MoveNext())
		{
			HmdDevice hmdDevice = (HmdDevice)enumerator.Current;
			Console.WriteLine(hmdDevice.SN + " " + hmdDevice.DeviceStatus);
		}
	}
}
