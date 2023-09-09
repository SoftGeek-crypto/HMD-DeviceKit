using System;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace hmd_pctool_windows;

internal class HmdOemLc : HmdOemInterface
{
	private class HmdLibApiLc
	{
		[DllImport("hmdLibrary_lc.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_init")]
		public static extern bool Init();

		[DllImport("hmdLibrary_lc.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_deinit")]
		public static extern bool Deinit();

		[DllImport("hmdLibrary_lc.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_Simlock")]
		public static extern bool Simlock(string filepath, StringBuilder key1, StringBuilder key2);

		[DllImport("hmdLibrary_lc.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_SimUnlock")]
		public static extern bool SimUnlock(string key1, string key2);

		[DllImport("hmdLibrary_lc.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_ReadPsn")]
		public static extern bool ReadPsn(StringBuilder sn);

		[DllImport("hmdLibrary_lc.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_WritePsn")]
		public static extern bool WritePsn(StringBuilder sn);

		[DllImport("hmdLibrary_lc.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_ReadImei")]
		public static extern bool ReadImei(StringBuilder imei);

		[DllImport("hmdLibrary_lc.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_WriteImei")]
		public static extern bool WriteImei(StringBuilder imei);

		[DllImport("hmdLibrary_lc.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_ReadImei2")]
		public static extern bool ReadImei2(StringBuilder imei);

		[DllImport("hmdLibrary_lc.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_WriteImei2")]
		public static extern bool WriteImei2(StringBuilder imei);

		[DllImport("hmdLibrary_lc.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_ReadMeid")]
		public static extern bool ReadMeid(StringBuilder meid);

		[DllImport("hmdLibrary_lc.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_WriteMeid")]
		public static extern bool WriteMeid(StringBuilder meid);

		[DllImport("hmdLibrary_lc.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_ReadWiFiAddr")]
		public static extern bool ReadWiFiAddr(StringBuilder addr);

		[DllImport("hmdLibrary_lc.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_WriteWiFiAddr")]
		public static extern bool WriteWiFiAddr(StringBuilder addr);

		[DllImport("hmdLibrary_lc.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_ReadBTAddr")]
		public static extern bool ReadBTAddr(StringBuilder addr);

		[DllImport("hmdLibrary_lc.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_WriteBTAddr")]
		public static extern bool WriteBTAddr(StringBuilder addr);
	}

	private CommandApi commandApi;

	public const string DLLNAME = "hmdLibrary_lc.dll";

	private Action<bool> mCallback;

	private bool isStopCheckDevice = false;

	public string GetDllName()
	{
		return "hmdLibrary_lc:continue";
	}

	public bool Init()
	{
		return HmdLibApiLc.Init();
	}

	public bool Deinit()
	{
		return HmdLibApiLc.Deinit();
	}

	public bool isSupportSlot()
	{
		return true;
	}

	public bool EnterPhoneEditMode(string serialNo, Action<bool> callback)
	{
		if (isDiagDeviceAvailable())
		{
			return true;
		}
		mCallback = callback;
		isStopCheckDevice = false;
		if (commandApi == null)
		{
			return false;
		}
		commandApi.SetActiveSlot("a");
		commandApi.ContinueBoot();
		checkDiagMode();
		return false;
	}

	public void StopEnterPhoneEditMode()
	{
		isStopCheckDevice = true;
	}

	private bool isDiagDeviceAvailable()
	{
		ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PnPEntity WHERE ClassGuid=\"{4d36e978-e325-11ce-bfc1-08002be10318}\"");
		foreach (ManagementObject item in managementObjectSearcher.Get())
		{
			if (item.GetPropertyValue("Name").ToString().Contains("Qualcomm HS-USB MDM Diagnostics") || item.GetPropertyValue("Name").ToString().Contains("Qualcomm HS-USB Diagnostics"))
			{
				return true;
			}
		}
		return false;
	}

	private void checkDiagMode()
	{
		Thread.Sleep(20000);
		int num = 0;
		bool flag = false;
		while (true)
		{
			if (isStopCheckDevice)
			{
				return;
			}
			Thread.Sleep(500);
			if (isDiagDeviceAvailable())
			{
				break;
			}
			num++;
			if (num > 180)
			{
				flag = true;
				break;
			}
		}
		Thread.Sleep(10000);
		if (flag)
		{
			MessageBox.Show("Cannot enter the DIAG mode, please make sure the software is supported.");
		}
		mCallback(!flag);
	}

	public bool Simlock(string filepath, StringBuilder key1, StringBuilder key2)
	{
		return HmdLibApiLc.Simlock(filepath, key1, key2);
	}

	public bool SimUnlock(string key1, string key2)
	{
		return HmdLibApiLc.SimUnlock(key1, key2);
	}

	public bool ReadPsn(StringBuilder sn)
	{
		return HmdLibApiLc.ReadPsn(sn);
	}

	public bool WritePsn(StringBuilder sn)
	{
		return HmdLibApiLc.WritePsn(sn);
	}

	public bool ReadImei(StringBuilder imei)
	{
		return HmdLibApiLc.ReadImei(imei);
	}

	public bool WriteImei(StringBuilder imei)
	{
		return HmdLibApiLc.WriteImei(imei);
	}

	public bool ReadImei2(StringBuilder imei)
	{
		return HmdLibApiLc.ReadImei2(imei);
	}

	public bool WriteImei2(StringBuilder imei)
	{
		return HmdLibApiLc.WriteImei2(imei);
	}

	public bool ReadMeid(StringBuilder meid)
	{
		return HmdLibApiLc.ReadMeid(meid);
	}

	public bool WriteMeid(StringBuilder meid)
	{
		return HmdLibApiLc.WriteMeid(meid);
	}

	public bool ReadWiFiAddr(StringBuilder addr)
	{
		return HmdLibApiLc.ReadWiFiAddr(addr);
	}

	public bool WriteWiFiAddr(StringBuilder addr)
	{
		return HmdLibApiLc.WriteWiFiAddr(addr);
	}

	public bool ReadBTAddr(StringBuilder addr)
	{
		return HmdLibApiLc.ReadBTAddr(addr);
	}

	public bool WriteBTAddr(StringBuilder addr)
	{
		return HmdLibApiLc.WriteBTAddr(addr);
	}

	public void SetCommandApi(CommandApi commandApi)
	{
		this.commandApi = commandApi;
	}

	public bool WriteImei(StringBuilder imei, StringBuilder signature)
	{
		throw new NotImplementedException();
	}

	public bool WriteImei2(StringBuilder imei, StringBuilder signature)
	{
		throw new NotImplementedException();
	}
}
