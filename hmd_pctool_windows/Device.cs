using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace hmd_pctool_windows;

public abstract class Device : IDevice
{
	protected class DeviceDoWorkArgs
	{
		public object Argument { get; }

		public DeviceDoWorkEventHandler Handler { get; }

		public DeviceEventType EventType { get; }

		public DeviceDoWorkArgs(DeviceDoWorkEventHandler handler, object argument = null, DeviceEventType eventType = DeviceEventType.NotDefine)
		{
			Argument = argument;
			Handler = handler;
			EventType = eventType;
		}
	}

	protected delegate void DeviceDoWorkEventHandler(object sender, DoWorkEventArgs e, object argument);

	private delegate int Func(object sender, DoWorkEventArgs e, object argument);

	public class SetValueArgs
	{
		private DeviceItemType type;

		private string value;

		public DeviceItemType Type => type;

		public string Value => value;

		public SetValueArgs(DeviceItemType type, string value)
		{
			this.type = type;
			this.value = value;
		}
	}

	private string serialNo;

	protected CommandApi commandApi;

	protected IAuthentication authenticationHandler;

	private BackgroundWorker backgroundWorker;

	protected DeviceEventType deviceEventType = DeviceEventType.NotDefine;

	protected int what = -1;

	private bool IsDebug = false;

	private DeviceStatus deviceStatus;

	protected DeviceInfo devInfo;

	protected readonly string Tag;

	private Queue<DeviceDoWorkArgs> workQueue = new Queue<DeviceDoWorkArgs>();

	private long lastRemovedTime;

	public static readonly long DefaultTime;

	public string SN => serialNo;

	public int What
	{
		get
		{
			return what;
		}
		set
		{
			what = value;
		}
	}

	public DeviceStatus DeviceStatus => deviceStatus;

	public long LastRemovedTime => lastRemovedTime;

	public bool IsWorkerCancel
	{
		get
		{
			if (backgroundWorker != null)
			{
				return backgroundWorker.CancellationPending;
			}
			return false;
		}
	}

	public event EventHandler<DeviceEventArgs> DeviceEventHandler;

	public void ExportInspectLog()
	{
		ArrangeWork(DoExportInspectLog);
	}

	protected virtual int DoExportInspectLog(object sender, DoWorkEventArgs e, object argument)
	{
		return 0;
	}

	public void SetDeviceStatus(DeviceStatus _deviceStatus)
	{
		StackTrace stackTrace = new StackTrace(fNeedFileInfo: true);
		if (stackTrace.GetFrame(1).GetMethod().DeclaringType.Name.Equals(DeviceManager.Instance.GetType().Name))
		{
			deviceStatus = _deviceStatus;
			return;
		}
		throw new InvalidOperationException("Can not be called by user");
	}

	public void SetLastRemovedTime(long timespan)
	{
		StackTrace stackTrace = new StackTrace(fNeedFileInfo: true);
		if (stackTrace.GetFrame(1).GetMethod().DeclaringType.Name.Equals(DeviceManager.Instance.GetType().Name))
		{
			lastRemovedTime = timespan;
			return;
		}
		throw new InvalidOperationException("Can not be called by user");
	}

	protected Device(string serialNo)
	{
		this.serialNo = serialNo;
		backgroundWorker = new BackgroundWorker
		{
			WorkerReportsProgress = true,
			WorkerSupportsCancellation = true
		};
		backgroundWorker.DoWork += DoWorkEventHandler;
		backgroundWorker.ProgressChanged += ProgressChangedEventHandler;
		backgroundWorker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs args)
		{
			RunWorkerCompletedEventHandler(sender, args);
			if (workQueue.Count > 0 && !backgroundWorker.IsBusy)
			{
				DeviceDoWorkArgs argument = workQueue.Dequeue();
				backgroundWorker.RunWorkerAsync(argument);
			}
		};
		authenticationHandler = AuthenticationFactory.GetAuthenticationHandler(this);
		commandApi = new CommandApi(this, CommandUtility.DefaultFastboot, CommandUtility.DefaultAdb);
		Tag = GetType().Name + "-" + serialNo;
		deviceStatus = DeviceStatus.Offline;
		lastRemovedTime = DefaultTime;
	}

	private bool AwaitLastWork()
	{
		while (backgroundWorker.IsBusy)
		{
			Thread.Sleep(100);
		}
		return true;
	}

	private void ArrangeWork(Func func, object funcArg = null, DeviceEventType type = DeviceEventType.NotDefine)
	{
		DeviceDoWorkArgs deviceDoWorkArgs = new DeviceDoWorkArgs(delegate(object sender, DoWorkEventArgs e, object argument)
		{
			func(sender, e, argument);
		}, funcArg, type);
		if (backgroundWorker != null && !backgroundWorker.IsBusy)
		{
			backgroundWorker.RunWorkerAsync(deviceDoWorkArgs);
		}
		else
		{
			workQueue.Enqueue(deviceDoWorkArgs);
		}
	}

	private void DoWorkEventHandler(object sender, DoWorkEventArgs e)
	{
		if (e != null && e.Argument != null)
		{
			DeviceDoWorkArgs deviceDoWorkArgs = e.Argument as DeviceDoWorkArgs;
			deviceEventType = deviceDoWorkArgs.EventType;
			deviceDoWorkArgs.Handler(sender, e, deviceDoWorkArgs.Argument);
		}
	}

	private void ProgressChangedEventHandler(object sender, ProgressChangedEventArgs e)
	{
		int num = -1;
		switch (deviceEventType)
		{
		case DeviceEventType.OnFlashStatus:
			num = 1;
			break;
		case DeviceEventType.OnUnlockFrpStatus:
			num = 1;
			break;
		}
		if (num > -1)
		{
			InvokeDeviceEventHandler(deviceEventType, num, e.ProgressPercentage);
		}
	}

	private void RunWorkerCompletedEventHandler(object sender, RunWorkerCompletedEventArgs e)
	{
		InvokeDeviceEventHandler(deviceEventType, what, e.Result);
		deviceEventType = DeviceEventType.NotDefine;
		what = -1;
	}

	protected void InvokeDeviceEventHandler(DeviceEventType type, int what, bool boolValue, int intValue, string stringValue)
	{
		DeviceEventArgs args = new DeviceEventArgs
		{
			EventType = type,
			What = what,
			BoolArg = boolValue,
			IntArg = intValue,
			StringArg = stringValue
		};
		InvokeDeviceEventHandler(args);
	}

	protected void InvokeUpdateLogEvent(string log)
	{
		PrintLog(log);
		LogUtility.AppendFlashLog(log);
		InvokeDeviceEventHandler(DeviceEventType.UpdateLog, 0, log);
	}

	protected void InvokeDeviceEventHandler(DeviceEventType type, int what, object value)
	{
		DeviceEventArgs deviceEventArgs = new DeviceEventArgs
		{
			EventType = type,
			What = what
		};
		if (value != null)
		{
			if (value is int)
			{
				deviceEventArgs.IntArg = (int)value;
			}
			else if (value is string)
			{
				deviceEventArgs.StringArg = (string)value;
			}
			else if (value is bool)
			{
				deviceEventArgs.BoolArg = (bool)value;
			}
		}
		InvokeDeviceEventHandler(deviceEventArgs);
	}

	private void InvokeDeviceEventHandler(DeviceEventArgs args)
	{
		if (this.DeviceEventHandler == null)
		{
			return;
		}
		Delegate[] invocationList = this.DeviceEventHandler.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			EventHandler<DeviceEventArgs> eventHandler = (EventHandler<DeviceEventArgs>)invocationList[i];
			ISynchronizeInvoke synchronizeInvoke = eventHandler.Target as ISynchronizeInvoke;
			try
			{
				if (synchronizeInvoke != null && synchronizeInvoke.InvokeRequired)
				{
					synchronizeInvoke.Invoke(eventHandler, new object[2] { this, args });
				}
				else
				{
					eventHandler(this, args);
				}
			}
			catch
			{
				LogUtility.E("334 Device InvokeDevice ", "");
			}
		}
	}

	protected void UpdateProgressChanged(int progress)
	{
		Console.WriteLine("progress " + progress);
		backgroundWorker.ReportProgress(progress);
	}

	public void CancelWork()
	{
		workQueue.Clear();
		if (backgroundWorker != null && backgroundWorker.IsBusy)
		{
			backgroundWorker.CancelAsync();
		}
	}

	protected void RequestPermission(CommandType type)
	{
		ArrangeWork(DoRequestPermission);
	}

	protected virtual int DoRequestPermission(object sender, DoWorkEventArgs e, object argument)
	{
		return 0;
	}

	public void RebootEdl()
	{
		ArrangeWork(DoRebootEdl);
	}

	protected virtual int DoRebootEdl(object sender, DoWorkEventArgs e, object argument)
	{
		return DoRequestPermission(sender, e, CommandType.RebootEdl);
	}

	public void LockBootloader()
	{
		ArrangeWork(DoLockBootloader);
	}

	protected virtual int DoLockBootloader(object sender, DoWorkEventArgs e, object argument)
	{
		return DoRequestPermission(sender, e, CommandType.LockBootloader);
	}

	public void BootToSystem()
	{
		ArrangeWork(DoBootToSystem);
	}

	protected virtual int DoBootToSystem(object sender, DoWorkEventArgs e, object argument)
	{
		return DoRequestPermission(sender, e, CommandType.BootToSystem);
	}

	public void GetVar(DeviceVarType type)
	{
		ArrangeWork(DoGetVar, type, DeviceEventType.OnGetVar);
	}

	private int DoGetVar(object sender, DoWorkEventArgs e, object argument)
	{
		if (DoRequestPermission(sender, e, argument) != 0)
		{
			return -1;
		}
		return 0;
	}

	public void GetAntiTheftStatus()
	{
		ArrangeWork(DoGetAntiTheftStatus, DeviceEventType.NotDefine);
	}

	protected virtual int DoGetAntiTheftStatus(object sender, DoWorkEventArgs e, object argument)
	{
		return DoRequestPermission(sender, e, CommandType.NoRequired);
	}

	public void ReadItem(DeviceItemType type)
	{
		ArrangeWork(DoReadItem, type, DeviceEventType.OnReadItem);
	}

	protected virtual int DoReadItem(object sender, DoWorkEventArgs e, object argument)
	{
		return DoRequestPermission(sender, e, CommandType.ReadItem);
	}

	public void WriteItem(DeviceItemType type, string value)
	{
		if (backgroundWorker != null && !backgroundWorker.IsBusy)
		{
			deviceEventType = DeviceEventType.OnWriteItem;
			DeviceDoWorkArgs argument2 = new DeviceDoWorkArgs(delegate(object sender, DoWorkEventArgs e, object argument)
			{
				DoWriteItem(sender, e, argument);
			}, new SetValueArgs(type, value));
			backgroundWorker.RunWorkerAsync(argument2);
		}
	}

	protected virtual int DoWriteItem(object sender, DoWorkEventArgs e, object argument)
	{
		return DoRequestPermission(sender, e, CommandType.WriteItem);
	}

	public void GetUnlockFrpResult()
	{
		ArrangeWork(DoGetUnlockFrpResult);
	}

	protected virtual int DoGetUnlockFrpResult(object sender, DoWorkEventArgs e, object argument)
	{
		return DoRequestPermission(sender, e, CommandType.UnlockFrp);
	}

	public void Flash(string path)
	{
		ArrangeWork(DoFlash, path, DeviceEventType.OnFlashStatus);
	}

	protected virtual int DoFlash(object sender, DoWorkEventArgs e, object argument)
	{
		return DoRequestPermission(sender, e, CommandType.Flash);
	}

	public void OtaUpdate(string path)
	{
		ArrangeWork(DoOtaUpdate, path, DeviceEventType.OnFlashStatus);
	}

	protected virtual int DoOtaUpdate(object sender, DoWorkEventArgs e, object argument)
	{
		return DoRequestPermission(sender, e, CommandType.OtaUpdate);
	}

	public void UnlockFrp()
	{
		ArrangeWork(DoUnlockFrp);
	}

	protected virtual int DoUnlockFrp(object sender, DoWorkEventArgs e, object argument)
	{
		return DoRequestPermission(sender, e, CommandType.UnlockFrp);
	}

	public void FactoryReset()
	{
		ArrangeWork(DoFactoryReset);
	}

	protected virtual int DoFactoryReset(object sender, DoWorkEventArgs e, object argument)
	{
		return DoRequestPermission(sender, e, CommandType.FactoryResets);
	}

	public void StartPhoneEdit()
	{
		ArrangeWork(DoStartPhoneEdit);
	}

	protected virtual int DoStartPhoneEdit(object sender, DoWorkEventArgs e, object argument)
	{
		return DoRequestPermission(sender, e, CommandType.StartPhoneEdit);
	}

	public void StartSimLock()
	{
		ArrangeWork(DoStartSimLock);
	}

	protected virtual int DoStartSimLock(object sender, DoWorkEventArgs e, object argument)
	{
		return DoRequestPermission(sender, e, CommandType.StartSimlock);
	}

	public void StopPhoneEditSimLock()
	{
		ArrangeWork(DoStopPhoneEditSimLock);
	}

	protected virtual int DoStopPhoneEditSimLock(object sender, DoWorkEventArgs e, object argument)
	{
		return 0;
	}

	public void SimLock(string filePath)
	{
		ArrangeWork(DoSimLock, filePath);
	}

	protected virtual int DoSimLock(object sender, DoWorkEventArgs e, object argument)
	{
		return DoRequestPermission(sender, e, CommandType.SimLock);
	}

	public void SimUnlock(UnLockKeys unLockKeys)
	{
		ArrangeWork(DoSimUnlock, unLockKeys);
	}

	protected virtual int DoSimUnlock(object sender, DoWorkEventArgs e, object argument)
	{
		return DoRequestPermission(sender, e, CommandType.SimUnlock);
	}

	public void SaveUnlockFrpResult()
	{
		ArrangeWork(DoSaveUnlockFrpResult);
	}

	protected virtual int DoSaveUnlockFrpResult(object sender, EventArgs e)
	{
		return 0;
	}

	protected virtual int DoSaveUnlockFrpResult(object sender, DoWorkEventArgs e, object argument)
	{
		return DoRequestPermission(sender, e, CommandType.UnlockFrp);
	}

	public void FrpErase()
	{
		ArrangeWork(DoFrpErase);
	}

	protected virtual int DoFrpErase(object sender, DoWorkEventArgs e, object argument)
	{
		return DoRequestPermission(sender, e, CommandType.FrpErase);
	}

	public void GetSku()
	{
		ArrangeWork(DoGetSku, DeviceEventType.OnGetVar);
	}

	protected virtual int DoGetSku(object sender, DoWorkEventArgs e, object argument)
	{
		return DoRequestPermission(sender, e, CommandType.GetSku);
	}

	protected void PrintLog(string log)
	{
		if (IsDebug || Program.GlobalDebugFlag)
		{
			LogUtility.D(Tag, log);
		}
	}

	public void ExitFastbootProcess()
	{
		if (commandApi != null)
		{
			commandApi.ExitProcess();
		}
	}
}
