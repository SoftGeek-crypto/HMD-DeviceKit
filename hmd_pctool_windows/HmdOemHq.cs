using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace hmd_pctool_windows;

internal class HmdOemHq : HmdOemInterface
{
	public class HmdLibApiHq
	{
		[DllImport("hmdLibrary_hq.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_Simlock")]
		public static extern bool Simlock(string filepath, StringBuilder key1, StringBuilder key2);

		[DllImport("hmdLibrary_hq.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_SimUnlock")]
		public static extern bool SimUnlock(string key1, string key2);

		[DllImport("hmdLibrary_hq.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_ReadPsn")]
		public static extern bool ReadPsn(StringBuilder sn);

		[DllImport("hmdLibrary_hq.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_WritePsn")]
		public static extern bool WritePsn(StringBuilder sn);

		[DllImport("hmdLibrary_hq.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_ReadImei")]
		public static extern bool ReadImei(StringBuilder imei);

		[DllImport("hmdLibrary_hq.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_WriteImei")]
		public static extern bool WriteImei(StringBuilder imei);

		[DllImport("hmdLibrary_hq.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_ReadImei2")]
		public static extern bool ReadImei2(StringBuilder imei);

		[DllImport("hmdLibrary_hq.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_WriteImei2")]
		public static extern bool WriteImei2(StringBuilder imei);

		[DllImport("hmdLibrary_hq.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_ReadMeid")]
		public static extern bool ReadMeid(StringBuilder meid);

		[DllImport("hmdLibrary_hq.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_Init")]
		public static extern bool Init();

		[DllImport("hmdLibrary_hq.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_Deinit")]
		public static extern bool Deinit();

		[DllImport("hmdLibrary_hq.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_ReadWiFiAddr")]
		public static extern bool ReadWiFiAddr(StringBuilder addr);

		[DllImport("hmdLibrary_hq.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_WriteWiFiAddr")]
		public static extern bool WriteWiFiAddr(StringBuilder addr);

		[DllImport("hmdLibrary_hq.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_ReadBTAddr")]
		public static extern bool ReadBTAddr(StringBuilder addr);

		[DllImport("hmdLibrary_hq.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_WriteBTAddr")]
		public static extern bool WriteBTAddr(StringBuilder addr);

		[DllImport("hmdLibrary_hq.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_GetLastError")]
		public static extern void GetLastError(StringBuilder sErrorInfo);
	}

	private CommandApi commandApi;

	private const string DLLNAME = "hmdLibrary_hq.dll";

	private bool initSuccess;

	private Action<bool> mCallback;

	public string GetDllName()
	{
		return "hmdLibrary_hq:reboot-edl";
	}

	public bool Init()
	{
		return initSuccess;
	}

	public bool Deinit()
	{
		if (!initSuccess)
		{
			return true;
		}
		bool result = true;
		try
		{
			result = HmdLibApiHq.Deinit();
		}
		catch (Exception)
		{
		}
		return result;
	}

	public bool isSupportSlot()
	{
		return true;
	}

	public bool EnterPhoneEditMode(string serialNo, Action<bool> callback)
	{
		mCallback = callback;
		initSuccess = false;
		if (commandApi == null)
		{
			return false;
		}
		new Thread((ThreadStart)delegate
		{
			Thread.Sleep(1000);
			if (HmdPcToolApi.rebootEdl() != 0)
			{
				MessageBox.Show("Cannot enter the META mode, please make sure the software is supported.");
				if (mCallback != null)
				{
					mCallback(obj: false);
				}
			}
		}).Start();
		initSuccess = HmdLibApiHq.Init();
		string text = "";
		if (!initSuccess)
		{
			Console.WriteLine("Init failed");
		}
		else
		{
			Console.WriteLine("Init success");
		}
		if (mCallback != null)
		{
			if (!initSuccess)
			{
				MessageBox.Show("Cannot enter the META mode, please make sure the software is supported.");
			}
			mCallback(initSuccess);
		}
		return false;
	}

	public void StopEnterPhoneEditMode()
	{
		mCallback = null;
	}

	public bool Simlock(string filepath, StringBuilder key1, StringBuilder key2)
	{
		return HmdLibApiHq.Simlock(filepath, key1, key2);
	}

	public bool SimUnlock(string key1, string key2)
	{
		return HmdLibApiHq.SimUnlock(key1, key2);
	}

	public bool ReadPsn(StringBuilder sn)
	{
		return HmdLibApiHq.ReadPsn(sn);
	}

	public bool WritePsn(StringBuilder sn)
	{
		return HmdLibApiHq.WritePsn(sn);
	}

	public bool ReadImei(StringBuilder imei)
	{
		return HmdLibApiHq.ReadImei(imei);
	}

	public bool WriteImei(StringBuilder imei)
	{
		return HmdLibApiHq.WriteImei(imei);
	}

	public bool ReadImei2(StringBuilder imei)
	{
		return HmdLibApiHq.ReadImei2(imei);
	}

	public bool WriteImei2(StringBuilder imei)
	{
		return HmdLibApiHq.WriteImei2(imei);
	}

	public bool ReadMeid(StringBuilder meid)
	{
		return HmdLibApiHq.ReadMeid(meid);
	}

	public bool WriteMeid(StringBuilder meid)
	{
		return false;
	}

	public bool ReadWiFiAddr(StringBuilder addr)
	{
		return HmdLibApiHq.ReadWiFiAddr(addr);
	}

	public bool WriteWiFiAddr(StringBuilder addr)
	{
		return HmdLibApiHq.WriteWiFiAddr(addr);
	}

	public bool ReadBTAddr(StringBuilder addr)
	{
		return HmdLibApiHq.ReadBTAddr(addr);
	}

	public bool WriteBTAddr(StringBuilder addr)
	{
		return HmdLibApiHq.WriteBTAddr(addr);
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
