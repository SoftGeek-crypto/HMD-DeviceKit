using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace hmd_pctool_windows;

public class CommandApi
{
	public enum ErrorCode
	{
		NoError,
		ExeNotExistOrNotApprove,
		NotInit,
		RequestPermissionFail,
		OperationFail,
		DeviceNotConnect,
		NoReturn,
		FailFromFastboot,
		PermissionDenied,
		FlashAbort,
		DeviceNotSet,
		CommandNotSupport,
		ExitWithErrorCode,
		OtaFailWithAdbNoError,
		NoEnoughMemory,
		JsonFormatError,
		ErrorNotDefine,
		FileNotFound,
		FailWithMaxRetry
	}

	public delegate void UpdateLog(string log);

	public enum AdbMode
	{
		device,
		recovery,
		sideload
	}

	private static readonly bool isDebug = false;

	private static readonly string staticTag = "CommandApi";

	private readonly object execLock = new object();

	private static readonly int defaultTimeout = 3000;

	private static readonly int waitInterval = 1000;

	private static readonly int defaultRetryTimes = 3;

	private readonly string tag;

	private Device device;

	private string fastbootPath;

	private string adbPath;

	private Process process;

	private int waitDeviceTimeout = defaultTimeout;

	public CommandApi(Device device)
		: this(device, null, null)
	{
	}

	public CommandApi(string client)
		: this(null, null, null)
	{
		if (!string.IsNullOrEmpty(client))
		{
			tag = staticTag + "-" + client;
		}
	}

	public CommandApi(Device device, string fastbootPath, string adbPath)
	{
		this.device = device;
		if (this.device != null)
		{
			tag = staticTag + "-" + device.SN;
		}
		if (string.IsNullOrEmpty(fastbootPath))
		{
			this.fastbootPath = CommandUtility.DefaultFastboot;
		}
		else
		{
			this.fastbootPath = fastbootPath;
		}
		if (string.IsNullOrEmpty(adbPath))
		{
			this.adbPath = CommandUtility.DefaultAdb;
		}
		else
		{
			this.adbPath = adbPath;
		}
	}

	public void SetTimeout(int timeoutInSecond)
	{
		waitDeviceTimeout = timeoutInSecond * 1000;
	}

	private string ExecAdbCommand(string arguments)
	{
		return ExecAdbCommand(arguments, null);
	}

	private string ExecAdbCommand(string arguments, UpdateLog updateLog)
	{
		return ExecCommand(isFastboot: false, arguments, updateLog);
	}

	public string ExecFastbootCommand(string arguments)
	{
		return ExecFastbootCommand(arguments, null);
	}

	private string ExecFastbootCommand(string arguments, UpdateLog updateLog)
	{
		return ExecCommand(isFastboot: true, arguments, updateLog);
	}

	private string ExecCommand(bool isFastboot, string arguments, UpdateLog updateLog)
	{
		lock (execLock)
		{
			if (isFastboot)
			{
				if (updateLog == null)
				{
					return ExecFastbootWithoutLog(arguments);
				}
				return ExecFastbootWithLog(arguments, updateLog);
			}
			return ExecAdb(arguments, updateLog);
		}
	}

	private string ExecAdb(string arguments, UpdateLog updateLog)
	{
		if (CommandUtility.CheckAdbStatus(adbPath) != 0)
		{
			return ErrorCode.ExeNotExistOrNotApprove.ToString();
		}
		process = new Process();
		process.StartInfo.FileName = adbPath;
		process.StartInfo.Arguments = arguments;
		process.StartInfo.UseShellExecute = false;
		process.StartInfo.CreateNoWindow = true;
		process.StartInfo.RedirectStandardOutput = true;
		process.StartInfo.RedirectStandardError = true;
		StringBuilder stdout = new StringBuilder();
		process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e)
		{
			if (updateLog != null)
			{
				updateLog(e.Data);
			}
			stdout.AppendLine(e.Data);
		};
		process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e)
		{
			if (updateLog != null)
			{
				updateLog(e.Data);
			}
			stdout.AppendLine(e.Data);
		};
		process.Start();
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();
		process.WaitForExit();
		int exitCode = process.ExitCode;
		process.Close();
		PrintLog($"ExecAdb: command={arguments}, exit={exitCode}, res={stdout.ToString().Trim()}");
		if (exitCode == 0)
		{
			return stdout.ToString();
		}
		return $"{ErrorCode.ExitWithErrorCode.ToString()}#{exitCode}:{stdout.ToString()}";
	}

	private string ExecFastbootWithLog(string arguments, UpdateLog updateLog)
	{
		if (CommandUtility.CheckFastbootStatus(fastbootPath) != 0)
		{
			return ErrorCode.ExeNotExistOrNotApprove.ToString();
		}
		process = new Process();
		process.StartInfo.FileName = fastbootPath;
		process.StartInfo.Arguments = arguments;
		process.StartInfo.UseShellExecute = false;
		process.StartInfo.CreateNoWindow = true;
		process.StartInfo.RedirectStandardOutput = true;
		process.StartInfo.RedirectStandardError = true;
		StringBuilder stdout = new StringBuilder();
		process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e)
		{
			if (updateLog != null)
			{
				updateLog(e.Data);
			}
			stdout.AppendLine(e.Data);
		};
		process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e)
		{
			if (updateLog != null)
			{
				updateLog(e.Data);
			}
			stdout.AppendLine(e.Data);
		};
		process.Start();
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();
		process.WaitForExit();
		int exitCode = process.ExitCode;
		process.Close();
		PrintLog($"ExecFastbootCommand: command={arguments}, exit={exitCode}, res={stdout}");
		if (exitCode == 0)
		{
			return stdout.ToString();
		}
		return $"{ErrorCode.ExitWithErrorCode.ToString()}#{exitCode}:{stdout.ToString()}";
	}

	private string ExecFastbootWithoutLog(string arguments)
	{
		if (CommandUtility.CheckFastbootStatus(fastbootPath) != 0)
		{
			return ErrorCode.ExeNotExistOrNotApprove.ToString();
		}
		process = new Process();
		process.StartInfo.FileName = fastbootPath;
		process.StartInfo.Arguments = arguments;
		process.StartInfo.UseShellExecute = false;
		process.StartInfo.RedirectStandardOutput = true;
		process.StartInfo.CreateNoWindow = true;
		process.StartInfo.RedirectStandardError = true;
		process.Start();
		process.WaitForExit();
		string text = process.StandardOutput.ReadToEnd();
		string text2 = process.StandardError.ReadToEnd();
		int exitCode = process.ExitCode;
		process.Close();
		if (text.Length == 0)
		{
			PrintLog($"ExecFastbootCommandWithoutLog: command={arguments}, exit={exitCode}, res={text2}");
			if (exitCode == 0)
			{
				return text2;
			}
			return $"{ErrorCode.ExitWithErrorCode.ToString()}#{exitCode}:{text2}";
		}
		PrintLog($"ExecFastbootCommandWithoutLog: command={arguments}, exit={exitCode}, res={text}");
		if (exitCode == 0)
		{
			return text;
		}
		return $"{ErrorCode.ExitWithErrorCode.ToString()}#{exitCode}:{text}";
	}

	public ErrorCode GetAdbDevices(StringBuilder devices)
	{
		return GetAdbDevices(devices, isRequireRes: false);
	}

	public ErrorCode GetAdbDevices(StringBuilder devices, bool isRequireRes)
	{
		for (int i = 1; i <= defaultRetryTimes; i++)
		{
			string command = CommandUtility.GetCommand("devices");
			string text = ExecAdbCommand(command);
			ErrorCode errorCode = HandleResponse(text);
			if (errorCode != 0 && i == defaultRetryTimes)
			{
				return errorCode;
			}
			if (isRequireRes)
			{
				devices.Append(text);
				return errorCode;
			}
			string[] array = text.Replace("\r\n", "\n").Split('\n');
			for (int j = 0; j < array.Length; j++)
			{
				if (string.IsNullOrEmpty(array[j]) || array[j].Contains("* daemon") || array[j].Contains("attached"))
				{
					continue;
				}
				string text2 = string.Empty;
				if (!array[j].Contains("offline"))
				{
					if (array[j].Contains("device"))
					{
						text2 = "device";
					}
					else if (array[j].Contains("recovery"))
					{
						text2 = "recovery";
					}
					else if (array[j].Contains("sideload"))
					{
						text2 = "sideload";
					}
					if (!string.IsNullOrEmpty(text2))
					{
						string text3 = array[j].Replace(text2, "").TrimEnd();
						devices.Append(text3 + ",");
					}
				}
			}
			if (devices.ToString().EndsWith(","))
			{
				devices.Remove(devices.Length - 1, 1);
			}
			PrintLog("GetAdbDevices: " + command);
			PrintLog("res: " + devices);
			if (devices.Length == 0)
			{
				return ErrorCode.NoReturn;
			}
		}
		return ErrorCode.FailWithMaxRetry;
	}

	public ErrorCode GetDevices(StringBuilder devices)
	{
		string command = CommandUtility.GetCommand("devices");
		string text = ExecFastbootCommand(command);
		ErrorCode errorCode = HandleResponse(text);
		if (errorCode != 0)
		{
			return errorCode;
		}
		string[] array = text.Replace("\r\n", "\n").Split('\n');
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (!string.IsNullOrEmpty(text2))
			{
				string text3 = text2.Replace("fastboot", "").TrimEnd();
				if (!text3.Contains("?"))
				{
					devices.Append(text3 + ",");
				}
			}
		}
		if (devices.ToString().EndsWith(","))
		{
			devices.Remove(devices.Length - 1, 1);
		}
		return ErrorCode.NoError;
	}

	public ErrorCode GetSignKey(StringBuilder key)
	{
		return GetSignKey(key, -1);
	}

	public ErrorCode GetSignKey(StringBuilder key, int timeout)
	{
		if (!IsFastbootDeviceConnectedWithTimeout(timeout))
		{
			return ErrorCode.DeviceNotConnect;
		}
		if (device == null || string.IsNullOrEmpty(device.SN))
		{
			return ErrorCode.DeviceNotSet;
		}
		string command = CommandUtility.GetCommand("oem", "sw_pub_key", " -s", device.SN);
		string text = ExecFastbootCommand(command);
		ErrorCode errorCode = HandleResponse(text);
		if (errorCode != 0)
		{
			LogUtility.D("393 CMDAPI", "Can't able to get pub key, failed for" + device.SN + Environment.NewLine + text.ToString());
			return errorCode;
		}
		string bootloaderValue = CommandUtility.GetBootloaderValue(text);
		if (bootloaderValue.Equals(ErrorCode.OperationFail.ToString()))
		{
			LogUtility.D("399 CMDAPI", "can't able to load bl value for " + device.SN);
			return ErrorCode.OperationFail;
		}
		key.Append(bootloaderValue);
		PrintLog("KeyValue: " + command);
		return ErrorCode.NoError;
	}

	public ErrorCode AuthStart(StringBuilder nounce)
	{
		return AuthStart(nounce, -1);
	}

	public ErrorCode AuthStart(StringBuilder nounce, int timeout)
	{
		if (!IsFastbootDeviceConnectedWithTimeout(timeout))
		{
			return ErrorCode.DeviceNotConnect;
		}
		if (device == null || string.IsNullOrEmpty(device.SN))
		{
			return ErrorCode.DeviceNotSet;
		}
		string command = CommandUtility.GetCommand("oem", "auth_start", "-s", device.SN);
		string text = ExecFastbootCommand(command);
		ErrorCode errorCode = HandleResponse(text);
		if (errorCode != 0)
		{
			LogUtility.D("393 CMDAPI", "Can't able to start auth, failed for" + device.SN + Environment.NewLine + text.ToString());
			return errorCode;
		}
		string bootloaderValue = CommandUtility.GetBootloaderValue(text);
		if (bootloaderValue.Equals(ErrorCode.OperationFail.ToString()))
		{
			LogUtility.D("399 CMDAPI", "can't able to load bl value for " + device.SN);
			return ErrorCode.OperationFail;
		}
		nounce.Append(bootloaderValue);
		PrintLog("res: " + nounce);
		return ErrorCode.NoError;
	}

	public ErrorCode ContinueBoot()
	{
		return ContinueBoot(-1);
	}

	public ErrorCode ContinueBoot(int timeout)
	{
		if (!IsFastbootDeviceConnectedWithTimeout(timeout))
		{
			return ErrorCode.DeviceNotConnect;
		}
		if (device == null || string.IsNullOrEmpty(device.SN))
		{
			return ErrorCode.DeviceNotSet;
		}
		string command = CommandUtility.GetCommand("continue", "-s", device.SN);
		PrintLog("continueBoot: " + command);
		ExecFastbootCommand(command);
		return ErrorCode.NoError;
	}

	public ErrorCode Erase(string partition, UpdateLog updateLog)
	{
		return Erase(partition, updateLog, -1);
	}

	public ErrorCode Erase(string partition, UpdateLog updateLog, int timeout)
	{
		if (!IsFastbootDeviceConnectedWithTimeout())
		{
			return ErrorCode.DeviceNotConnect;
		}
		if (device == null || string.IsNullOrEmpty(device.SN))
		{
			return ErrorCode.DeviceNotSet;
		}
		string command = CommandUtility.GetCommand("erase", partition, "-s", device.SN);
		string text = ExecFastbootCommand(command, updateLog);
		ErrorCode errorCode = HandleResponse(text);
		if (errorCode != 0)
		{
			LogUtility.D("449 CMDAPI ", partition + " erase failed for " + device.SN + Environment.NewLine + text.ToString());
			return errorCode;
		}
		PrintLog("erase: " + command);
		return ErrorCode.NoError;
	}

	public ErrorCode EraseAll(UpdateLog updateLog)
	{
		return EraseAll(updateLog, -1);
	}

	public ErrorCode EraseAll(UpdateLog updateLog, int timeout)
	{
		if (!IsFastbootDeviceConnectedWithTimeout())
		{
			return ErrorCode.DeviceNotConnect;
		}
		if (device == null || string.IsNullOrEmpty(device.SN))
		{
			return ErrorCode.DeviceNotSet;
		}
		string command = CommandUtility.GetCommand("-w", "-s", device.SN);
		string text = ExecFastbootCommand(command, updateLog);
		ErrorCode errorCode = HandleResponse(text);
		if (errorCode != 0)
		{
			LogUtility.D("476 CMDAPI ", "erase all failed for " + device.SN + Environment.NewLine + text.ToString());
			return errorCode;
		}
		PrintLog("EraseAll: " + command);
		return ErrorCode.NoError;
	}

	public ErrorCode FlashZip(string partition, string path, UpdateLog updateLog, string sparse_value = null)
	{
		return FlashZip(partition, path, updateLog, -1, sparse_value);
	}

	public ErrorCode FlashZip(string partition, string path, UpdateLog updateLog, int timeout, string sparse_value = null)
	{
		if (!IsFastbootDeviceConnectedWithTimeout(timeout))
		{
			return ErrorCode.DeviceNotConnect;
		}
		if (device == null || string.IsNullOrEmpty(device.SN))
		{
			return ErrorCode.DeviceNotSet;
		}
		if (!File.Exists(path))
		{
			return ErrorCode.FileNotFound;
		}
		if (!path.StartsWith("\"") || !path.EndsWith("\""))
		{
			path = "\"" + path + "\"";
		}
		object obj;
		switch (partition)
		{
		default:
			obj = "flash";
			break;
		case "boot":
		case "boot_a":
		case "boot_b":
			obj = "flash:raw";
			break;
		}
		string text = (string)obj;
		string text2 = ((sparse_value == null) ? "" : ("-S " + sparse_value));
		string command = CommandUtility.GetCommand(text, partition, path, text2, "-s", device.SN);
		string text3 = ExecFastbootCommand(command, updateLog);
		ErrorCode errorCode = HandleResponse(text3);
		if (errorCode != 0)
		{
			LogUtility.D("513 CMDAPI ", "flash " + partition + " failed for " + device.SN + "Res: " + text3.ToString());
			return errorCode;
		}
		PrintLog("flashZip: " + command);
		return ErrorCode.NoError;
	}

	public ErrorCode GetAdbDeviceInfo(StringBuilder infoJson)
	{
		return GetAdbDeviceInfo(infoJson, -1);
	}

	public ErrorCode GetAdbDeviceInfo(StringBuilder infoJson, int timeout)
	{
		if (!IsAdbDeviceConnectedWithTimeout(timeout))
		{
			return ErrorCode.DeviceNotConnect;
		}
		if (device == null || string.IsNullOrEmpty(device.SN))
		{
			return ErrorCode.DeviceNotSet;
		}
		string command = CommandUtility.GetCommand("-s", device.SN, "shell", "get_devinfo");
		string text = ExecAdbCommand(command);
		ErrorCode errorCode = HandleResponse(text);
		if (errorCode != 0)
		{
			return errorCode;
		}
		if (string.IsNullOrEmpty(text))
		{
			return ErrorCode.OperationFail;
		}
		infoJson.Append(text.Trim());
		PrintLog("GetAdbDeviceInfo: " + command);
		PrintLog("res: " + infoJson);
		return ErrorCode.NoError;
	}

	public ErrorCode GetDeviceInfo(StringBuilder infoJson)
	{
		return GetDeviceInfo(infoJson, -1);
	}

	public ErrorCode GetDeviceInfo(StringBuilder infoJson, int timeout)
	{
		if (!IsFastbootDeviceConnectedWithTimeout(timeout))
		{
			return ErrorCode.DeviceNotConnect;
		}
		if (device == null || string.IsNullOrEmpty(device.SN))
		{
			return ErrorCode.DeviceNotSet;
		}
		string command = CommandUtility.GetCommand("oem", "get_devinfo", "-s", device.SN);
		string text = ExecFastbootCommand(command);
		ErrorCode errorCode = HandleResponse(text);
		if (errorCode != 0)
		{
			LogUtility.D("571 CMDAPI ", "getdevinfo failed for " + device.SN + Environment.NewLine + text.ToString());
			return errorCode;
		}
		string bootloaderJson = CommandUtility.GetBootloaderJson(text);
		if (bootloaderJson.Equals(ErrorCode.OperationFail.ToString()))
		{
			LogUtility.D("577 CMDAPI ", "can't load the json");
			return ErrorCode.OperationFail;
		}
		infoJson.Append(bootloaderJson.Trim());
		PrintLog("res: " + infoJson);
		return ErrorCode.NoError;
	}

	public ErrorCode GetDllName(StringBuilder version)
	{
		return GetDllName(version, -1);
	}

	public ErrorCode GetDllName(StringBuilder version, int timeout)
	{
		if (!IsFastbootDeviceConnectedWithTimeout(timeout))
		{
			return ErrorCode.DeviceNotConnect;
		}
		if (device == null || string.IsNullOrEmpty(device.SN))
		{
			return ErrorCode.DeviceNotSet;
		}
		string command = CommandUtility.GetCommand("oem", "getdllname", "-s", device.SN);
		string text = ExecFastbootCommand(command);
		ErrorCode errorCode = HandleResponse(text);
		if (errorCode != 0)
		{
			LogUtility.D("606 CMDAPI ", "dllname failed for " + device.SN + Environment.NewLine + text.ToString());
			return errorCode;
		}
		string oemValue = CommandUtility.GetOemValue(text);
		if (oemValue.Equals(ErrorCode.OperationFail.ToString()))
		{
			LogUtility.D("getdllname response doesn't contain \"dllname=\"", "");
			return ErrorCode.OperationFail;
		}
		LogUtility.D("getdllname response contains \"dllname=\"" + oemValue, "");
		version.Append(oemValue);
		PrintLog("res: " + version);
		return ErrorCode.NoError;
	}

	public ErrorCode GetLevel(ref int level)
	{
		return GetLevel(ref level, -1);
	}

	public ErrorCode GetLevel(ref int level, int timeout)
	{
		if (!IsFastbootDeviceConnectedWithTimeout(timeout))
		{
			return ErrorCode.DeviceNotConnect;
		}
		if (device == null || string.IsNullOrEmpty(device.SN))
		{
			return ErrorCode.DeviceNotSet;
		}
		string command = CommandUtility.GetCommand("oem", "battery", "getcapacity", "-s", device.SN);
		string text = ExecFastbootCommand(command);
		ErrorCode errorCode = HandleResponse(text);
		if (errorCode != 0)
		{
			return errorCode;
		}
		string oemValue = CommandUtility.GetOemValue(text);
		try
		{
			level = int.Parse(oemValue);
		}
		catch (FormatException)
		{
			LogUtility.E(staticTag, "654 CMDAPI Fail in getLevel: Can not parse cap to int.");
			return ErrorCode.OperationFail;
		}
		PrintLog("res: " + level);
		return ErrorCode.NoError;
	}

	public ErrorCode GetPermission(ref byte permission)
	{
		return GetPermission(ref permission, -1);
	}

	public ErrorCode GetPermission(ref byte permission, int timeout)
	{
		if (!IsFastbootDeviceConnectedWithTimeout(timeout))
		{
			return ErrorCode.DeviceNotConnect;
		}
		string command = CommandUtility.GetCommand("oem", "getpermission", "-s", device.SN);
		string text = ExecFastbootCommand(command);
		ErrorCode errorCode = HandleResponse(text);
		if (errorCode != 0)
		{
			LogUtility.D("678 CMDAPI ", "getPer failed for " + device.SN + Environment.NewLine + text.ToString());
			return errorCode;
		}
		string oemValue = CommandUtility.GetOemValue(text);
		try
		{
			permission = byte.Parse(oemValue);
		}
		catch (FormatException)
		{
			LogUtility.E(staticTag, " 689 CMDAPI Fail in GetPermission: Can not parse oem value to byte.");
			return ErrorCode.OperationFail;
		}
		PrintLog("res: " + permission);
		return ErrorCode.NoError;
	}

	public ErrorCode GetSecurityVersion(StringBuilder version)
	{
		return GetSecurityVersion(version, -1);
	}

	public ErrorCode GetSecurityVersion(StringBuilder version, int timeout)
	{
		if (!IsFastbootDeviceConnectedWithTimeout(timeout))
		{
			return ErrorCode.DeviceNotConnect;
		}
		if (device == null || string.IsNullOrEmpty(device.SN))
		{
			return ErrorCode.DeviceNotSet;
		}
		string command = CommandUtility.GetCommand("oem", "getsecurityversion", "-s", device.SN);
		string text = ExecFastbootCommand(command);
		ErrorCode errorCode = HandleResponse(text);
		if (errorCode != 0)
		{
			LogUtility.D("717 CMDAPI ", "getsecV failed for " + device.SN + Environment.NewLine + text.ToString());
			return errorCode;
		}
		string bootloaderValue = CommandUtility.GetBootloaderValue(text);
		if (bootloaderValue.Equals(ErrorCode.OperationFail.ToString()))
		{
			return ErrorCode.OperationFail;
		}
		version.Append(bootloaderValue);
		PrintLog("res: " + version);
		return ErrorCode.NoError;
	}

	public string GetSkuID()
	{
		return GetSkuID(-1);
	}

	public string GetSkuID(int timeout)
	{
		try
		{
			StringBuilder stringBuilder = new StringBuilder(128);
			ErrorCode skuId = GetSkuId(stringBuilder, timeout);
			if (skuId == ErrorCode.NoError)
			{
				return stringBuilder.ToString();
			}
			LogUtility.E(staticTag, "GetSkuID fail: " + GetErrorMessage(skuId));
			return string.Empty;
		}
		catch (Exception ex)
		{
			LogUtility.E(staticTag, "CMDAPI GetSkuID fail: catch exception, " + ex.Message);
			return string.Empty;
		}
	}

	public ErrorCode GetSkuId(StringBuilder value)
	{
		return GetSkuId(value, -1);
	}

	public ErrorCode GetSkuId(StringBuilder value, int timeout)
	{
		if (!IsFastbootDeviceConnectedWithTimeout(timeout))
		{
			return ErrorCode.DeviceNotConnect;
		}
		if (device == null || string.IsNullOrEmpty(device.SN))
		{
			return ErrorCode.DeviceNotSet;
		}
		string command = CommandUtility.GetCommand("oem", "repair", "skuid", "get", "-s", device.SN);
		string text = ExecFastbootCommand(command);
		ErrorCode errorCode = HandleResponse(text);
		if (errorCode != 0)
		{
			return errorCode;
		}
		string oemValue = CommandUtility.GetOemValue(text);
		if (oemValue.Equals(ErrorCode.OperationFail.ToString()))
		{
			return ErrorCode.OperationFail;
		}
		value.Append(oemValue);
		PrintLog("res: " + value);
		return ErrorCode.NoError;
	}

	public ErrorCode GetVar(string property, StringBuilder product)
	{
		return GetVar(property, product, -1);
	}

	public ErrorCode GetVar(string property, StringBuilder product, int timeout)
	{
		if (!IsFastbootDeviceConnectedWithTimeout(timeout))
		{
			return ErrorCode.DeviceNotConnect;
		}
		if (device == null || string.IsNullOrEmpty(device.SN))
		{
			return ErrorCode.DeviceNotSet;
		}
		string command = CommandUtility.GetCommand("getvar", property, "-s", device.SN);
		string text = ExecFastbootCommand(command);
		ErrorCode errorCode = HandleResponse(text);
		if (errorCode != 0)
		{
			return errorCode;
		}
		string varValue = CommandUtility.GetVarValue(text);
		if (varValue.Equals(ErrorCode.OperationFail.ToString()))
		{
			return ErrorCode.OperationFail;
		}
		product.Append(varValue);
		PrintLog("res: " + product);
		return ErrorCode.NoError;
	}

	public string GetVarValue(string property)
	{
		return GetVarValue(property, -1);
	}

	public string GetVarValue(string property, int timeout)
	{
		try
		{
			StringBuilder stringBuilder = new StringBuilder(128);
			ErrorCode var = GetVar(property.ToLower(), stringBuilder, timeout);
			if (var == ErrorCode.NoError)
			{
				return stringBuilder.ToString();
			}
			LogUtility.E(staticTag, "GetVarValue fail: " + GetErrorMessage(var));
			return string.Empty;
		}
		catch (Exception ex)
		{
			LogUtility.E(staticTag, "CMDAPI GetVarValue fail: catch exception, " + ex.Message);
			return string.Empty;
		}
	}

	public ErrorCode GetWallpaper(StringBuilder value)
	{
		return GetWallpaper(value, -1);
	}

	public ErrorCode GetWallpaper(StringBuilder value, int timeout)
	{
		if (!IsFastbootDeviceConnectedWithTimeout(timeout))
		{
			return ErrorCode.DeviceNotConnect;
		}
		if (device == null || string.IsNullOrEmpty(device.SN))
		{
			return ErrorCode.DeviceNotSet;
		}
		string command = CommandUtility.GetCommand("oem", "repair", "wallpapered", "get", "-s", device.SN);
		string text = ExecFastbootCommand(command);
		ErrorCode errorCode = HandleResponse(text);
		if (errorCode != 0)
		{
			return errorCode;
		}
		string oemValue = CommandUtility.GetOemValue(text);
		if (oemValue.Equals(ErrorCode.OperationFail.ToString()))
		{
			return ErrorCode.OperationFail;
		}
		value.Append(oemValue);
		PrintLog("res: " + value);
		return ErrorCode.NoError;
	}

	public string GetWallpaperID()
	{
		return GetWallpaperID(-1);
	}

	public string GetWallpaperID(int timeout)
	{
		try
		{
			StringBuilder stringBuilder = new StringBuilder(128);
			ErrorCode wallpaper = GetWallpaper(stringBuilder, timeout);
			if (wallpaper == ErrorCode.NoError)
			{
				return stringBuilder.ToString();
			}
			LogUtility.E(staticTag, "GetWallpaperId fail: " + GetErrorMessage(wallpaper));
			return string.Empty;
		}
		catch (Exception ex)
		{
			LogUtility.E(staticTag, "909 CMDAPI GetWallpaperId fail: Catch exception, " + ex.Message);
			return string.Empty;
		}
	}

	public bool IsAdbDeviceConnected()
	{
		if (device == null || string.IsNullOrEmpty(device.SN))
		{
			return false;
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (GetAdbDevices(stringBuilder) != 0)
		{
			return false;
		}
		string[] array = stringBuilder.ToString().Split(',');
		foreach (string text in array)
		{
			if (text.Trim().Equals(device.SN))
			{
				return true;
			}
		}
		return false;
	}

	public bool IsAdbDeviceConnectedWIthTimeout()
	{
		return IsAdbDeviceConnectedWithTimeout(-1);
	}

	public bool IsAdbDeviceConnectedWithTimeout(int timeout)
	{
		int num = timeout;
		if (num == -1)
		{
			num = waitDeviceTimeout;
		}
		PrintLog($"Waiting device ready in {num} microseconds");
		while (num > 0)
		{
			if (IsAdbDeviceConnected())
			{
				Thread.Sleep(waitInterval);
				return true;
			}
			Thread.Sleep(waitInterval);
			num -= waitInterval;
		}
		return false;
	}

	public bool IsAdbDeviceInRequiredMode(AdbMode adbMode)
	{
		if (device == null || string.IsNullOrEmpty(device.SN))
		{
			return false;
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (GetAdbDevices(stringBuilder, isRequireRes: true) == ErrorCode.NoError)
		{
			if (stringBuilder.Length == 0)
			{
				return false;
			}
			string value = adbMode.ToString();
			string[] array = stringBuilder.ToString().Replace("\r\n", "\n").Split('\n');
			for (int i = 0; i < array.Length; i++)
			{
				if (i != 0 && !string.IsNullOrEmpty(array[i]) && array[i].Contains(device.SN) && array[i].Contains(value))
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool IsAdbDeviceInRequiredModeWithTimeout(AdbMode adbMode)
	{
		return IsAdbDeviceInRequiredModeWithTimeout(adbMode, -1);
	}

	public bool IsAdbDeviceInRequiredModeWithTimeout(AdbMode adbMode, int timeout)
	{
		int num = timeout;
		if (num == -1)
		{
			num = 20000;
		}
		PrintLog($"Waiting device ready in {adbMode.ToString()} mode in {num} microseconds");
		while (num > 0)
		{
			if (IsAdbDeviceInRequiredMode(adbMode))
			{
				Thread.Sleep(waitInterval);
				return true;
			}
			Thread.Sleep(waitInterval);
			num -= waitInterval;
		}
		return false;
	}

	public bool IsFastbootDeviceConnected()
	{
		if (device == null || string.IsNullOrEmpty(device.SN))
		{
			return false;
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (GetDevices(stringBuilder) != 0)
		{
			return false;
		}
		string[] array = stringBuilder.ToString().Split(',');
		foreach (string text in array)
		{
			if (text.Trim().Equals(device.SN))
			{
				return true;
			}
		}
		return false;
	}

	public bool IsFastbootDeviceConnectedWithTimeout()
	{
		return IsFastbootDeviceConnectedWithTimeout(-1);
	}

	public bool IsFastbootDeviceConnectedWithTimeout(int timeout)
	{
		int num = timeout;
		if (num == -1)
		{
			num = waitDeviceTimeout;
		}
		PrintLog($"Waiting device ready in {num} microseconds");
		while (num > 0)
		{
			if (IsFastbootDeviceConnected())
			{
				Thread.Sleep(waitInterval);
				return true;
			}
			Thread.Sleep(waitInterval);
			num -= waitInterval;
		}
		return false;
	}

	public ErrorCode OtaUpdate(string path, UpdateLog updateLog)
	{
		return OtaUpdate(path, updateLog, -1);
	}

	public ErrorCode OtaUpdate(string path, UpdateLog updateLog, int timeout)
	{
		if (!IsAdbDeviceInRequiredModeWithTimeout(AdbMode.sideload, timeout))
		{
			return ErrorCode.DeviceNotConnect;
		}
		if (device == null || string.IsNullOrEmpty(device.SN))
		{
			return ErrorCode.DeviceNotSet;
		}
		if (!File.Exists(path))
		{
			return ErrorCode.FileNotFound;
		}
		string command = CommandUtility.GetCommand("-s", device.SN, "sideload", path);
		string text = ExecAdbCommand(command, updateLog);
		ErrorCode errorCode = HandleResponse(text);
		if (errorCode != 0)
		{
			LogUtility.D("1098 CMDAPI ", "OTA failed for " + device.SN + Environment.NewLine + text.ToString());
			return errorCode;
		}
		return ErrorCode.NoError;
	}

	public ErrorCode Reboot()
	{
		return Reboot(-1);
	}

	public ErrorCode Reboot(int timeout)
	{
		if (!IsFastbootDeviceConnectedWithTimeout(timeout))
		{
			return ErrorCode.DeviceNotConnect;
		}
		if (device == null || string.IsNullOrEmpty(device.SN))
		{
			return ErrorCode.DeviceNotSet;
		}
		string command = CommandUtility.GetCommand("reboot", "-s", device.SN);
		ExecFastbootCommand(command);
		return ErrorCode.NoError;
	}

	public ErrorCode RebootAdb2Bootloader()
	{
		return RebootAdb2Bootloader(-1);
	}

	public ErrorCode RebootAdb2Bootloader(int timeout)
	{
		if (!IsAdbDeviceConnectedWithTimeout(timeout))
		{
			return ErrorCode.DeviceNotConnect;
		}
		if (device == null || string.IsNullOrEmpty(device.SN))
		{
			return ErrorCode.DeviceNotSet;
		}
		string command = CommandUtility.GetCommand("-s", device.SN, "reboot", "bootloader");
		ExecAdbCommand(command);
		return ErrorCode.NoError;
	}

	public ErrorCode RebootAdb2Sideload()
	{
		return RebootAdb2Sideload(isEraseUserdata: false, -1);
	}

	public ErrorCode RebootAdb2Sideload(bool isEraseUserdata)
	{
		return RebootAdb2Sideload(isEraseUserdata, -1);
	}

	public ErrorCode RebootAdb2Sideload(bool isEraseUserdata, int timeout)
	{
		if (!IsAdbDeviceConnectedWithTimeout(timeout))
		{
			return ErrorCode.DeviceNotConnect;
		}
		if (device == null || string.IsNullOrEmpty(device.SN))
		{
			return ErrorCode.DeviceNotSet;
		}
		string text = "sura_update";
		if (isEraseUserdata)
		{
			text += "_eraseuserdata";
		}
		string command = CommandUtility.GetCommand("-s", device.SN, "reboot", text);
		ExecAdbCommand(command);
		return ErrorCode.NoError;
	}

	public ErrorCode RebootBootloader()
	{
		return RebootBootloader(-1);
	}

	public ErrorCode RebootBootloader(int timeout)
	{
		if (!IsFastbootDeviceConnectedWithTimeout(timeout))
		{
			return ErrorCode.DeviceNotConnect;
		}
		if (device == null || string.IsNullOrEmpty(device.SN))
		{
			return ErrorCode.DeviceNotSet;
		}
		string command = CommandUtility.GetCommand("reboot-bootloader", "-s", device.SN);
		ExecFastbootCommand(command);
		return ErrorCode.NoError;
	}

	public ErrorCode RebootEdl()
	{
		return RebootEdl(-1);
	}

	public ErrorCode RebootEdl(int timeout)
	{
		if (!IsFastbootDeviceConnectedWithTimeout(timeout))
		{
			return ErrorCode.DeviceNotConnect;
		}
		if (device == null || string.IsNullOrEmpty(device.SN))
		{
			return ErrorCode.DeviceNotSet;
		}
		string command = CommandUtility.GetCommand("reboot-edl", "-s", device.SN);
		ExecFastbootCommand(command);
		return ErrorCode.NoError;
	}

	public ErrorCode LockBootloader()
	{
		return LockBootloader(-1);
	}

	public ErrorCode LockBootloader(int timeout)
	{
		if (!IsFastbootDeviceConnectedWithTimeout(timeout))
		{
			return ErrorCode.DeviceNotConnect;
		}
		if (device == null || string.IsNullOrEmpty(device.SN))
		{
			return ErrorCode.DeviceNotSet;
		}
		string command = CommandUtility.GetCommand("flashing lock");
		PrintLog("LockBootloader: " + command);
		string text = ExecFastbootCommand(command);
		ErrorCode errorCode = HandleResponse(text);
		if (errorCode != 0)
		{
			LogUtility.D("1240 CMDAPI ", "bt lock failed for " + device.SN + Environment.NewLine + text.ToString());
			return errorCode;
		}
		return ErrorCode.NoError;
	}

	public ErrorCode RebootSideload()
	{
		return RebootSideload(isEraseUserdata: false, -1);
	}

	public ErrorCode RebootSideload(bool isEraseUserdata)
	{
		return RebootSideload(isEraseUserdata, -1);
	}

	public ErrorCode RebootSideload(bool isEraseUserdata, int timeout)
	{
		if (!IsFastbootDeviceConnectedWithTimeout(timeout))
		{
			return ErrorCode.DeviceNotConnect;
		}
		if (device == null || string.IsNullOrEmpty(device.SN))
		{
			return ErrorCode.DeviceNotSet;
		}
		string text = "sura_update";
		if (isEraseUserdata)
		{
			text += "_eraseuserdata";
		}
		string command = CommandUtility.GetCommand("oem", text, "-s", device.SN);
		PrintLog("RebootSideload: " + command);
		string text2 = ExecFastbootCommand(command);
		ErrorCode errorCode = HandleResponse(text2);
		if (errorCode == ErrorCode.NoReturn)
		{
			PrintLog("res: no reture, assume success.");
			return ErrorCode.NoError;
		}
		bool flag = CommandUtility.IsOK(text2);
		PrintLog("res: " + flag);
		if (!flag)
		{
			return ErrorCode.OperationFail;
		}
		return ErrorCode.NoError;
	}

	public ErrorCode RequestPermission(HmdPermissionType pType, string response)
	{
		return RequestPermission(pType, response, -1);
	}

	public ErrorCode RequestPermission(HmdPermissionType pType, string response, int timeout)
	{
		if (!IsFastbootDeviceConnectedWithTimeout(timeout))
		{
			return ErrorCode.DeviceNotConnect;
		}
		if (device == null || string.IsNullOrEmpty(device.SN))
		{
			return ErrorCode.DeviceNotSet;
		}
		string command = CommandUtility.GetCommand("oem", "permission", pType.ToString().ToLower(), response, "-s", device.SN);
		string text = ExecFastbootCommand(command);
		ErrorCode errorCode = HandleResponse(text);
		if (errorCode != 0)
		{
			LogUtility.D("1311 CMDAPI ", pType.ToString() + " perReq for " + device.SN + Environment.NewLine + text.ToString());
			return errorCode;
		}
		bool flag = CommandUtility.IsOK(text);
		PrintLog("requestPermission: " + command);
		PrintLog("res: " + flag);
		if (!flag)
		{
			LogUtility.D("1319 CMDAPI ", "perReq");
			return ErrorCode.OperationFail;
		}
		return ErrorCode.NoError;
	}

	public ErrorCode SetActiveSlot(string slot)
	{
		return SetActiveSlot(slot, -1);
	}

	public ErrorCode SetActiveSlot(string slot, int timeout)
	{
		if (!IsFastbootDeviceConnectedWithTimeout(timeout))
		{
			return ErrorCode.DeviceNotConnect;
		}
		if (device == null || string.IsNullOrEmpty(device.SN))
		{
			return ErrorCode.DeviceNotSet;
		}
		string command = CommandUtility.GetCommand("set_active", slot, "-s", device.SN);
		string text = ExecFastbootCommand(command);
		ErrorCode errorCode = HandleResponse(text);
		if (errorCode != 0)
		{
			LogUtility.D("1345 CMDAPI ", "set_active failed for " + device.SN + Environment.NewLine + text.ToString());
			return errorCode;
		}
		bool flag = CommandUtility.IsOK(text);
		PrintLog("res: " + flag);
		if (!flag)
		{
			LogUtility.D("1353 CMDAPI", "operation failed");
			return ErrorCode.OperationFail;
		}
		return ErrorCode.NoError;
	}

	public ErrorCode GetCurrentSlot(StringBuilder res)
	{
		return GetCurrentSlot(res, -1);
	}

	public ErrorCode GetCurrentSlot(StringBuilder result, int timeout)
	{
		if (!IsFastbootDeviceConnectedWithTimeout(timeout))
		{
			return ErrorCode.DeviceNotConnect;
		}
		if (device == null || string.IsNullOrEmpty(device.SN))
		{
			return ErrorCode.DeviceNotSet;
		}
		string command = CommandUtility.GetCommand("getvar", "current-slot", "-s", device.SN);
		string text = ExecFastbootCommand(command);
		result.Append(text);
		ErrorCode errorCode = HandleResponse(text);
		if (errorCode != 0)
		{
			LogUtility.D("1392 CMDAPI ", "current-slot failed for " + device.SN + Environment.NewLine + text.ToString());
			return errorCode;
		}
		bool flag = CommandUtility.IsOK(text);
		PrintLog("res: " + flag);
		if (!flag)
		{
			LogUtility.D("1399 CMDAPI", "operation failed");
			return ErrorCode.OperationFail;
		}
		return ErrorCode.NoError;
	}

	public ErrorCode SetSkuId(string value)
	{
		return SetSkuId(value, -1);
	}

	public ErrorCode SetSkuId(string value, int timeout)
	{
		if (!IsFastbootDeviceConnectedWithTimeout(timeout))
		{
			return ErrorCode.DeviceNotConnect;
		}
		if (device == null || string.IsNullOrEmpty(device.SN))
		{
			return ErrorCode.DeviceNotSet;
		}
		string command = CommandUtility.GetCommand("oem", "repair", "skuid", "set", value, "-s", device.SN);
		string text = ExecFastbootCommand(command);
		ErrorCode errorCode = HandleResponse(text);
		if (errorCode != 0)
		{
			return errorCode;
		}
		bool flag = CommandUtility.IsOK(text);
		PrintLog("res: " + flag);
		if (!flag)
		{
			return ErrorCode.OperationFail;
		}
		return ErrorCode.NoError;
	}

	public ErrorCode SetWallpaper(string value)
	{
		return SetWallpaper(value, -1);
	}

	public ErrorCode SetWallpaper(string value, int timeout)
	{
		if (!IsFastbootDeviceConnectedWithTimeout(timeout))
		{
			return ErrorCode.DeviceNotConnect;
		}
		string command = CommandUtility.GetCommand("oem", "repair", "wallpapered", "set", value, "-s", device.SN);
		string text = ExecFastbootCommand(command);
		ErrorCode errorCode = HandleResponse(text);
		if (errorCode != 0)
		{
			return errorCode;
		}
		bool flag = CommandUtility.IsOK(text);
		PrintLog("res: " + flag);
		if (!flag)
		{
			return ErrorCode.OperationFail;
		}
		return ErrorCode.NoError;
	}

	public ErrorCode UpdateZip(string path, UpdateLog updateLog)
	{
		return UpdateZip(path, updateLog, -1);
	}

	public ErrorCode UpdateZip(string path, UpdateLog updateLog, int timeout)
	{
		if (!IsFastbootDeviceConnectedWithTimeout(timeout))
		{
			return ErrorCode.DeviceNotConnect;
		}
		if (device == null || string.IsNullOrEmpty(device.SN))
		{
			return ErrorCode.DeviceNotSet;
		}
		if (!File.Exists(path))
		{
			return ErrorCode.FileNotFound;
		}
		string command = CommandUtility.GetCommand("update ", path, " -s ", device.SN);
		string text = ExecFastbootCommand(command, updateLog);
		ErrorCode errorCode = HandleResponse(text);
		if (errorCode != 0)
		{
			LogUtility.D("1443 CMDAPI ", "update zip failed for " + device.SN + Environment.NewLine + text.ToString());
			return errorCode;
		}
		return ErrorCode.NoError;
	}

	public ErrorCode UpdateZipWipe(string path, UpdateLog updateLog)
	{
		return UpdateZipWipe(path, updateLog, -1);
	}

	public ErrorCode UpdateZipWipe(string path, UpdateLog updateLog, int timeout)
	{
		if (!IsFastbootDeviceConnectedWithTimeout(timeout))
		{
			return ErrorCode.DeviceNotConnect;
		}
		if (device == null || string.IsNullOrEmpty(device.SN))
		{
			return ErrorCode.DeviceNotSet;
		}
		if (!File.Exists(path))
		{
			return ErrorCode.FileNotFound;
		}
		string command = CommandUtility.GetCommand("-w", "update", path, "-s", device.SN);
		string text = ExecFastbootCommand(command, updateLog);
		ErrorCode errorCode = HandleResponse(text);
		if (errorCode != 0)
		{
			LogUtility.D("1474 CMDAPI ", "update zip wipe for " + device.SN + Environment.NewLine + text.ToString());
			return errorCode;
		}
		return ErrorCode.NoError;
	}

	public bool IsDeviceExist()
	{
		return device != null;
	}

	public void ExitProcess()
	{
		try
		{
			if (process != null && !process.HasExited)
			{
				process.Kill();
			}
		}
		catch (Exception ex)
		{
			LogUtility.E(staticTag, "CMDAPI ExitProcess skip: " + ex.Message + "\n" + ex.StackTrace);
		}
	}

	public static ErrorCode TestHandleResponse(string resMsg)
	{
		return HandleResponse(resMsg);
	}

	private static ErrorCode HandleResponse(string resMsg)
	{
		if (!string.IsNullOrEmpty(resMsg))
		{
			if (resMsg.Contains(CommandUtility.ReturnString.Failed) || resMsg.Contains(CommandUtility.ReturnString.Failed.ToLower()))
			{
				if (resMsg.Contains(CommandUtility.ReturnString.Permission))
				{
					return ErrorCode.PermissionDenied;
				}
				if (resMsg.Contains(CommandUtility.ReturnString.UnknownCommand))
				{
					return ErrorCode.CommandNotSupport;
				}
				if (resMsg.Contains(CommandUtility.ReturnString.Adb))
				{
					return ErrorCode.OtaFailWithAdbNoError;
				}
				return ErrorCode.FailFromFastboot;
			}
			if (resMsg.Contains(CommandUtility.ReturnString.Error))
			{
				if (resMsg.Contains(CommandUtility.ReturnString.UpdatePackage))
				{
					return ErrorCode.FlashAbort;
				}
				return ErrorCode.ErrorNotDefine;
			}
			if (resMsg.Contains(ErrorCode.ExitWithErrorCode.ToString()))
			{
				LogUtility.D(staticTag, "ExecCommand error with exit code = " + resMsg.Substring(resMsg.LastIndexOf('#') + 1));
				return ErrorCode.ExitWithErrorCode;
			}
			return ErrorCode.NoError;
		}
		return ErrorCode.NoReturn;
	}

	public static string GetErrorMessage(ErrorCode ec)
	{
		return ec switch
		{
			ErrorCode.ExeNotExistOrNotApprove => "executable fastboot is not exist", 
			ErrorCode.RequestPermissionFail => "request permission fail", 
			ErrorCode.OperationFail => "operation fail", 
			ErrorCode.DeviceNotConnect => "device is not connected", 
			ErrorCode.NoReturn => "no return data", 
			ErrorCode.FailFromFastboot => "fail from fastboot", 
			ErrorCode.PermissionDenied => "permission denied", 
			ErrorCode.FlashAbort => "flash abort", 
			ErrorCode.DeviceNotSet => "device is not set to fastboot or device sn is null", 
			ErrorCode.CommandNotSupport => "command is not support", 
			ErrorCode.ExitWithErrorCode => "adb or fastboot exit code is not 0", 
			ErrorCode.OtaFailWithAdbNoError => "Ota fail with following log, adb: failed to read command: No error", 
			ErrorCode.NoEnoughMemory => "No enough memory space", 
			ErrorCode.JsonFormatError => "Json format error", 
			ErrorCode.ErrorNotDefine => "error from fastboot", 
			ErrorCode.FileNotFound => "file not found", 
			ErrorCode.FailWithMaxRetry => $"fail with maximun retry times({defaultRetryTimes} times)", 
			_ => "error not defined", 
		};
	}

	private void PrintLog(string log)
	{
		if (isDebug || Program.GlobalDebugFlag)
		{
			LogUtility.D(tag, log);
		}
	}
}
