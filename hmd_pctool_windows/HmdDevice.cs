using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace hmd_pctool_windows;

public class HmdDevice : Device
{
	private enum EraseFrpResult
	{
		NOT_DONE,
		PASS,
		FAIL
	}

	public class CFlashDetails
	{
		public string cfBasePath;

		public ROMConfig cfRomConfig;
	}

	private static readonly int rebootFastbootWaitingTime = 60000;

	private static readonly int startPollingDeviceDelay = 1000;

	private static readonly int devicePollingPeriod = 500;

	private static readonly string versionPrefix = "";

	private HmdOemInterface hmdOemInterface;

	private ROMConfig romConfig;

	private SignatureValues sign;

	private AntiRollbackDetails antiRb;

	private int ArValueCount = -1;

	private string currentSku;

	private string targetSku;

	private string logFileName = "";

	private string permissionLog = "";

	private static Dictionary<string, bool> testcaseResults = new Dictionary<string, bool>();

	public static CFlashDetails cFlash = new CFlashDetails();

	public static bool IsCFlashEnabled = false;

	public static bool IsExtractedOnce = false;

	private const string OEM_WT = "hmdLibraryWt:continue";

	private const string OEM_LC = "hmdLibrary_lc:continue";

	private const string OEM_TN = "hmdlibrary_tn:continue";

	private const string OEM_HQ = "hmdLibrary_hq:reboot-edl";

	private const string OEM_SC = "HMDSimlock:enter_calibration";

	private const string OEM_MW = "HMDSimlockMw:enter_calibration";

	private const string OEM_V2 = "hmdlibraryV2:continue:2";

	private const string OEM_HQ_QC = "hmdLibrary_hq_qc:continue";

	private const string OEM_IRIS = "HmdLibrary_Iris:oem enter_calibration";

	private const string OEM_IRONMAN = "hmdLibrary_wt:oem reboot meta";

	private const string REQUEST_PERMISSION_FAIL = "request permission fail";

	private string dataCable;

	private bool isEraseUserdata = false;

	private bool isIgnorePermission = false;

	private bool isEraseFRP = false;

	private bool isBaseTests = false;

	private bool isAllTests = false;

	private EraseFrpResult eraseFrpResult;

	public string SKU => currentSku;

	public int DataCable
	{
		set
		{
			dataCable = $"Device {value}";
		}
	}

	public HmdDevice(string serialNo)
		: base(serialNo)
	{
		DetectOemInterface();
		LogUtility.E("OEM ", hmdOemInterface.ToString() + "SN-> " + base.SN);
	}

	protected override int DoRequestPermission(object sender, DoWorkEventArgs e, object argument)
	{
		CommandType commandType = (CommandType)argument;
		int result = -1;
		int result2 = 0;
		try
		{
			switch (commandType)
			{
			case CommandType.Flash:
				break;
			case CommandType.RebootEdl:
				if (RequestHmdPermission(HmdPermissionType.Flash) != 0)
				{
					return result;
				}
				if (RequestHmdPermission(HmdPermissionType.Simlock) != 0)
				{
					return result;
				}
				if (RequestHmdPermission(HmdPermissionType.Repair) != 0)
				{
					return result;
				}
				break;
			case CommandType.ReadItem:
			case CommandType.WriteItem:
				break;
			case CommandType.SimLock:
			case CommandType.SimUnlock:
				break;
			case CommandType.OtaUpdate:
				break;
			case CommandType.UnlockFrp:
				break;
			case CommandType.FactoryResets:
				break;
			case CommandType.StartSimlock:
				if (RequestHmdPermission(HmdPermissionType.Repair) != 0)
				{
					return result;
				}
				if (RequestHmdPermission(HmdPermissionType.Simlock) != 0)
				{
					return result;
				}
				break;
			case CommandType.StartPhoneEdit:
			case CommandType.GetSku:
				if (RequestHmdPermission(HmdPermissionType.Repair) != 0)
				{
					return result;
				}
				break;
			case CommandType.FrpErase:
			case CommandType.LockBootloader:
				if (RequestHmdPermission(HmdPermissionType.Flash) != 0)
				{
					return result;
				}
				if (RequestHmdPermission(HmdPermissionType.Repair) != 0)
				{
					return result;
				}
				break;
			case CommandType.BootToSystem:
				if (RequestHmdPermission(HmdPermissionType.Flash) != 0)
				{
					return result;
				}
				break;
			case CommandType.PhoneEdit:
			case CommandType.StopPhoneEditSimlock:
				break;
			}
		}
		catch (Exception ex)
		{
			LogUtility.D("Do Request Permission " + ex.Message, "\n" + ex.StackTrace);
			return result;
		}
		return result2;
	}

	protected override int DoGetAntiTheftStatus(object sender, DoWorkEventArgs e, object argument)
	{
		throw new NotImplementedException();
	}

	protected override int DoGetUnlockFrpResult(object sender, DoWorkEventArgs e, object argument)
	{
		throw new NotImplementedException();
	}

	public void SetIsIgnorePermissionDuringFlash(bool isIgnore)
	{
		isIgnorePermission = isIgnore;
	}

	public void SetIsEraseUserData(bool isErase)
	{
		isEraseUserdata = isErase;
	}

	public void SetIsEraseFRP(bool isEraseFRP)
	{
		this.isEraseFRP = isEraseFRP;
	}

	public void SetIsRunTests(bool isBaseTests, bool isAllTests)
	{
		this.isBaseTests = isBaseTests;
		this.isAllTests = isAllTests;
	}

	public void SetTargetSku(string targetSku)
	{
		this.targetSku = targetSku;
	}

	public void UpdateSWSKUID(string targetSku)
	{
		this.targetSku = targetSku;
		UpdateSkuID();
	}

	protected override int DoFlash(object sender, DoWorkEventArgs e, object argument)
	{
		eraseFrpResult = EraseFrpResult.NOT_DONE;
		LogUtility.E("/********", "Started for " + base.SN);
		logFileName = DateTime.Now.ToString("ddMMMMyyyy_HH.mm_") + base.SN + "_" + AzureNativeClient.Instance.UserName + "_flashlog.txt";
		long num = (long)(1000.0 * DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
		InvokeDeviceEventHandler(DeviceEventType.OnFlashStatus, 0, null);
		int num2 = 0;
		string zipPath = (string)argument;
		PrintLog($"Start flash, erase userdata = {isEraseUserdata}, ignore permission = {isIgnorePermission}, erase FRP = {isEraseFRP}");
		if (RequestHmdPermission(HmdPermissionType.Flash) != 0)
		{
			OnFlashError(e, "Flash permission fail: doesn't have permission for this model.");
			return -1;
		}
		InvokeUpdateLogEvent("Preparing files for flash...");
		if (!ReadRomConfig(zipPath) || romConfig == null)
		{
			OnFlashError(e, "Flash fail: Can not get romconfig or romconfig is null.\n\nThe error can arise due to incorrect or corrupted ROM package.\nPlease check whether the ROM package is for deviceKit.\nIf so, please redownload the ROM package and try again.");
			return -1;
		}
		num2++;
		UpdateProgressChanged(num2);
		romConfig.BasePath += $"-{num}";
		if (IsCFlashEnabled)
		{
			if (IsExtractedOnce)
			{
				romConfig = cFlash.cfRomConfig;
				romConfig.BasePath = cFlash.cfBasePath;
			}
			else
			{
				cFlash.cfRomConfig = romConfig;
				cFlash.cfBasePath = romConfig.BasePath;
			}
		}
		if (romConfig.isOTA)
		{
			if (isEraseFRP)
			{
				eraseFrpResult = EraseFrp();
				InvokeUpdateLogEvent("Erasing FPR..." + eraseFrpResult);
			}
			return DoOtaUpdate(sender, e, argument);
		}
		if (base.IsWorkerCancel)
		{
			OnFlashError(e, "Flash fail: User cancel.");
			return -1;
		}
		if (RequestHmdPermission(HmdPermissionType.Repair) != 0 && !isIgnorePermission)
		{
			LogUtility.D("DeviceFlash", "Request Repair permission fail.");
		}
		if (!CheckRequries())
		{
			OnFlashError(e, "Flash fail: Requirement check fail.");
			return -1;
		}
		num2++;
		UpdateProgressChanged(num2);
		if (base.IsWorkerCancel)
		{
			OnFlashError(e, "Flash fail: User cancel.");
			return -1;
		}
		JObject jObject = GetDeviceInfoJson(isFastboot: true);
		if (jObject != null)
		{
			jObject["Software"] = romConfig.RomVersion;
			antiRb = JsonConvert.DeserializeObject<AntiRollbackDetails>(jObject.ToString());
			ArValueCount = jObject.ToString().Select((char c, int i) => jObject.ToString().Substring(i)).Count((string sub) => sub.StartsWith("AntiRollback"));
		}
		num2++;
		UpdateProgressChanged(num2);
		if (base.IsWorkerCancel)
		{
			OnFlashError(e, "Flash fail: User cancel.");
			return -1;
		}
		if (!string.IsNullOrEmpty(currentSku) && !currentSku.Equals(targetSku))
		{
			InvokeUpdateLogEvent("  > Setting Sku id = " + targetSku);
			Thread.Sleep(500);
			if (UpdateSkuID() != 0)
			{
				OnFlashError(e, "UpdateSkuID fail.");
				return -1;
			}
		}
		num2 += 3;
		UpdateProgressChanged(num2);
		InvokeUpdateLogEvent("  > Queueing for unzip");
		StringBuilder key = new StringBuilder();
		sign = new SignatureValues();
		bool flag = false;
		bool isArVerified = false;
		if (ArValueCount > 0)
		{
			if (!CheckAntiRollback(antiRb, ArValueCount))
			{
				OnFlashError(e, "Flash fail: AntiRollBack verification failed.");
				return -1;
			}
			isArVerified = true;
		}
		if (CheckSigned(key))
		{
			if (!ReadSignature(zipPath))
			{
				OnFlashError(e, "Flash fail: Can't able to read signature from the ROM package.");
				return -1;
			}
			flag = true;
			if (!CheckVersion(jObject, isArVerified))
			{
				OnFlashError(e, "Flash fail: Selected software package is downgrading the version in the device.");
				return -1;
			}
		}
		if (IsCFlashEnabled)
		{
			if (!IsExtractedOnce)
			{
				DeviceManager.semaphoreUnzip.WaitOne();
				if (!UnzipRomZip(e, num2, 29))
				{
					DeviceManager.semaphoreUnzip.Release();
					return -1;
				}
				DeviceManager.semaphoreUnzip.Release();
				IsExtractedOnce = true;
			}
			else
			{
				InvokeUpdateLogEvent("  > Skipped File extration....");
			}
		}
		else
		{
			DeviceManager.semaphoreUnzip.WaitOne();
			if (!UnzipRomZip(e, num2, 29))
			{
				DeviceManager.semaphoreUnzip.Release();
				return -1;
			}
			DeviceManager.semaphoreUnzip.Release();
		}
		num2 += 30;
		UpdateProgressChanged(num2);
		InvokeUpdateLogEvent("timestamp:" + num);
		if (flag)
		{
			if (!VerifyZipSign(sign, romConfig.BasePath))
			{
				OnFlashError(e, "Package verification got failed.\n please make sure that the package is signed and verified.");
				return -1;
			}
			InvokeUpdateLogEvent("  > Signature Verification: Ok.");
		}
		if (base.IsWorkerCancel)
		{
			OnFlashError(e, "Flash fail: User cancel.");
			return -1;
		}
		if (romConfig.RequiredRebootPartitions != null && romConfig.RequiredRebootPartitions.Count > 0)
		{
			if (RequestHmdPermission(HmdPermissionType.Flash) != 0 && !isIgnorePermission)
			{
				OnFlashError(e, jObject, "Flash fail: Request Flash permission fail before Update Reboot Partitions.");
				return -1;
			}
			InvokeUpdateLogEvent("Updating reboot-partition");
			if (!UpdatePartitions(romConfig.RequiredRebootPartitions, num2, 1))
			{
				OnFlashError(e, jObject, "Flash fail: Update Reboot Partitions fail.");
				return -1;
			}
			InvokeUpdateLogEvent("  > Success");
			InvokeUpdateLogEvent("Rebooting device...");
			RestartBootloaderAndWait(rebootFastbootWaitingTime);
		}
		else
		{
			InvokeUpdateLogEvent("No reboot-partition required, skip.");
		}
		num2 += 2;
		UpdateProgressChanged(num2);
		if (!UpdateBootloader(e, jObject))
		{
			InvokeUpdateLogEvent("  > cant update bootloader");
			return -1;
		}
		num2 += 4;
		UpdateProgressChanged(num2);
		if (!UpdateRadio(e, jObject))
		{
			InvokeUpdateLogEvent("  > cant update radio");
			return -1;
		}
		num2 += 4;
		UpdateProgressChanged(num2);
		if (base.IsWorkerCancel)
		{
			OnFlashError(e, "Flash fail: User cancel.");
			return -1;
		}
		if (romConfig.PrePartitions != null && romConfig.PrePartitions.Count > 0)
		{
			if (RequestHmdPermission(HmdPermissionType.Flash) != 0 && !isIgnorePermission)
			{
				OnFlashError(e, jObject, "Flash fail: Request Flash permission fail before Update Pre-Partitions.");
				return -1;
			}
			InvokeUpdateLogEvent("Updating pre-partitions");
			if (!UpdatePartitions(romConfig.PrePartitions, num2, 9))
			{
				OnFlashError(e, jObject, "Flash fail: Update Pre-Partitions fail.");
				return -1;
			}
			InvokeUpdateLogEvent("  > Success");
			InvokeUpdateLogEvent("Rebooting device...");
			RestartBootloaderAndWait(rebootFastbootWaitingTime);
		}
		else
		{
			InvokeUpdateLogEvent("No pre-partition required, skip.");
		}
		num2 += 10;
		UpdateProgressChanged(num2);
		if (base.IsWorkerCancel)
		{
			OnFlashError(e, "Flash fail: User cancel.");
			return -1;
		}
		if (RequestHmdPermission(HmdPermissionType.Flash) != 0 && !isIgnorePermission)
		{
			OnFlashError(e, jObject, "Flash fail 56: Request Flash permission fail before UpdateImage.");
			return -1;
		}
		InvokeUpdateLogEvent("Updating images");
		if (!UpdateImage(num2, 29))
		{
			OnFlashError(e, jObject, "Flash fail: UpdateImage fail.");
			return -1;
		}
		InvokeUpdateLogEvent("  > Success");
		InvokeUpdateLogEvent("Rebooting device...");
		RestartBootloaderAndWait(rebootFastbootWaitingTime);
		num2 += 30;
		UpdateProgressChanged(num2);
		if (base.IsWorkerCancel)
		{
			OnFlashError(e, "Flash fail: User cancel.");
			return -1;
		}
		if (romConfig.PostPartitions != null && romConfig.PostPartitions.Count > 0)
		{
			if (RequestHmdPermission(HmdPermissionType.Flash) != 0 && !isIgnorePermission)
			{
				OnFlashError(e, jObject, "Flash fail: Request Flash permission fail before Update Post-Partitions.");
				return -1;
			}
			InvokeUpdateLogEvent("Updating post-partitions");
			if (!UpdatePartitions(romConfig.PostPartitions, num2, 9))
			{
				OnFlashError(e, jObject, "Flash fail: UpdatePartitions fail.");
				return -1;
			}
			InvokeUpdateLogEvent("  > Success");
			InvokeUpdateLogEvent("Rebooting device...");
			RestartBootloaderAndWait(rebootFastbootWaitingTime);
		}
		else
		{
			InvokeUpdateLogEvent("No post-partition required, skip.");
		}
		num2 += 10;
		UpdateProgressChanged(num2);
		GC.Collect();
		if (base.IsWorkerCancel)
		{
			OnFlashError(e, "Flash fail: User cancel.");
			return -1;
		}
		if (RequestHmdPermission(HmdPermissionType.Flash) != 0 && !isIgnorePermission)
		{
			OnFlashError(e, jObject, "Flash fail: Request Flash permission fail before Set Active slot to A.");
			return -1;
		}
		bool flag2 = IsCurrentSlotA();
		InvokeUpdateLogEvent("Is Current-Slot A:" + flag2);
		if (!flag2 && !SetActiveSlot())
		{
			OnFlashError(e, "Flash fail: SetActiveSlot fail.");
			return -1;
		}
		RestartBootloaderAndWait(rebootFastbootWaitingTime);
		num2 += 3;
		UpdateProgressChanged(num2);
		if (base.IsWorkerCancel)
		{
			OnFlashError(e, "Flash fail: User cancel.");
			return -1;
		}
		FinishUpdateZip();
		num2++;
		UpdateProgressChanged(num2);
		if (base.IsWorkerCancel)
		{
			OnFlashError(e, "Flash fail: User cancel.");
			return -1;
		}
		if ((isEraseUserdata || isEraseFRP) && RequestHmdPermission(HmdPermissionType.Flash) != 0 && !isIgnorePermission)
		{
			OnFlashError(e, jObject, "Flash fail: Request Flash permission fail before Erase FRP and userdata.");
			return -1;
		}
		bool flag3 = false;
		if (isAllTests || isEraseUserdata)
		{
			InvokeUpdateLogEvent("Erasing usedata.");
			flag3 = EraseUserData(e);
			if (isAllTests || isBaseTests)
			{
				testcaseResults.Add("TEST1:ERASE USERDATA-IMG", flag3);
			}
		}
		if (isAllTests || isEraseFRP)
		{
			eraseFrpResult = EraseFrp();
			InvokeUpdateLogEvent("Erasing FPR..." + eraseFrpResult);
			if (isAllTests || isBaseTests)
			{
				testcaseResults.Add("TEST2:ERASE FRP", eraseFrpResult == EraseFrpResult.PASS);
			}
		}
		RestartBootloaderAndWait(rebootFastbootWaitingTime);
		jObject = GetDeviceInfoJson(isFastboot: true);
		CreateFlashStatusLog(jObject, isFlashSuccess: true, flag3, string.Empty, eraseFrpResult);
		if (isBaseTests || isAllTests)
		{
			RunTests(isAllTests);
			PrintTestCases();
		}
		commandApi.Reboot();
		base.What = 3;
		long num3 = (long)(1000.0 * DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
		if (e != null)
		{
			e.Result = $"Flash success [{(num3 - num) / 1000}s].";
		}
		PrintLog("Flash success.");
		RemoveFlashTempFolder();
		Task.Run(async delegate
		{
			await AzureNativeClient.Instance.FlashLog(base.SN, romConfig.RomVersion, logFileName, Assembly.GetExecutingAssembly().GetName().Version.ToString(), "Flash success");
		});
		LogUtility.MoveLogFile();
		LogUtility.E("*********", "Success for " + base.SN);
		return 0;
	}

	private void RunTests(bool isAll)
	{
		testcaseResults.Add("TEST3:FLASH_UNLOCK", RequestHmdPermission(HmdPermissionType.Flash) == 0);
		CommandApi.ErrorCode errorCode;
		if (isAll)
		{
			errorCode = commandApi.EraseAll(null);
			testcaseResults.Add("TEST4:FACTORY RESET-CMD", errorCode == CommandApi.ErrorCode.NoError);
		}
		testcaseResults.Add("TEST5:REPAIR_UNLOCK", RequestHmdPermission(HmdPermissionType.Repair) == 0);
		StringBuilder stringBuilder = new StringBuilder(20);
		errorCode = commandApi.GetSkuId(stringBuilder);
		testcaseResults.Add("TEST6:GET_SKU", errorCode == CommandApi.ErrorCode.NoError);
		errorCode = commandApi.SetSkuId(stringBuilder.ToString());
		testcaseResults.Add("TEST7:SET_SKU", errorCode == CommandApi.ErrorCode.NoError);
		StringBuilder stringBuilder2 = new StringBuilder(20);
		errorCode = commandApi.GetWallpaper(stringBuilder2);
		testcaseResults.Add("TEST8:GET_WPID", errorCode == CommandApi.ErrorCode.NoError);
		errorCode = commandApi.SetWallpaper(stringBuilder2.ToString());
		testcaseResults.Add("TEST9:SET_WPID", errorCode == CommandApi.ErrorCode.NoError);
		if (isAll)
		{
			errorCode = commandApi.LockBootloader();
			testcaseResults.Add("TEST10:LOCK_BT", errorCode == CommandApi.ErrorCode.NoError);
		}
		testcaseResults.Add("TEST11:SIMLOCK_UNLOCK", RequestHmdPermission(HmdPermissionType.Simlock) == 0);
		testcaseResults.Add("TEST12:Flash", value: true);
	}

	private async void PrintTestCases()
	{
		string passed2 = "";
		string failed = "";
		string model = ((devInfo != null) ? devInfo.ProductModel : "Not detected");
		InvokeUpdateLogEvent("  >Test Results");
		foreach (KeyValuePair<string, bool> testCase in testcaseResults)
		{
			InvokeUpdateLogEvent("  >Test Results " + testCase.Key + ":" + testCase.Value);
			Console.WriteLine("  >Test Results " + testCase.Key + ":" + testCase.Value);
			if (testCase.Value)
			{
				passed2 = passed2 + "|" + testCase.Key;
			}
			else
			{
				failed = failed + "|" + testCase.Key;
			}
		}
		passed2 += "|TEST12:FLASH DEVICE";
		await AzureNativeClient.Instance.TestCaseLog(base.SN, model, passed2, failed);
	}

	private int OnFlashError(DoWorkEventArgs e, string errorMsg)
	{
		return OnFlashError(e, null, errorMsg);
	}

	private int OnFlashError(DoWorkEventArgs e, JObject jObject, string errorMsg)
	{
		base.What = 4;
		if (e != null)
		{
			e.Result = errorMsg;
		}
		LogUtility.E(Tag, "Update fail: " + errorMsg);
		RemoveFlashTempFolder();
		LogUtility.MoveLogFile();
		Task.Run(async delegate
		{
			await AzureNativeClient.Instance.FlashLog(base.SN, romConfig.RomVersion, logFileName, Assembly.GetExecutingAssembly().GetName().Version.ToString(), errorMsg + ":" + permissionLog);
		});
		CreateFlashStatusLog(jObject, isFlashSuccess: false, isEraseSuccess: false, errorMsg, eraseFrpResult);
		LogUtility.E("*********", "Failed for " + base.SN);
		return -1;
	}

	private int OnOtaError(DoWorkEventArgs e, string errorMsg)
	{
		return OnOtaError(e, null, errorMsg);
	}

	private int OnOtaError(DoWorkEventArgs e, JObject jObject, string errorMsg)
	{
		base.What = 4;
		e.Result = errorMsg;
		LogUtility.E(Tag, "Update fail: " + errorMsg);
		RemoveFlashTempFolder();
		LogUtility.MoveLogFile();
		CreateFlashStatusLog(jObject, isFlashSuccess: false, isEraseSuccess: false, errorMsg, eraseFrpResult);
		return -1;
	}

	private void RemoveFlashTempFolder()
	{
		try
		{
			if (!IsCFlashEnabled && romConfig != null && Directory.Exists(romConfig.BasePath))
			{
				InvokeUpdateLogEvent("Removing temp files");
				string text = (romConfig.BasePath.EndsWith("\\") ? romConfig.BasePath.Remove(romConfig.BasePath.Length - 1, 1) : romConfig.BasePath);
				text += ".tmp";
				Directory.Move(romConfig.BasePath, text);
				Directory.Delete(text, recursive: true);
			}
		}
		catch (Exception ex)
		{
			InvokeUpdateLogEvent("Removing temp files failed:" + ex.Message);
		}
	}

	protected override int DoOtaUpdate(object sender, DoWorkEventArgs e, object argument)
	{
		int progress = 0;
		InvokeDeviceEventHandler(DeviceEventType.OnFlashStatus, 0, "ota");
		long num = 0L;
		if (romConfig == null)
		{
			num = (long)(1000.0 * DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
			InvokeDeviceEventHandler(DeviceEventType.OnFlashStatus, 0, null);
			string zipPath = (string)argument;
			InvokeUpdateLogEvent("Preparing rom for ota update...");
			if (!ReadRomConfig(zipPath) || romConfig == null)
			{
				OnOtaError(e, "Ota update fail: Can not get romconfig or romconfig is null.");
				return -1;
			}
			romConfig.BasePath += $"-{num}";
		}
		progress++;
		UpdateProgressChanged(progress);
		if (num == 0)
		{
			try
			{
				num = long.Parse(romConfig.BasePath.Substring(romConfig.BasePath.LastIndexOf("-")));
			}
			catch (Exception)
			{
				num = (long)(1000.0 * DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
			}
		}
		if (base.IsWorkerCancel)
		{
			OnOtaError(e, "Flash fail: User cancel.");
			return -1;
		}
		JObject deviceInfoJson = GetDeviceInfoJson(isFastboot: true);
		if (deviceInfoJson != null)
		{
			deviceInfoJson["Software"] = romConfig.RomVersion;
		}
		progress++;
		UpdateProgressChanged(progress);
		InvokeUpdateLogEvent("  > Queueing for unzip");
		DeviceManager.semaphoreUnzip.WaitOne();
		if (!UnzipOtaZip(e, progress, 10))
		{
			DeviceManager.semaphoreUnzip.Release();
			return -1;
		}
		DeviceManager.semaphoreUnzip.Release();
		progress += 10;
		UpdateProgressChanged(progress);
		if (base.IsWorkerCancel)
		{
			OnOtaError(e, "Ota update fail: User cancel.");
			return -1;
		}
		InvokeUpdateLogEvent("Reboot device to sideload mode for ota update...");
		if (commandApi.IsFastbootDeviceConnected())
		{
			if (commandApi.RebootSideload(isEraseUserdata) != 0)
			{
				OnOtaError(e, deviceInfoJson, "Ota update fail: This device not support sura_update.");
				return -1;
			}
		}
		else
		{
			commandApi.RebootAdb2Sideload(isEraseUserdata);
		}
		progress++;
		UpdateProgressChanged(progress);
		if (!commandApi.IsAdbDeviceInRequiredModeWithTimeout(CommandApi.AdbMode.sideload))
		{
			OnOtaError(e, deviceInfoJson, "Ota update fail: Device not enter sideload mode.");
			return -1;
		}
		InvokeUpdateLogEvent("Device is ready for ota update...");
		progress += 3;
		UpdateProgressChanged(progress);
		if (base.IsWorkerCancel)
		{
			OnOtaError(e, "Ota update fail: User cancel.");
			return -1;
		}
		string text = Path.Combine(romConfig.BasePath, romConfig.RomPath);
		InvokeUpdateLogEvent("Updating OTA image from " + text);
		CommandApi.ErrorCode errorCode = commandApi.OtaUpdate(text, delegate(string log)
		{
			int otaFlashProgress = GetOtaFlashProgress(log);
			if (otaFlashProgress >= 0)
			{
				int progress2 = progress + (int)((double)otaFlashProgress * 85.0 / 94.0);
				UpdateProgressChanged(progress2);
			}
		});
		if (errorCode != 0)
		{
			OnOtaError(e, deviceInfoJson, "Ota update fail: updata ota zip fail, " + CommandApi.GetErrorMessage(errorCode));
			return -1;
		}
		UpdateProgressChanged(100);
		CreateFlashStatusLog(deviceInfoJson, isFlashSuccess: true, isEraseUserdata, string.Empty, eraseFrpResult);
		base.What = 3;
		long num2 = (long)(1000.0 * DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
		e.Result = $"Ota update success [{(num2 - num) / 1000}s].";
		PrintLog("Ota update success.");
		RemoveFlashTempFolder();
		return 0;
	}

	protected override int DoUnlockFrp(object sender, DoWorkEventArgs e, object argument)
	{
		int result = -1;
		int result2 = 0;
		CommandApi.ErrorCode errorCode = CommandApi.ErrorCode.NoError;
		InvokeDeviceEventHandler(DeviceEventType.OnUnlockFrpStatus, 0, 1);
		StringBuilder stringBuilder = new StringBuilder();
		try
		{
			if (devInfo == null || IsDevInfoObsolete())
			{
				errorCode = UpdateDevInfo();
				if (errorCode != 0)
				{
					throw new Exception("Fail to get AntiTheftStatus, ret = " + CommandApi.GetErrorMessage(errorCode));
				}
			}
			Enum.TryParse<AntiTheftStatus>(devInfo.AntiTheftStatus, out var result3);
			if (result3 == AntiTheftStatus.Disabled)
			{
				InvokeDeviceEventHandler(DeviceEventType.OnUnlockFrpStatus, 0, 3);
				return result2;
			}
			errorCode = commandApi.GetVar("token", stringBuilder);
			if (errorCode != 0)
			{
				throw new Exception("Fail to get token, ret = " + CommandApi.GetErrorMessage(errorCode));
			}
			StringBuilder version = new StringBuilder(32);
			errorCode = commandApi.GetSecurityVersion(version);
			if (errorCode != 0)
			{
				throw new Exception("Fail to get Security version, ret = " + CommandApi.GetErrorMessage(errorCode));
			}
			string signFilePath = GetSignFilePath(stringBuilder, version, result3);
			errorCode = commandApi.FlashZip("frp-unlock", signFilePath, null);
			if (errorCode != 0)
			{
				throw new Exception("Fail to flash frp-unlock, ret = " + CommandApi.GetErrorMessage(errorCode));
			}
			File.Delete(signFilePath);
			if (devInfo != null)
			{
				devInfo.AntiTheftStatus = AntiTheftStatus.Disabled.ToString();
			}
			InvokeDeviceEventHandler(DeviceEventType.OnUnlockFrpStatus, 0, 3);
			CreateUnlockLog(UnlockLogMode.Unlock, "Pass");
			return result2;
		}
		catch (Exception ex)
		{
			ReportFrpUnlockFail(ex.Message);
			if (ex.Message.Contains("anti-theft status is triggered"))
			{
				CreateUnlockLog(UnlockLogMode.Unlock, "Triggered");
			}
			else
			{
				CreateUnlockLog(UnlockLogMode.Unlock, "Fail");
			}
			return result;
		}
	}

	private string GetSignFilePath(StringBuilder token, StringBuilder version, AntiTheftStatus antiTheftStatus)
	{
		try
		{
			ServerResponse unlockSign = authenticationHandler.GetUnlockSign(token.ToString(), version.ToString(), antiTheftStatus);
			if (unlockSign.IsSuccessed)
			{
				string text = Path.GetTempPath() + "unlock_sign_" + base.SN + ".bin";
				File.WriteAllBytes(text, Convert.FromBase64String(unlockSign.ChiperResponse));
				return text;
			}
			throw new Exception("Fail to export the unlock signature file, reason: " + unlockSign.FailReason);
		}
		catch (Exception)
		{
			throw;
		}
	}

	private void ReportFrpUnlockFail(string failMsg)
	{
		LogUtility.D("FRP_UNLOCK", failMsg);
		InvokeDeviceEventHandler(DeviceEventType.OnUnlockFrpStatus, 0, 4);
		InvokeDeviceEventHandler(DeviceEventType.OnCommandFail, 0, failMsg.Remove(failMsg.IndexOf(',')));
	}

	protected override int DoFactoryReset(object sender, DoWorkEventArgs e, object argument)
	{
		deviceEventType = DeviceEventType.OnCommandSuccess;
		what = 10;
		int result = -1;
		int result2 = 0;
		if (RequestHmdPermission(HmdPermissionType.Flash) != 0)
		{
			deviceEventType = DeviceEventType.OnCommandFail;
			e.Result = "request permission fail";
			return result;
		}
		if (base.DoFactoryReset(sender, e, argument) == 0)
		{
			CommandApi.ErrorCode errorCode = commandApi.EraseAll(null);
			if (errorCode != 0)
			{
				deviceEventType = DeviceEventType.OnCommandFail;
				e.Result = CommandApi.GetErrorMessage(errorCode);
				return result;
			}
			return result2;
		}
		deviceEventType = DeviceEventType.OnCommandFail;
		e.Result = "request permission fail";
		return result;
	}

	protected override int DoStartPhoneEdit(object sender, DoWorkEventArgs e, object argument)
	{
		deviceEventType = DeviceEventType.OnCommandSuccess;
		what = 11;
		int result = -1;
		int result2 = 0;
		if (!IsInAdminMode())
		{
			deviceEventType = DeviceEventType.OnCommandFail;
			e.Result = "Please restart DeviceKit with administrator permissions.";
			return result;
		}
		if (base.DoStartPhoneEdit(sender, e, argument) == 0)
		{
			HmdOemProvider.RunScript("beforephoneedit.txt");
			EnterPhoneEditMode(sender, e, argument);
			return result2;
		}
		deviceEventType = DeviceEventType.OnCommandFail;
		e.Result = "request permission fail";
		return result;
	}

	protected override int DoStartSimLock(object sender, DoWorkEventArgs e, object argument)
	{
		deviceEventType = DeviceEventType.OnCommandSuccess;
		what = 12;
		int result = -1;
		int result2 = 0;
		if (!IsInAdminMode())
		{
			deviceEventType = DeviceEventType.OnCommandFail;
			e.Result = "Please restart DeviceKit with administrator permissions.";
			return result;
		}
		if (base.DoStartSimLock(sender, e, argument) == 0)
		{
			HmdOemProvider.RunScript("beforesimcontrol.txt");
			EnterPhoneEditMode(sender, e, argument);
			return result2;
		}
		deviceEventType = DeviceEventType.OnCommandFail;
		e.Result = "request permission fail";
		return result;
	}

	private void EnterPhoneEditMode(object sender, DoWorkEventArgs e, object argument)
	{
		hmdOemInterface.EnterPhoneEditMode(base.SN, delegate(bool bSuccess)
		{
			if (!bSuccess)
			{
				DoStopPhoneEditSimLock(sender, e, argument);
				deviceEventType = DeviceEventType.OnCommandFail;
				e.Result = "EnterPhoneEditMode fail";
			}
			else
			{
				hmdOemInterface.Init();
			}
		});
	}

	protected override int DoStopPhoneEditSimLock(object sender, DoWorkEventArgs e, object argument)
	{
		deviceEventType = DeviceEventType.OnCommandSuccess;
		what = 13;
		int num = -1;
		int num2 = 0;
		if (base.DoStopPhoneEditSimLock(sender, e, argument) == 0)
		{
			hmdOemInterface.StopEnterPhoneEditMode();
			int result = num2;
			if (!hmdOemInterface.Deinit())
			{
				deviceEventType = DeviceEventType.OnCommandFail;
				result = num;
			}
			return result;
		}
		deviceEventType = DeviceEventType.OnCommandFail;
		e.Result = "request permission fail";
		return num;
	}

	protected override int DoSimLock(object sender, DoWorkEventArgs e, object argument)
	{
		deviceEventType = DeviceEventType.OnCommandSuccess;
		what = 6;
		int result = 0;
		int result2 = -1;
		if (base.DoSimLock(sender, e, argument) == 0)
		{
			string text = (string)argument;
			StringBuilder stringBuilder = new StringBuilder(17);
			StringBuilder stringBuilder2 = new StringBuilder(17);
			try
			{
				if (!File.Exists(text))
				{
					deviceEventType = DeviceEventType.OnCommandFail;
					e.Result = text + " does not exist";
					return result2;
				}
				if (!hmdOemInterface.Simlock(text, stringBuilder, stringBuilder2))
				{
					deviceEventType = DeviceEventType.OnCommandFail;
					e.Result = "Execute Simlock fail.";
					return result2;
				}
				string result3 = ((stringBuilder.ToString() != string.Empty) ? ("PIN1:" + stringBuilder.ToString()) : "") + ((stringBuilder2.ToString() != string.Empty) ? ("\nPIN2:" + stringBuilder2.ToString()) : "");
				e.Result = result3;
			}
			catch (Exception ex)
			{
				deviceEventType = DeviceEventType.OnCommandFail;
				e.Result = ex.Message;
				LogUtility.D("904 HMDDevice ", ex.Message);
				return result2;
			}
			return result;
		}
		return result2;
	}

	protected override int DoSimUnlock(object sender, DoWorkEventArgs e, object argument)
	{
		deviceEventType = DeviceEventType.OnCommandSuccess;
		what = 7;
		int result = 0;
		int result2 = -1;
		if (base.DoSimUnlock(sender, e, argument) == 0)
		{
			UnLockKeys unLockKeys = (UnLockKeys)argument;
			if (!hmdOemInterface.SimUnlock(unLockKeys.key1, unLockKeys.key2))
			{
				Console.WriteLine("DoSimUnlock Fail");
				deviceEventType = DeviceEventType.OnCommandFail;
				e.Result = "Execute SimUnlock fail.";
				return result2;
			}
			Console.WriteLine("DoSimUnlock OK");
			return result;
		}
		return result2;
	}

	protected override int DoSaveUnlockFrpResult(object sender, DoWorkEventArgs e, object argument)
	{
		throw new NotImplementedException();
	}

	protected override int DoExportInspectLog(object sender, DoWorkEventArgs e, object argument)
	{
		return CreateUnlockLog(UnlockLogMode.Inspect);
	}

	protected override int DoRebootEdl(object sender, DoWorkEventArgs e, object argument)
	{
		what = 2;
		int result = -1;
		int result2 = 0;
		if (base.DoRebootEdl(sender, e, argument) == 0)
		{
			CommandApi.ErrorCode errorCode = commandApi.RebootEdl();
			if (errorCode == CommandApi.ErrorCode.NoError)
			{
				deviceEventType = DeviceEventType.OnCommandSuccess;
				return result2;
			}
			e.Result = CommandApi.GetErrorMessage(errorCode);
			deviceEventType = DeviceEventType.OnCommandFail;
			return result;
		}
		deviceEventType = DeviceEventType.OnCommandFail;
		e.Result = "request permission fail";
		return result;
	}

	protected override int DoLockBootloader(object sender, DoWorkEventArgs e, object argument)
	{
		what = 16;
		int result = -1;
		int result2 = 0;
		if (base.DoLockBootloader(sender, e, argument) == 0)
		{
			CommandApi.ErrorCode errorCode = commandApi.LockBootloader();
			if (errorCode == CommandApi.ErrorCode.NoError)
			{
				deviceEventType = DeviceEventType.OnCommandSuccess;
				MessageBox.Show("Device need to be reboot, Click OK to Proceed!!!");
				commandApi.Reboot();
				return result2;
			}
			e.Result = CommandApi.GetErrorMessage(errorCode);
			deviceEventType = DeviceEventType.OnCommandFail;
			return result;
		}
		deviceEventType = DeviceEventType.OnCommandFail;
		e.Result = "request permission fail";
		return result;
	}

	protected override int DoBootToSystem(object sender, DoWorkEventArgs e, object argument)
	{
		what = 17;
		int result = -1;
		int result2 = 0;
		if (base.DoBootToSystem(sender, e, argument) == 0)
		{
			CommandApi.ErrorCode errorCode = commandApi.ContinueBoot();
			if (errorCode == CommandApi.ErrorCode.NoError)
			{
				deviceEventType = DeviceEventType.OnCommandSuccess;
				return result2;
			}
			e.Result = CommandApi.GetErrorMessage(errorCode);
			deviceEventType = DeviceEventType.OnCommandFail;
			return result;
		}
		deviceEventType = DeviceEventType.OnCommandFail;
		e.Result = "request permission fail";
		return result;
	}

	protected override int DoReadItem(object sender, DoWorkEventArgs e, object argument)
	{
		deviceEventType = DeviceEventType.OnReadItem;
		DeviceItemType deviceItemType = (DeviceItemType)(what = (int)(DeviceItemType)argument);
		int num = -1;
		int num2 = 0;
		StringBuilder stringBuilder = new StringBuilder(20);
		int result = num2;
		if (base.DoReadItem(sender, e, argument) == 0)
		{
			switch (deviceItemType)
			{
			case DeviceItemType.Psn:
				if (!hmdOemInterface.ReadPsn(stringBuilder))
				{
					result = num;
					deviceEventType = DeviceEventType.OnCommandFail;
					what = 3;
				}
				break;
			case DeviceItemType.Imei:
				if (!hmdOemInterface.ReadImei(stringBuilder))
				{
					result = num;
					deviceEventType = DeviceEventType.OnCommandFail;
					what = 3;
				}
				break;
			case DeviceItemType.Imei2:
				if (!hmdOemInterface.ReadImei2(stringBuilder))
				{
					result = num;
					deviceEventType = DeviceEventType.OnCommandFail;
					what = 3;
				}
				break;
			case DeviceItemType.Meid:
				if (!hmdOemInterface.ReadMeid(stringBuilder))
				{
					result = num;
					deviceEventType = DeviceEventType.OnCommandFail;
					what = 3;
				}
				break;
			case DeviceItemType.WifiAddr:
				if (!hmdOemInterface.ReadWiFiAddr(stringBuilder))
				{
					result = num;
					deviceEventType = DeviceEventType.OnCommandFail;
					what = 3;
				}
				break;
			case DeviceItemType.BtAddress:
				if (!hmdOemInterface.ReadBTAddr(stringBuilder))
				{
					result = num;
					deviceEventType = DeviceEventType.OnCommandFail;
					what = 3;
				}
				break;
			case DeviceItemType.WallPaper:
			case DeviceItemType.SkuId:
			{
				if (RequestHmdPermission(HmdPermissionType.Repair) != 0)
				{
					deviceEventType = DeviceEventType.OnCommandFail;
					e.Result = "request permission fail";
					result = num;
				}
				CommandApi.ErrorCode errorCode = ReadItemByFastBoot(deviceItemType, stringBuilder);
				if (errorCode != 0)
				{
					result = num;
					deviceEventType = DeviceEventType.OnCommandFail;
					what = 3;
					stringBuilder.AppendLine(CommandApi.GetErrorMessage(errorCode));
				}
				break;
			}
			case DeviceItemType.ReadOnlyImei:
			case DeviceItemType.AntiTheftStatus:
			case DeviceItemType.Model:
			{
				CommandApi.ErrorCode errorCode = ReadItemByFastBoot(deviceItemType, stringBuilder);
				if (errorCode != 0)
				{
					result = num;
					deviceEventType = DeviceEventType.OnCommandFail;
					what = 3;
					stringBuilder.AppendLine(CommandApi.GetErrorMessage(errorCode));
				}
				break;
			}
			}
			e.Result = stringBuilder.ToString();
			return result;
		}
		deviceEventType = DeviceEventType.OnCommandFail;
		e.Result = "request permission fail";
		return num;
	}

	protected override int DoWriteItem(object sender, DoWorkEventArgs e, object argument)
	{
		deviceEventType = DeviceEventType.OnWriteItem;
		SetValueArgs setValueArgs = (SetValueArgs)argument;
		DeviceItemType type = setValueArgs.Type;
		string value = setValueArgs.Value;
		what = (int)type;
		StringBuilder stringBuilder = new StringBuilder(value);
		StringBuilder imei = new StringBuilder();
		StringBuilder signature = new StringBuilder();
		int num = 0;
		int num2 = -1;
		int result = num;
		if (base.DoWriteItem(sender, e, argument) == 0)
		{
			switch (type)
			{
			case DeviceItemType.Psn:
				if (!hmdOemInterface.WritePsn(stringBuilder))
				{
					result = num2;
					deviceEventType = DeviceEventType.OnCommandFail;
					what = 4;
				}
				break;
			case DeviceItemType.Imei:
				if (hmdOemInterface.ToString() == "hmd_pctool_windows.HmdOemLibV2")
				{
					try
					{
						seprateImeiSignature(value, out imei, out signature);
					}
					catch
					{
						MessageBox.Show("Please enter the value in correct format imei: signature(512 digit)");
					}
					if (!hmdOemInterface.WriteImei(imei, signature))
					{
						result = num2;
						deviceEventType = DeviceEventType.OnCommandFail;
						what = 4;
					}
				}
				else if (!hmdOemInterface.WriteImei(stringBuilder))
				{
					result = num2;
					deviceEventType = DeviceEventType.OnCommandFail;
					what = 4;
				}
				break;
			case DeviceItemType.Imei2:
				if (hmdOemInterface.ToString() == "hmd_pctool_windows.HmdOemLibV2")
				{
					try
					{
						seprateImeiSignature(value, out imei, out signature);
					}
					catch
					{
						MessageBox.Show("Please enter the value in correct format imei: signature(512 digit)");
					}
					if (!hmdOemInterface.WriteImei2(imei, signature))
					{
						result = num2;
						deviceEventType = DeviceEventType.OnCommandFail;
						what = 4;
					}
				}
				else if (!hmdOemInterface.WriteImei2(stringBuilder))
				{
					result = num2;
					deviceEventType = DeviceEventType.OnCommandFail;
					what = 4;
				}
				break;
			case DeviceItemType.Meid:
				if (!hmdOemInterface.WriteMeid(stringBuilder))
				{
					result = num2;
					deviceEventType = DeviceEventType.OnCommandFail;
					what = 4;
				}
				break;
			case DeviceItemType.WifiAddr:
				if (!hmdOemInterface.WriteWiFiAddr(stringBuilder))
				{
					result = num2;
					deviceEventType = DeviceEventType.OnCommandFail;
					what = 4;
				}
				break;
			case DeviceItemType.BtAddress:
				if (!hmdOemInterface.WriteBTAddr(stringBuilder))
				{
					result = num2;
					deviceEventType = DeviceEventType.OnCommandFail;
					what = 4;
				}
				break;
			case DeviceItemType.WallPaper:
			case DeviceItemType.SkuId:
			{
				LogUtility.E(Tag, "req per");
				if (RequestHmdPermission(HmdPermissionType.Repair) != 0)
				{
					deviceEventType = DeviceEventType.OnCommandFail;
					e.Result = "request permission fail";
					result = num2;
				}
				LogUtility.E(Tag, "write");
				CommandApi.ErrorCode errorCode = WriteItemByFastBoot(type, value);
				if (errorCode != 0)
				{
					deviceEventType = DeviceEventType.OnCommandFail;
					what = 4;
					e.Result = CommandApi.GetErrorMessage(errorCode);
					result = num2;
				}
				break;
			}
			}
			return result;
		}
		deviceEventType = DeviceEventType.OnCommandFail;
		e.Result = "request permission fail";
		return num2;
	}

	private void seprateImeiSignature(string value, out StringBuilder imei, out StringBuilder signature)
	{
		string[] array = value.Split(':');
		imei = new StringBuilder(array[0]);
		signature = new StringBuilder(array[1]);
	}

	public int FlashRomImage(string path)
	{
		int result = 0;
		DoFlash(null, null, path);
		return result;
	}

	public string GetAutomationToken(string text)
	{
		StringBuilder stringBuilder = new StringBuilder(32);
		CommandApi.ErrorCode var = commandApi.GetVar("serialno", stringBuilder);
		if (var != 0)
		{
			Console.WriteLine("Fail to get serailNo , ret = " + CommandApi.GetErrorMessage(var));
			return null;
		}
		Thread.Sleep(500);
		RSACryptoService rSACryptoService = new RSACryptoService("MIIEowIBAAKCAQEAxUTn9HoPCzsGcTfdfGqHq7p2HCLPg5CJMDfIV5niv/uXIxSAtxamH1eKTTFVXyyiorBVnSJxKO5ruV6DL2I7RI6tC8h3wEwCDxftz0zkwEQge2lHz64/S5urlNeM+bp/ywZi1C6c8oO/MIt0hQgEF9yGaeijTkWNhjVzJDyr/owV3uPsdLSRorlWnc8WziHlQpagmoXSXfQWkiNMMF6pWV9gvsAjVYjQwTGPENbU5jgsm6Rsz8U+YxWvNk8IyOOinMXD65Re/b1a+f5b4wIKCM7kZ5UK7topHob5BBzGGicotSpjoPPkEkCvIk9fjH2Ywa4figw6dZezrxeVyL0RPwIDAQABAoIBAGES6Xn63pBOOXtZXFqvKZguJ5Ts5GT/qSLbMHE7PsPukI8otbZjJNhjgaE+154AHwAj+d1bZ4gW21fa1H9qvXOdKjaULampPZIj2liapC6g18MjKb1fJ7KTJjoWYD87sUs9F0EGtyD4CAthdLNKIImFcXeIjWQlAeG7R6/bU1/svaeTnd2+U2MhDa9I7Kry1DVZj4Dv2BQ9PQjR7ddKrZI4RAG/dzhSxUGhT+5pomj3mDxZXRj16czQaWShkcnBUyS0R9SYjD4MPr08FUvIX229hN6VvqyQMItJ6KLh1Srpp5249fpRnF6Tc0saHijF/VpJV5T1JOml/SlX6U2bnCECgYEA874WJYQ3M7BJxEHLbma5iyCdxFcPGB3gCwxUD1ECx1279S1k7GvuyF2Bty/gisuPngtyRNWtmibZRVrdg9bn1F1RpKVKPxLUaSjas3gad9KKScMRSdmr5+Fj9lhbqSgMG1AmCcBvi/Ai/m7KCNNBqR9foWq69B5vAUUghI3W4nkCgYEAzzCG3RNaENNtMyH3h2oofWA9T5EWKW3WmEtg2M/8os4rvs4ua7OnvLCJtnI2+j5Ep964vbt8QeTOK0wv2l0CtF6rmrwqZdi2U2UQpm8lZf8bewBBy6NHlYdKMWM6rMq+HDY51vAQFD8C/ObJ/giIjXBfBUWm9VnRqI9sgzktY3cCgYBA/geju0yI4NHanfyjlIqW+Xx39QrWUGkEKSZk6yIFjQ3oQ1Fs5R7HmH9VHFQQTlUePEkc56khuIgowSDd3bj1XGi/sT9J8DhpTfZ68mSEXMR5BKWgfoUjEGt6LXdLdJ09zzJFWWWk98Qs+devYL1aXj4+qVnubAsHWKpiDfwlaQKBgAxxfagJYX9hM02+3H7lgUkGXqhIrmwOjLTY0hgzZZjhiP8Mov0U7R4H/D1Y3rRoyPbMCYxbljre4wL2sGkM7PyoMuY4JtO3EDwx9a4JPtXBXIUmnsz8IXB5j5snun5mLsTC/PZLtKuCnUtTEQ6QtKLJ/Or0I/LYUh8tffbjmDZBAoGBALWZFhyyJuRJgbnEJMCmqD2axjxkPu5oiICPnJb4kq4fEfDAmsaMKYZBG4FpQmEV7y+IX10TTWVXVMxNOAQH8gjlM0kham2JzMaW+P1WL1ICmMWfzpFBeU9A5pleuZXv2fJzaxeaK0I3ZTrLkbr/q9mSTTAzYMZlnjotHNwK66Ev", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAxUTn9HoPCzsGcTfdfGqHq7p2HCLPg5CJMDfIV5niv/uXIxSAtxamH1eKTTFVXyyiorBVnSJxKO5ruV6DL2I7RI6tC8h3wEwCDxftz0zkwEQge2lHz64/S5urlNeM+bp/ywZi1C6c8oO/MIt0hQgEF9yGaeijTkWNhjVzJDyr/owV3uPsdLSRorlWnc8WziHlQpagmoXSXfQWkiNMMF6pWV9gvsAjVYjQwTGPENbU5jgsm6Rsz8U+YxWvNk8IyOOinMXD65Re/b1a+f5b4wIKCM7kZ5UK7topHob5BBzGGicotSpjoPPkEkCvIk9fjH2Ywa4figw6dZezrxeVyL0RPwIDAQAB");
		ServerResponse chiperResponse = authenticationHandler.GetChiperResponse(HmdPermissionType.Flash, stringBuilder.ToString(), rSACryptoService.Encrypt(text), "2");
		if (chiperResponse.IsSuccessed)
		{
			return chiperResponse.ChiperResponse;
		}
		Console.WriteLine("Fail to get token: " + chiperResponse.FailReason);
		return null;
	}

	public int RequestHmdPermission(HmdPermissionType type)
	{
		int result = -1;
		int result2 = 0;
		StringBuilder stringBuilder = new StringBuilder(32);
		CommandApi.ErrorCode var = commandApi.GetVar("serialno", stringBuilder);
		if (var != 0)
		{
			LogUtility.E("Request permission:Fail to get serailNo , ret = ", CommandApi.GetErrorMessage(var));
			permissionLog = "sn fail";
			return result;
		}
		Thread.Sleep(500);
		StringBuilder stringBuilder2 = new StringBuilder(1024);
		var = commandApi.AuthStart(stringBuilder2);
		if (var != 0)
		{
			LogUtility.E("Request permission:Fail to auth start , ret = ", CommandApi.GetErrorMessage(var));
			permissionLog = "auth start fail";
			return result;
		}
		Thread.Sleep(500);
		StringBuilder stringBuilder3 = new StringBuilder(32);
		var = commandApi.GetSecurityVersion(stringBuilder3);
		if (var != 0)
		{
			LogUtility.E("Request permission:Fail to get Security version , ret = ", CommandApi.GetErrorMessage(var));
			permissionLog = "security version fail";
			return result;
		}
		Thread.Sleep(500);
		ServerResponse chiperResponse = authenticationHandler.GetChiperResponse(type, stringBuilder.ToString(), stringBuilder2.ToString(), stringBuilder3.ToString());
		if (!chiperResponse.IsSuccessed)
		{
			LogUtility.E("Request permission:serverResponse.FailReason ", chiperResponse.ChiperResponse + " " + chiperResponse.Message);
			permissionLog = "server fail " + $"{chiperResponse.Message} {type} {stringBuilder} {stringBuilder2} {stringBuilder3}";
			return result;
		}
		var = commandApi.RequestPermission(type, chiperResponse.ChiperResponse);
		if (var != 0)
		{
			LogUtility.E("Request permsiion:", CommandApi.GetErrorMessage(var));
			permissionLog = "device fail" + var;
			return result;
		}
		LogUtility.E("Request permission:" + type, " success");
		return result2;
	}

	private void DetectOemInterface()
	{
		StringBuilder stringBuilder = new StringBuilder(256);
		hmdOemInterface = new HmdOemWt();
		if (commandApi.GetDllName(stringBuilder) == CommandApi.ErrorCode.NoError)
		{
			if (stringBuilder.ToString().Equals("hmdLibrary_hq:reboot-edl", StringComparison.CurrentCultureIgnoreCase))
			{
				hmdOemInterface = new HmdOemHq();
			}
			if (stringBuilder.ToString().Equals("hmdlibrary_tn:continue", StringComparison.CurrentCultureIgnoreCase))
			{
				hmdOemInterface = new HmdOemTn();
			}
			if (stringBuilder.ToString().Equals("hmdLibrary_hq_qc:continue", StringComparison.CurrentCultureIgnoreCase))
			{
				hmdOemInterface = new HmdOemHqQc();
			}
			if (stringBuilder.ToString().Equals("HMDSimlock:enter_calibration", StringComparison.CurrentCultureIgnoreCase))
			{
				hmdOemInterface = new HmdOemSc();
			}
			if (stringBuilder.ToString().Equals("hmdLibrary_lc:continue", StringComparison.CurrentCultureIgnoreCase))
			{
				hmdOemInterface = new HmdOemLc();
			}
			if (stringBuilder.ToString().Equals("hmdLibraryWt:continue", StringComparison.CurrentCultureIgnoreCase))
			{
				hmdOemInterface = new HmdOemWt();
			}
			if (stringBuilder.ToString().Equals("HMDSimlockMw:enter_calibration", StringComparison.CurrentCultureIgnoreCase))
			{
				hmdOemInterface = new HmdOemMw();
			}
			if (stringBuilder.ToString().Equals("hmdlibraryV2:continue:2", StringComparison.CurrentCultureIgnoreCase))
			{
				hmdOemInterface = new HmdOemLibV2();
			}
			if (stringBuilder.ToString().Equals("HmdLibrary_Iris:oem enter_calibration", StringComparison.CurrentCultureIgnoreCase))
			{
				hmdOemInterface = new HmdOemIris();
			}
			if (stringBuilder.ToString().Equals("hmdLibrary_wt:oem reboot meta", StringComparison.CurrentCultureIgnoreCase))
			{
				hmdOemInterface = new HmdOemIronman();
			}
		}
		hmdOemInterface.SetCommandApi(commandApi);
	}

	private bool IsInAdminMode()
	{
		WindowsPrincipal windowsPrincipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
		if (!windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator))
		{
			return false;
		}
		return true;
	}

	private bool ReadRomConfig(string zipPath)
	{
		PrintLog("ReadRomConfig() from zip file = " + zipPath);
		ROMConfig rOMConfig = ZipUtility.ReadJsonInZip<ROMConfig>(zipPath, "rom.json", base.SN);
		if (rOMConfig == null)
		{
			return false;
		}
		rOMConfig.ZipPath = zipPath;
		rOMConfig.BasePath = Path.GetTempPath() + Path.GetFileNameWithoutExtension(zipPath);
		romConfig = rOMConfig;
		return true;
	}

	private bool CheckSigned(StringBuilder key)
	{
		CommandApi.ErrorCode signKey = commandApi.GetSignKey(key);
		if (signKey != 0)
		{
			LogUtility.E("Fail to get key , ret = ", CommandApi.GetErrorMessage(signKey));
			return false;
		}
		sign.signKey = key.ToString();
		return true;
	}

	private bool ReadSignature(string zipPath)
	{
		try
		{
			InvokeUpdateLogEvent("ReadRomConfig() from zip file...");
			List<string> list = ZipUtility.ReadSignature(zipPath, "cert", base.SN);
			if (list.Count() != 4)
			{
				return false;
			}
			sign.romSign = list[1];
			sign.packName = list[2].Replace(":", "");
			sign.packSign = list[3];
			InvokeUpdateLogEvent("  > reading signature is done");
			return true;
		}
		catch (Exception ex)
		{
			InvokeUpdateLogEvent("Readsign failed:" + ex.Message);
			return false;
		}
	}

	private bool VerifyZipSign(SignatureValues signature, string extractedPath)
	{
		try
		{
			RsaSignatureVerifier rsaSignatureVerifier = new RsaSignatureVerifier(signature.signKey);
			bool flag = rsaSignatureVerifier.VerifySign(extractedPath + "\\rom.json", signature.romSign);
			bool flag2 = rsaSignatureVerifier.VerifySign(extractedPath + "\\" + signature.packName, signature.packSign);
			return flag && flag2;
		}
		catch (Exception ex)
		{
			InvokeUpdateLogEvent("Verify Sign failed:" + ex.Message);
			return false;
		}
	}

	private bool CheckAntiRollback(AntiRollbackDetails anti, int count)
	{
		if (anti.AntiRollback > -1 && count == 1)
		{
			if (anti.AntiRollback <= romConfig.AntiRollback)
			{
				InvokeUpdateLogEvent("  > AntiRollBack check-> succeed.");
				return true;
			}
			InvokeUpdateLogEvent($"  > AntiRollBack check-> failed. Device:{anti.AntiRollback},Package:{romConfig.AntiRollback}");
			return false;
		}
		if (count == 3)
		{
			if (anti.AntiRollback_HW <= romConfig.AntiRollback_HW && anti.AntiRollback_vbmeta <= romConfig.AntiRollback_vbmeta && anti.AntiRollback_vbmeta_system <= romConfig.AntiRollback_vbmeta_system)
			{
				InvokeUpdateLogEvent("  > AntiRollBack check-> succeed.");
				return true;
			}
			InvokeUpdateLogEvent($"  > AntiRollBack check-> failed.Device:{anti.AntiRollback_HW},{anti.AntiRollback_vbmeta},{anti.AntiRollback_vbmeta_system},Package:{romConfig.AntiRollback_HW},{romConfig.AntiRollback_vbmeta},{romConfig.AntiRollback_vbmeta_system}");
			return false;
		}
		return false;
	}

	private bool CheckVersion(JObject jObject, bool isArVerified)
	{
		try
		{
			if (romConfig.RomVersion.Contains("WND") || jObject.GetValue("Product").ToString().Contains("WND"))
			{
				string text = ((romConfig.RomVersion.IndexOf("WND") != -1) ? romConfig.RomVersion.Substring(7, 10) : "Incorrect format of rom version");
				string text2 = (string?)jObject.GetValue("Version");
				if (text == "Incorrect format of rom version")
				{
					return false;
				}
				Version version = new Version(text);
				string text3 = ((text2.Length == 13) ? text2.Substring(3) : "Error invalid length.");
				if (text3 == "Error invalid length.")
				{
					return false;
				}
				Version version2 = new Version(text3);
				if (version >= version2)
				{
					InvokeUpdateLogEvent("  > Check version: Ok, Upgradble!!!");
					return true;
				}
				return false;
			}
			if (romConfig.RomVersion.Contains("aoki") || romConfig.RomVersion.Contains("dm5") || jObject.GetValue("ProductModel").ToString().Contains("aoki") || jObject.GetValue("ProductModel").ToString().Contains("deadmau5"))
			{
				if (isArVerified)
				{
					return true;
				}
				InvokeUpdateLogEvent("  > Anti-Rollback value is missing, Please use older version to upgrade");
				return false;
			}
			InvokeUpdateLogEvent("  > Check version: failed, reason: model not detectable!");
			return false;
		}
		catch (Exception ex)
		{
			InvokeUpdateLogEvent("  > Check version failed:" + ex.Message);
			return false;
		}
	}

	private bool CheckRequries()
	{
		PrintLog("CheckRequries()");
		foreach (KeyValuePair<string, string> require in romConfig.Requires)
		{
			if (!CheckRequire(require.Key, require.Value))
			{
				return false;
			}
		}
		return true;
	}

	private bool CheckRequire(string property, string expectedValue)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (commandApi.GetVar(property, stringBuilder) != 0)
		{
			return false;
		}
		if (stringBuilder.ToString().Equals(expectedValue))
		{
			return true;
		}
		return false;
	}

	private CommandApi.ErrorCode UpdateSkuID()
	{
		if (string.IsNullOrEmpty(currentSku))
		{
			InvokeUpdateLogEvent("Skip UpdateSkuID, No permission for get/set sku.");
			return CommandApi.ErrorCode.NoError;
		}
		PrintLog("UpdateSkuID()");
		if (string.IsNullOrEmpty(targetSku))
		{
			targetSku = currentSku;
		}
		return commandApi.SetSkuId(targetSku);
	}

	private bool UnzipRomZip(DoWorkEventArgs e, int progress, int progressRange)
	{
		if (base.IsWorkerCancel)
		{
			OnFlashError(e, "Flash fail: User cancel.");
			return false;
		}
		InvokeUpdateLogEvent("  > Extracting rom zip");
		PrintLog($"UnzipRomZip() from progress = {progress}");
		int baseProgress = progress;
		ZipUtility.ZipError zipError = ZipUtility.UnzipFile(this, romConfig.ZipPath, romConfig.BasePath, delegate(long now, long total)
		{
			int num3 = (int)((double)now / (double)total * (double)progressRange / 3.0) + baseProgress;
			if (num3 > progress)
			{
				progress = num3;
				UpdateProgressChanged(num3);
			}
		}, isCleanTarget: true);
		if (zipError != 0)
		{
			OnUnzipFail(e, zipError);
			return false;
		}
		if (base.IsWorkerCancel)
		{
			OnFlashError(e, "Flash fail: User cancel.");
			return false;
		}
		baseProgress += 10;
		ZipUtility.ZipError zipError2 = ZipUtility.UnzipFile(this, Path.Combine(romConfig.BasePath, romConfig.RomPath), romConfig.BasePath, delegate(long now, long total)
		{
			int num2 = (int)((double)now / (double)total * (double)progressRange / 3.0) + baseProgress;
			if (num2 > progress)
			{
				progress = num2;
				UpdateProgressChanged(num2);
			}
		}, isCleanTarget: false);
		if (zipError2 != 0)
		{
			OnUnzipFail(e, zipError2);
			return false;
		}
		if (base.IsWorkerCancel)
		{
			OnFlashError(e, "Flash fail: User cancel.");
			return false;
		}
		baseProgress += 10;
		string targetPath = Path.Combine(romConfig.BasePath, "SystemZip");
		ZipUtility.ZipError zipError3 = ZipUtility.UnzipFile(this, Path.Combine(romConfig.BasePath, romConfig.SystemZip), targetPath, delegate(long now, long total)
		{
			int num = (int)((double)now / (double)total * (double)progressRange / 3.0) + baseProgress;
			if (num > progress)
			{
				progress = num;
				UpdateProgressChanged(num);
			}
		}, isCleanTarget: false);
		if (zipError3 != 0)
		{
			OnUnzipFail(e, zipError3);
			return false;
		}
		InvokeUpdateLogEvent("  > Success");
		return true;
	}

	private void OnUnzipFail(DoWorkEventArgs e, ZipUtility.ZipError ze, bool isOta = false)
	{
		string empty = string.Empty;
		empty = ((ze != ZipUtility.ZipError.ErrorWithOutDefine) ? " with " : ".\n");
		empty += ZipUtility.GetZipErrorMessage(ze);
		InvokeUpdateLogEvent("  > Fail" + empty);
		string errorMsg = "Flash fail: Extract zip fail.\n\n" + empty;
		if (isOta)
		{
			errorMsg = "Ota update fail: Extract zip fail.\n\n" + empty;
		}
		OnFlashError(e, null, errorMsg);
	}

	private void UpdateLogFromFlash(string log)
	{
		InvokeUpdateLogEvent("  > " + log);
	}

	private CommandApi.ErrorCode FlashTargetZip(string partition, string zipFile)
	{
		CommandApi.ErrorCode result = commandApi.FlashZip(partition, zipFile, UpdateLogFromFlash, romConfig.SparseSize);
		Thread.Sleep(1000);
		return result;
	}

	private bool UpdateBootloader(DoWorkEventArgs e, JObject jObject)
	{
		PrintLog("UpdateBootloader()");
		string text = null;
		string text2 = null;
		if (romConfig.Bootloader != null)
		{
			text = Path.Combine(romConfig.BasePath, romConfig.Bootloader);
			text2 = "bootloader";
		}
		else
		{
			if (romConfig.Aboot == null)
			{
				InvokeUpdateLogEvent("No bootloader update required, skip.");
				return true;
			}
			text = Path.Combine(romConfig.BasePath, romConfig.Aboot);
			text2 = "aboot";
		}
		if (base.IsWorkerCancel)
		{
			OnFlashError(e, "Flash fail: User cancel.");
			return false;
		}
		if (RequestHmdPermission(HmdPermissionType.Flash) != 0 && !isIgnorePermission)
		{
			OnFlashError(e, jObject, "Flash fail: Request Flash permission fail before UpdateBootloader.");
			return false;
		}
		InvokeUpdateLogEvent("Updating bootloader");
		InvokeUpdateLogEvent("  > Flashing " + text2 + "...");
		CommandApi.ErrorCode errorCode = FlashTargetZip(text2, text);
		if (errorCode == CommandApi.ErrorCode.NoError)
		{
			InvokeUpdateLogEvent("  > Success");
			InvokeUpdateLogEvent("Rebooting device...");
			RestartBootloaderAndWait(rebootFastbootWaitingTime);
			return true;
		}
		InvokeUpdateLogEvent("  x Flashing " + text2 + " fail, " + CommandApi.GetErrorMessage(errorCode) + ".");
		OnFlashError(e, jObject, "Flash fail: Update bootloader fail.");
		return false;
	}

	private bool RestartBootloaderAndWait(int timeout)
	{
		PrintLog($"RestartBootloaderAndWait() timeout = {timeout}");
		commandApi.RebootBootloader();
		Thread.Sleep(startPollingDeviceDelay);
		while (!commandApi.IsFastbootDeviceConnected())
		{
			if ((timeout -= devicePollingPeriod) > 0)
			{
				Thread.Sleep(devicePollingPeriod);
				continue;
			}
			return false;
		}
		return true;
	}

	private bool UpdateRadio(DoWorkEventArgs e, JObject jObject)
	{
		PrintLog("UpdateRadio()");
		string text = null;
		string text2 = null;
		if (romConfig.Radio != null)
		{
			text = Path.Combine(romConfig.BasePath, romConfig.Radio);
			text2 = "radio";
		}
		else
		{
			if (romConfig.Modem == null)
			{
				InvokeUpdateLogEvent("No radio update required, skip.");
				return true;
			}
			text = Path.Combine(romConfig.BasePath, romConfig.Modem);
			text2 = "modem";
		}
		if (base.IsWorkerCancel)
		{
			OnFlashError(e, "Flash fail: User cancel.");
			return false;
		}
		if (RequestHmdPermission(HmdPermissionType.Flash) != 0 && !isIgnorePermission)
		{
			OnFlashError(e, jObject, "Flash fail: Request Flash permission fail before UpdateRadio.");
			return false;
		}
		InvokeUpdateLogEvent("Updating radio");
		InvokeUpdateLogEvent("  > Flashing " + text2 + "...");
		CommandApi.ErrorCode errorCode = FlashTargetZip(text2, text);
		if (errorCode == CommandApi.ErrorCode.NoError)
		{
			InvokeUpdateLogEvent("  > Success");
			InvokeUpdateLogEvent("Rebooting device...");
			RestartBootloaderAndWait(rebootFastbootWaitingTime);
			return true;
		}
		InvokeUpdateLogEvent("  x Flashing " + text2 + " fail, " + CommandApi.GetErrorMessage(errorCode) + ".");
		OnFlashError(e, jObject, "Flash fail: Update radio fail.");
		return false;
	}

	private bool UpdatePartitions(Dictionary<string, string> partitions, int startProgress, int progressRange, bool isRebootBootloader = false)
	{
		if (partitions != null)
		{
			int count = partitions.Count;
			double num = 0.0;
			int num2 = startProgress;
			foreach (KeyValuePair<string, string> partition in partitions)
			{
				if (!FlashImage(partition.Key, Path.Combine(romConfig.BasePath, partition.Value)))
				{
					return false;
				}
				num += 1.0;
				int num3 = (int)(num / (double)count / (double)progressRange * 100.0);
				if (num3 + startProgress > num2)
				{
					num2 = num3 + startProgress;
					UpdateProgressChanged(num2);
				}
				if (isRebootBootloader)
				{
					RestartBootloaderAndWait(rebootFastbootWaitingTime);
				}
				else
				{
					Thread.Sleep(500);
				}
			}
			return true;
		}
		InvokeUpdateLogEvent("UpdatePartitions fail: partitions is null.");
		return false;
	}

	private bool FlashImage(string partition, string imagePath)
	{
		PrintLog("FlashImage(): partition = " + partition + ", imagePath = " + imagePath);
		CommandApi.ErrorCode errorCode;
		if (CheckHasSlot(partition))
		{
			InvokeUpdateLogEvent("  > Flashing slot a...");
			errorCode = FlashTargetZip(partition + "_a", imagePath);
			if (errorCode != 0)
			{
				InvokeUpdateLogEvent("  x Flashing slot a Fail, " + CommandApi.GetErrorMessage(errorCode) + ".");
				return false;
			}
			InvokeUpdateLogEvent("  > Flashing slot b...");
			errorCode = FlashTargetZip(partition + "_b", imagePath);
			if (errorCode != 0)
			{
				InvokeUpdateLogEvent("  x Flashing slot b Fail, " + CommandApi.GetErrorMessage(errorCode) + ".");
				return false;
			}
			return true;
		}
		InvokeUpdateLogEvent("  > Flashing " + partition + "...");
		errorCode = FlashTargetZip(partition, imagePath);
		if (errorCode != 0)
		{
			InvokeUpdateLogEvent("  x Flashing " + partition + " Fail, " + CommandApi.GetErrorMessage(errorCode) + ".");
			return false;
		}
		return true;
	}

	private bool CheckHasSlot(string img)
	{
		PrintLog("CheckHasSlot(): img = " + img);
		StringBuilder stringBuilder = new StringBuilder(32);
		if (commandApi.GetVar("has-slot:" + img, stringBuilder) != 0)
		{
			InvokeUpdateLogEvent("Fail to get has-slot:" + img);
			return false;
		}
		if (stringBuilder.ToString().EndsWith("Yes", StringComparison.CurrentCultureIgnoreCase))
		{
			InvokeUpdateLogEvent("has-slot: " + stringBuilder.ToString());
			return true;
		}
		return false;
	}

	public bool UpdateImage(int progress, int progressRange)
	{
		int baseProgress = progress;
		string text = Path.Combine(romConfig.BasePath, "SystemZip");
		PrintLog("UpdateImage path = " + text);
		long num = 0L;
		long flashingSizeRef = 0L;
		string[] files = Directory.GetFiles(text);
		foreach (string text2 in files)
		{
			if (text2.EndsWith(".img") && !text2.EndsWith("userdata.img") && !text2.EndsWith("cache.img"))
			{
				string fileName = Path.Combine(text, text2);
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(text2);
				bool flag = CheckHasSlot(fileNameWithoutExtension);
				num += new FileInfo(fileName).Length / 1024;
				if (flag)
				{
					num += new FileInfo(fileName).Length / 1024;
				}
			}
		}
		string[] files2 = Directory.GetFiles(text);
		foreach (string text3 in files2)
		{
			if (!text3.EndsWith(".img") || text3.EndsWith("userdata.img") || text3.EndsWith("cache.img"))
			{
				continue;
			}
			string text4 = Path.Combine(text, text3);
			PrintLog("Process file " + text4);
			string fileNameWithoutExtension2 = Path.GetFileNameWithoutExtension(text3);
			if (CheckHasSlot(fileNameWithoutExtension2))
			{
				InvokeUpdateLogEvent("  > Flashing slot a...");
				if (!FlashSystemZipImage(fileNameWithoutExtension2 + "_a", text4, ref flashingSizeRef, num, ref progress, progressRange, baseProgress))
				{
					InvokeUpdateLogEvent(" Failed: Flashing slot a partition = " + fileNameWithoutExtension2 + "_a");
					return false;
				}
				InvokeUpdateLogEvent("  > Flashing slot b...");
				if (!FlashSystemZipImage(fileNameWithoutExtension2 + "_b", text4, ref flashingSizeRef, num, ref progress, progressRange, baseProgress))
				{
					InvokeUpdateLogEvent(" Failed: Flashing slot a partition = " + fileNameWithoutExtension2 + "_a");
					return false;
				}
			}
			else
			{
				InvokeUpdateLogEvent("  > Flash zip partition = " + fileNameWithoutExtension2);
				if (!FlashSystemZipImage(fileNameWithoutExtension2, text4, ref flashingSizeRef, num, ref progress, progressRange, baseProgress))
				{
					InvokeUpdateLogEvent("  > Failed: Flash zip partition = " + fileNameWithoutExtension2);
					return false;
				}
			}
		}
		return true;
	}

	private bool FlashSystemZipImage(string partition, string path, ref long flashingSizeRef, long totalFlashSize, ref int progressRef, int progressRange, int baseProgress)
	{
		bool isWaitOk = false;
		long flashingSize = flashingSizeRef;
		int progress = progressRef;
		CommandApi.ErrorCode errorCode = commandApi.FlashZip(partition, path, delegate(string log)
		{
			if (string.IsNullOrEmpty(log))
			{
				LogUtility.D("1869 HMDdevice Failed: Flash Zip fastboot cmd null returned", "");
			}
			else
			{
				InvokeUpdateLogEvent("  > " + log);
				if (log.Contains("FAILED"))
				{
					LogUtility.D("1875 HMDdevice Failed: Flash zip fastboot cmd", "");
				}
				else if (isWaitOk && log.Contains("OKAY"))
				{
					isWaitOk = false;
					int num = (int)((double)flashingSize / (double)totalFlashSize * (double)progressRange) + baseProgress;
					if (num > progress)
					{
						progress = num;
						UpdateProgressChanged(num);
					}
				}
				else
				{
					long sendingSize = GetSendingSize(log);
					if (sendingSize > 0)
					{
						isWaitOk = true;
						flashingSize += sendingSize;
					}
				}
			}
		}, romConfig.SparseSize);
		if (errorCode != 0)
		{
			InvokeUpdateLogEvent("  x Flashing " + partition + " fail, " + CommandApi.GetErrorMessage(errorCode) + " Path:" + path);
			LogUtility.E("1902: HMDdevice failed fastboot flash zip returned error", "");
			return false;
		}
		flashingSizeRef = flashingSize;
		progressRef = progress;
		Thread.Sleep(1000);
		return true;
	}

	private long GetSendingSize(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return -1L;
		}
		long num = -1L;
		string pattern = "Sending (.* )?'.*' (.*\\/.* )?\\((?<size>.*) KB\\)";
		Match match = Regex.Match(input, pattern);
		try
		{
			string s = match.Groups["size"].Value.Replace(",", "");
			num = long.Parse(s);
		}
		catch (Exception)
		{
			num = -1L;
		}
		return num;
	}

	private bool IsCurrentSlotA()
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (commandApi.GetCurrentSlot(stringBuilder) == CommandApi.ErrorCode.NoError)
		{
			InvokeUpdateLogEvent("Current Slot:" + stringBuilder.ToString());
		}
		if (stringBuilder.ToString().Contains("current-slot: a") || stringBuilder.ToString().Contains("current-slot:a"))
		{
			return true;
		}
		return false;
	}

	private bool SetActiveSlot()
	{
		if (commandApi.SetActiveSlot("a") == CommandApi.ErrorCode.NoError)
		{
			PrintLog("SetActiveSlot");
			InvokeUpdateLogEvent("---------------------------------------------");
			InvokeUpdateLogEvent("Success: Set active slot to a...");
		}
		return true;
	}

	private void FinishUpdateZip()
	{
		PrintLog("FinishUpdateZip");
		if (!Program.isFactoryVersion())
		{
			RestartBootloaderAndWait(rebootFastbootWaitingTime);
			return;
		}
		string path = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "finishflash.txt");
		if (!File.Exists(path))
		{
			InvokeUpdateLogEvent("No custom script to execute, reboot now.");
			RestartBootloaderAndWait(rebootFastbootWaitingTime);
			return;
		}
		if (RequestHmdPermission(HmdPermissionType.Repair) != 0 && !isIgnorePermission)
		{
			InvokeUpdateLogEvent("Unable to run custom script without repair permission, reboot now.");
			RestartBootloaderAndWait(rebootFastbootWaitingTime);
			return;
		}
		TextReader textReader = File.OpenText(path);
		using (Process process = new Process())
		{
			string text;
			while ((text = textReader.ReadLine()) != null)
			{
				if (!text.StartsWith("#"))
				{
					process.StartInfo.FileName = CommandUtility.DefaultFastboot;
					process.StartInfo.Arguments = text;
					process.StartInfo.UseShellExecute = false;
					process.StartInfo.RedirectStandardOutput = true;
					process.StartInfo.CreateNoWindow = true;
					process.StartInfo.RedirectStandardError = true;
					InvokeUpdateLogEvent("Execute \"fastboot " + text + "\"");
					process.Start();
					string log = process.StandardError.ReadToEnd();
					InvokeUpdateLogEvent(log);
					if (process.ExitCode != 0)
					{
						return;
					}
					Thread.Sleep(500);
				}
			}
		}
		RestartBootloaderAndWait(rebootFastbootWaitingTime);
	}

	private bool EraseUserData(DoWorkEventArgs e)
	{
		RequestHmdPermission(HmdPermissionType.Flash);
		bool result = false;
		string[] files = Directory.GetFiles(romConfig.BasePath);
		foreach (string text in files)
		{
			if ((text.EndsWith("userdata.img") || text.EndsWith("cache.img")) && FlashTargetZip(Path.GetFileNameWithoutExtension(text), text) == CommandApi.ErrorCode.NoError)
			{
				result = true;
			}
		}
		string[] files2 = Directory.GetFiles(Path.Combine(romConfig.BasePath, "SystemZip"));
		foreach (string text2 in files2)
		{
			if ((text2.EndsWith("userdata.img") || text2.EndsWith("cache.img")) && FlashTargetZip(Path.GetFileNameWithoutExtension(text2), text2) == CommandApi.ErrorCode.NoError)
			{
				result = true;
			}
		}
		if (commandApi.EraseAll(UpdateLogFromFlash) == CommandApi.ErrorCode.NoError)
		{
			return true;
		}
		return result;
	}

	[Obsolete]
	private bool UpdateUserDataAndCache()
	{
		CommandApi.ErrorCode errorCode = commandApi.EraseAll(UpdateLogFromFlash);
		string text = Path.Combine(romConfig.BasePath, "userdata.img");
		if (File.Exists(text) && FlashTargetZip("userdata", text) != 0)
		{
			return false;
		}
		string text2 = Path.Combine(romConfig.BasePath, "cache.img");
		if (File.Exists(text2) && FlashTargetZip("cache", text2) != 0)
		{
			return false;
		}
		return true;
	}

	private JObject GetDeviceInfoJson(bool isFastboot)
	{
		StringBuilder stringBuilder = new StringBuilder();
		CommandApi.ErrorCode errorCode = CommandApi.ErrorCode.NoError;
		if (((!isFastboot) ? commandApi.GetAdbDeviceInfo(stringBuilder) : commandApi.GetDeviceInfo(stringBuilder)) != 0)
		{
			return null;
		}
		JObject result = null;
		try
		{
			result = JObject.Parse(stringBuilder.ToString());
			return result;
		}
		catch (Exception ex)
		{
			LogUtility.E(Tag, "Can not parse device info json with exception: " + ex.Message + "\n" + ex.StackTrace + "\noriginal json string = " + stringBuilder.ToString());
		}
		return result;
	}

	private void CreateFlashStatusLog(JObject jObject, bool isFlashSuccess, bool isEraseSuccess, string errorMsg, EraseFrpResult eraseFrpResult = EraseFrpResult.NOT_DONE)
	{
		OutputXml.FlashStatus flashStatus = new OutputXml.FlashStatus();
		OutputXml.Station station = new OutputXml.Station();
		if (!string.IsNullOrEmpty(errorMsg))
		{
			station.ErrorMsg = errorMsg;
		}
		flashStatus.FlashStation = station;
		if (jObject == null)
		{
			return;
		}
		OutputXml.XmlDevice xmlDevice = new OutputXml.XmlDevice();
		xmlDevice.DataCable = dataCable;
		xmlDevice.OEM = (string?)jObject.GetValue("OEM");
		if (xmlDevice.OEM == null)
		{
			xmlDevice.OEM = "HMD Global";
		}
		xmlDevice.Model = (string?)jObject.GetValue("ProductModel");
		xmlDevice.IMEI = (string?)jObject.GetValue("IMEI");
		string text = (string?)jObject.GetValue("ProductTAcode");
		switch (xmlDevice.Model)
		{
		case "Armstrong_VZW":
		{
			string text2 = (string?)jObject.GetValue("Version");
			if (text2 == "0_40B" || text2 == "0_440" || text2 == "0_500")
			{
				xmlDevice.Software = "00VZW_" + text2;
			}
			else if (text == "TA-1231" || text == "TA-1221")
			{
				xmlDevice.Software = "00VZW_" + text2 + "_" + text.Substring(3);
			}
			else
			{
				xmlDevice.Software = "00VZW_" + text2;
			}
			break;
		}
		case "RiseAgainst_VZW":
			xmlDevice.Software = "00VPO_" + (string?)jObject.GetValue("Version");
			break;
		case "Deadpool_VZW":
		case "Nokia 3 V":
			xmlDevice.Software = "00VZW_" + (string?)jObject.GetValue("Version");
			break;
		case "aoki":
			xmlDevice.Software = versionPrefix + (string?)jObject.GetValue("SWVersion");
			break;
		default:
			xmlDevice.Software = versionPrefix + (string?)jObject.GetValue("Version");
			break;
		}
		xmlDevice.FlashResult = (isFlashSuccess ? "Pass" : "Fail");
		xmlDevice.DataRemovalResult = (isEraseSuccess ? "Pass" : "Fail");
		if (eraseFrpResult != 0)
		{
			xmlDevice.EraseFrpResult = ((eraseFrpResult == EraseFrpResult.PASS) ? "Pass" : "Fail");
		}
		flashStatus.Device = xmlDevice;
		flashStatus.ToVzwXmlFile(null);
		flashStatus.ToCdrXmlFile(null);
	}

	private int CreateUnlockLog(UnlockLogMode mode, string outcome = "N/A")
	{
		bool flag = false;
		try
		{
			OutputXml.Unlockresult unlockresult = new OutputXml.Unlockresult();
			unlockresult.UnlockStation = new OutputXml.Station(AzureNativeClient.Instance.UserName);
			JObject deviceInfoJson = GetDeviceInfoJson(isFastboot: true);
			OutputXml.XmlDevice xmlDevice = new OutputXml.XmlDevice();
			xmlDevice.DataCable = dataCable;
			if (deviceInfoJson != null)
			{
				xmlDevice.OEM = (string?)deviceInfoJson.GetValue("OEM");
				xmlDevice.Model = (string?)deviceInfoJson.GetValue("ProductModel");
				xmlDevice.IMEI = (string?)deviceInfoJson.GetValue("IMEI");
				xmlDevice.OSPlatform = (string?)deviceInfoJson.GetValue("OSPlatform");
				xmlDevice.OSVersion = (string?)deviceInfoJson.GetValue("OSVersion");
				xmlDevice.BasebandVersion = (string?)deviceInfoJson.GetValue("BasebandVersion");
				xmlDevice.AntiTheftStatus = (string?)deviceInfoJson.GetValue("AntiTheftStatus");
			}
			else
			{
				xmlDevice.OEM = devInfo.OEM;
				xmlDevice.Model = devInfo.ProductModel;
				xmlDevice.IMEI = devInfo.IMEI;
				xmlDevice.OSPlatform = devInfo.OSPlatform;
				xmlDevice.OSVersion = devInfo.OSVersion;
				xmlDevice.BasebandVersion = devInfo.BasebandVersion;
				xmlDevice.AntiTheftStatus = devInfo.AntiTheftStatus;
			}
			unlockresult.Device = xmlDevice;
			if (mode == UnlockLogMode.Unlock)
			{
				xmlDevice.Outcome = outcome;
			}
			flag = unlockresult.ToXmlFile(mode);
		}
		catch (Exception ex)
		{
			flag = false;
			LogUtility.D("CreateUnlockStatusLog", ex.ToString());
		}
		return (!flag) ? (-1) : 0;
	}

	public void UpdateCurrentSkuId()
	{
		if (string.IsNullOrEmpty(currentSku) && RequestHmdPermission(HmdPermissionType.Repair) == 0)
		{
			currentSku = commandApi.GetSkuID();
		}
	}

	private bool UnzipOtaZip(DoWorkEventArgs e, int progress, int progressRange)
	{
		if (base.IsWorkerCancel)
		{
			OnFlashError(e, "Ota update fail: User cancel.");
			return false;
		}
		InvokeUpdateLogEvent("  > Extracting ota zip");
		PrintLog($"UnzipOtaZip() from progress = {progress}");
		int baseProgress = progress;
		ZipUtility.ZipError zipError = ZipUtility.UnzipFile(this, romConfig.ZipPath, romConfig.BasePath, delegate(long now, long total)
		{
			int num = (int)((double)now / (double)total * (double)progressRange) + baseProgress;
			if (num > progress)
			{
				progress = num;
				UpdateProgressChanged(num);
			}
		}, isCleanTarget: true);
		if (zipError != 0)
		{
			OnUnzipFail(e, zipError, isOta: true);
			return false;
		}
		InvokeUpdateLogEvent("  > Success");
		return true;
	}

	private int GetOtaFlashProgress(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return -1;
		}
		int num = -1;
		string pattern = "serving: \\'.*\\' *\\(~?(?<progress>.*)%\\)";
		Match match = Regex.Match(input, pattern);
		try
		{
			string s = match.Groups["progress"].Value.Replace(",", "");
			num = int.Parse(s);
		}
		catch (Exception)
		{
			num = -1;
		}
		return num;
	}

	private CommandApi.ErrorCode ReadItemByFastBoot(DeviceItemType deviceItemType, StringBuilder sb)
	{
		CommandApi.ErrorCode errorCode = CommandApi.ErrorCode.NoError;
		switch (deviceItemType)
		{
		case DeviceItemType.WallPaper:
			errorCode = commandApi.GetWallpaper(sb);
			break;
		case DeviceItemType.SkuId:
			errorCode = commandApi.GetSkuId(sb);
			break;
		case DeviceItemType.ReadOnlyImei:
			errorCode = CommandApi.ErrorCode.NoError;
			if (devInfo == null || IsDevInfoObsolete())
			{
				errorCode = UpdateDevInfo();
				if (errorCode != 0)
				{
					return errorCode;
				}
			}
			sb.Append(devInfo.IMEI);
			break;
		case DeviceItemType.AntiTheftStatus:
			errorCode = CommandApi.ErrorCode.NoError;
			if (devInfo == null || IsDevInfoObsolete())
			{
				errorCode = UpdateDevInfo();
				if (errorCode != 0)
				{
					return errorCode;
				}
			}
			sb.Append(devInfo.AntiTheftStatus);
			break;
		case DeviceItemType.Model:
			errorCode = CommandApi.ErrorCode.NoError;
			if (devInfo == null || IsDevInfoObsolete())
			{
				errorCode = UpdateDevInfo();
				if (errorCode != 0)
				{
					return errorCode;
				}
			}
			sb.Append(devInfo.ProductModel);
			break;
		}
		return errorCode;
	}

	private bool IsDevInfoObsolete()
	{
		if ((DateTime.Now - devInfo.CreateTime).TotalSeconds > 5.0)
		{
			return true;
		}
		return false;
	}

	private CommandApi.ErrorCode UpdateDevInfo()
	{
		StringBuilder stringBuilder = new StringBuilder();
		CommandApi.ErrorCode deviceInfo = commandApi.GetDeviceInfo(stringBuilder);
		if (deviceInfo == CommandApi.ErrorCode.NoError)
		{
			try
			{
				devInfo = JsonConvert.DeserializeObject<DeviceInfo>(stringBuilder.ToString());
				devInfo.CreateTime = DateTime.Now;
			}
			catch (Exception ex)
			{
				LogUtility.E(Tag, "Can not parse device info json with exception.\n" + ex.StackTrace + "\noriginal json string = " + stringBuilder.ToString());
				return CommandApi.ErrorCode.JsonFormatError;
			}
		}
		return deviceInfo;
	}

	private CommandApi.ErrorCode WriteItemByFastBoot(DeviceItemType deviceItemType, string value)
	{
		CommandApi.ErrorCode result = CommandApi.ErrorCode.NoError;
		switch (deviceItemType)
		{
		case DeviceItemType.WallPaper:
			result = commandApi.SetWallpaper(value);
			break;
		case DeviceItemType.SkuId:
			result = commandApi.SetSkuId(value);
			break;
		}
		return result;
	}

	private EraseFrpResult EraseFrp()
	{
		if (RequestHmdPermission(HmdPermissionType.Repair) != 0)
		{
			LogUtility.D(Tag, "EraseFrp failed due to lack of repair permission");
			return EraseFrpResult.FAIL;
		}
		CommandApi.ErrorCode errorCode = commandApi.Erase("config", null);
		if (errorCode == CommandApi.ErrorCode.NoError)
		{
			return EraseFrpResult.PASS;
		}
		LogUtility.D(Tag, "EraseFrp failed due to ret is " + errorCode);
		return EraseFrpResult.FAIL;
	}

	protected override int DoFrpErase(object sender, DoWorkEventArgs e, object argument)
	{
		what = 14;
		int result = 0;
		int result2 = -1;
		if (base.DoFrpErase(sender, e, argument) == 0)
		{
			CommandApi.ErrorCode errorCode = commandApi.Erase("config", null);
			if (errorCode == CommandApi.ErrorCode.NoError)
			{
				deviceEventType = DeviceEventType.OnCommandSuccess;
				return result;
			}
			e.Result = CommandApi.GetErrorMessage(errorCode);
			deviceEventType = DeviceEventType.OnCommandFail;
			return result2;
		}
		e.Result = "request permission fail";
		deviceEventType = DeviceEventType.OnCommandFail;
		return result2;
	}

	protected override int DoGetSku(object sender, DoWorkEventArgs e, object argument)
	{
		if (base.DoGetSku(sender, e, argument) != 0)
		{
			deviceEventType = DeviceEventType.OnCommandFail;
			e.Result = "GetSkuFail: request permission fail.";
			return -1;
		}
		StringBuilder stringBuilder = new StringBuilder(20);
		CommandApi.ErrorCode skuId = commandApi.GetSkuId(stringBuilder);
		base.What = 15;
		if (skuId != 0)
		{
			deviceEventType = DeviceEventType.OnCommandFail;
			e.Result = "GetSkuFail: " + CommandApi.GetErrorMessage(skuId);
			return -1;
		}
		deviceEventType = DeviceEventType.OnCommandSuccess;
		currentSku = stringBuilder.ToString();
		return 0;
	}
}
