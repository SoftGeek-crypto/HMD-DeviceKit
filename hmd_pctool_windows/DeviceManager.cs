using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace hmd_pctool_windows;

internal class DeviceManager
{
	public enum DmEventType
	{
		MonitoringStarted,
		MonitoringStopped,
		DevicesAdded,
		DevicesRemoved
	}

	private readonly Devices devices;

	private readonly BackgroundWorker worker;

	private readonly List<string> currentDeviceList;

	private readonly List<string> incommingDeviceList;

	private readonly List<string> diffList;

	private readonly List<string> addedDevices;

	private readonly List<string> removedDevices;

	private DmEventType dmEventType;

	private readonly StringBuilder sb;

	private int pollingInterval;

	private static readonly Lazy<DeviceManager> lazy = new Lazy<DeviceManager>(() => new DeviceManager());

	private CommandApi commandApi;

	private readonly long TIMEOUT_FOR_REMOVE_DEVICE = 3L;

	public static readonly Semaphore semaphoreUnzip = new Semaphore(1, 1);

	public static DeviceManager Instance => lazy.Value;

	public event EventHandler<DmEventArgs> EventHandler;

	private DeviceManager()
	{
		commandApi = new CommandApi("DeviceManager");
		worker = new BackgroundWorker
		{
			WorkerSupportsCancellation = true
		};
		worker.DoWork += DoWork;
		sb = new StringBuilder();
		currentDeviceList = new List<string>();
		incommingDeviceList = new List<string>();
		diffList = new List<string>();
		addedDevices = new List<string>();
		removedDevices = new List<string>();
		devices = new Devices();
		pollingInterval = 1000;
	}

	private void DoWork(object sender, DoWorkEventArgs e)
	{
		BackgroundWorker backgroundWorker = sender as BackgroundWorker;
		while (!backgroundWorker.CancellationPending)
		{
			GetDevice();
			Thread.Sleep(pollingInterval);
			bool flag = true;
		}
		e.Cancel = true;
	}

	public bool StartMonitoring()
	{
		if (!worker.IsBusy)
		{
			worker.RunWorkerAsync();
			dmEventType = DmEventType.MonitoringStarted;
			SendEvent(null, dmEventType);
			return true;
		}
		return false;
	}

	public async Task<bool> StartMonitoringAsync()
	{
		if (!worker.IsBusy)
		{
			worker.RunWorkerAsync();
			dmEventType = DmEventType.MonitoringStarted;
			SendEvent(null, dmEventType);
			return true;
		}
		if (worker.CancellationPending)
		{
			await WorkerIsNotBusy();
			worker.RunWorkerAsync();
			dmEventType = DmEventType.MonitoringStarted;
			SendEvent(null, dmEventType);
			return true;
		}
		return false;
	}

	private async Task<bool> WorkerIsNotBusy()
	{
		await Task.Run(delegate
		{
			while (worker.IsBusy)
			{
			}
		});
		return true;
	}

	public bool StopMonitoring()
	{
		if (worker.WorkerSupportsCancellation)
		{
			worker.CancelAsync();
			dmEventType = DmEventType.MonitoringStopped;
			SendEvent(null, dmEventType);
			currentDeviceList.Clear();
			incommingDeviceList.Clear();
			commandApi.ExitProcess();
			foreach (Device device in devices)
			{
				device.SetDeviceStatus(DeviceStatus.Offline);
			}
			return true;
		}
		return false;
	}

	private void GetDevice()
	{
		try
		{
			long timeStamp = GetTimeStamp();
			RemoveTimeOutDevices(timeStamp);
			ClearParameters();
			if (commandApi == null)
			{
			}
			CommandApi.ErrorCode errorCode = commandApi.GetDevices(sb);
			if (errorCode != 0 && errorCode != CommandApi.ErrorCode.NoReturn)
			{
				Console.WriteLine(errorCode.ToString());
				return;
			}
			if (!string.IsNullOrEmpty(sb.ToString()))
			{
				incommingDeviceList.AddRange(sb.ToString().Split(','));
			}
			IEnumerable<string> source = currentDeviceList.Except(incommingDeviceList);
			IEnumerable<string> source2 = incommingDeviceList.Except(currentDeviceList);
			diffList.AddRange(source.ToList());
			diffList.AddRange(source2.ToList());
			if (diffList.Count == 0)
			{
				return;
			}
			foreach (string diff in diffList)
			{
				if (incommingDeviceList.IndexOf(diff) != -1)
				{
					addedDevices.Add(diff);
				}
				else if (currentDeviceList.IndexOf(diff) != -1)
				{
					removedDevices.Add(diff);
				}
			}
			if (addedDevices.Count != 0)
			{
				dmEventType = DmEventType.DevicesAdded;
				Devices devices = new Devices();
				foreach (string sn2 in addedDevices)
				{
					Device device = this.devices.Find((Device x) => x.SN.Equals(sn2));
					if (device == null)
					{
						device = new HmdDevice(sn2);
					}
					if (device.DeviceStatus == DeviceStatus.TemporaryOffline && timeStamp - device.LastRemovedTime <= TIMEOUT_FOR_REMOVE_DEVICE)
					{
						device.SetDeviceStatus(DeviceStatus.Online);
					}
					else
					{
						devices.Add(device);
					}
					this.devices.Add(device);
					device.SetDeviceStatus(DeviceStatus.Online);
				}
				if (devices.Count > 0)
				{
					SendEvent(devices.ToArray(), dmEventType);
				}
			}
			if (removedDevices.Count != 0)
			{
				dmEventType = DmEventType.DevicesRemoved;
				foreach (string sn in removedDevices)
				{
					Device device2 = this.devices.Find((Device x) => x.SN.Equals(sn));
					if (device2 != null)
					{
						device2.SetDeviceStatus(DeviceStatus.TemporaryOffline);
						device2.SetLastRemovedTime(timeStamp);
					}
				}
			}
			if (diffList.Count != 0)
			{
				UpdateIncommingDeviceList();
			}
		}
		catch (Exception ex)
		{
			LogUtility.D("229 DM " + ex.Message, "\n" + ex.StackTrace);
		}
	}

	public void SetPollingInterval(int interval)
	{
		pollingInterval = interval;
	}

	private void SendEvent(Device[] device, DmEventType dmEventType)
	{
		if (this.EventHandler != null)
		{
			DmEventArgs e = new DmEventArgs
			{
				EventType = dmEventType,
				Devices = device
			};
			this.EventHandler(this, e);
		}
	}

	private void UpdateIncommingDeviceList()
	{
		currentDeviceList.Clear();
		currentDeviceList.AddRange(incommingDeviceList);
	}

	private void ClearParameters()
	{
		sb.Clear();
		addedDevices.Clear();
		removedDevices.Clear();
		diffList.Clear();
		incommingDeviceList.Clear();
	}

	public Device[] GetDeviceList()
	{
		if (worker.IsBusy)
		{
			return devices.ToArray();
		}
		return null;
	}

	private long GetTimeStamp()
	{
		return Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds);
	}

	private void RemoveTimeOutDevices(long timeStamp)
	{
		Devices devices = new Devices();
		foreach (Device device in this.devices)
		{
			if (device.DeviceStatus == DeviceStatus.TemporaryOffline && timeStamp - device.LastRemovedTime > TIMEOUT_FOR_REMOVE_DEVICE)
			{
				device.SetDeviceStatus(DeviceStatus.Offline);
				devices.Add(device);
			}
		}
		if (devices.Count > 0)
		{
			SendEvent(devices.ToArray(), dmEventType);
		}
	}
}
