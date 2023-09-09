using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace hmd_pctool_windows;

internal class HmdOemMw : HmdOemInterface
{
	private class HmdLibApiMw
	{
		[DllImport("hmdLibrary_mw.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_init")]
		public static extern bool Init();

		[DllImport("hmdLibrary_mw.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_deinit")]
		public static extern bool Deinit();

		[DllImport("hmdLibrary_mw.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_Simlock")]
		public static extern bool Simlock(string filepath, StringBuilder key1, StringBuilder key2);

		[DllImport("hmdLibrary_mw.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_SimUnlock")]
		public static extern bool SimUnlock(string key1, string key2);

		[DllImport("hmdLibrary_mw.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_ReadPsn")]
		public static extern bool ReadPsn(StringBuilder sn);

		[DllImport("hmdLibrary_mw.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_WritePsn")]
		public static extern bool WritePsn(StringBuilder sn);

		[DllImport("hmdLibrary_mw.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_ReadImei")]
		public static extern bool ReadImei(StringBuilder imei);

		[DllImport("hmdLibrary_mw.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_WriteImei")]
		public static extern bool WriteImei(StringBuilder imei);

		[DllImport("hmdLibrary_mw.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_ReadImei2")]
		public static extern bool ReadImei2(StringBuilder imei);

		[DllImport("hmdLibrary_mw.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_WriteImei2")]
		public static extern bool WriteImei2(StringBuilder imei);

		[DllImport("hmdLibrary_mw.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_ReadMeid")]
		public static extern bool ReadMeid(StringBuilder meid);

		[DllImport("hmdLibrary_mw.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_WriteMeid")]
		public static extern bool WriteMeid(StringBuilder meid);

		[DllImport("hmdLibrary_mw.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_ReadWiFiAddr")]
		public static extern bool ReadWiFiAddr(StringBuilder addr);

		[DllImport("hmdLibrary_mw.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_WriteWiFiAddr")]
		public static extern bool WriteWiFiAddr(StringBuilder addr);

		[DllImport("hmdLibrary_mw.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_ReadBTAddr")]
		public static extern bool ReadBTAddr(StringBuilder addr);

		[DllImport("hmdLibrary_mw.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_WriteBTAddr")]
		public static extern bool WriteBTAddr(StringBuilder addr);
	}

	private CommandApi commandApi;

	public const string DLLNAME = "hmdLibrary_mw.dll";

	private bool initSuccess;

	private Action<bool> mCallback;

	public string GetDllName()
	{
		return "HMDSimlockMw:enter_calibration";
	}

	public bool Init()
	{
		return HmdLibApiMw.Init();
	}

	public bool Deinit()
	{
		return true;
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
		if (commandApi.ExecFastbootCommand("oem enter_calibration").Contains("ExitWithErrorCode"))
		{
			MessageBox.Show("Cannot enter the Calibration mode, please make sure the software is supported !");
			if (mCallback != null)
			{
				mCallback(obj: false);
			}
		}
		Console.WriteLine("Enter calibration by command fastboot oem enter_calibration success");
		Console.WriteLine("Wait 10 sec to call HmdLibrary_Mw Init()");
		Thread.Sleep(10000);
		initSuccess = true;
		if (mCallback != null)
		{
			if (!initSuccess)
			{
				MessageBox.Show("Cannot enter the Calibration mode, please make sure the software is supported !!!");
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
		return HmdLibApiMw.Simlock(filepath, key1, key2);
	}

	public bool SimUnlock(string key1, string key2)
	{
		return HmdLibApiMw.SimUnlock(key1, key2);
	}

	public bool ReadPsn(StringBuilder sn)
	{
		return HmdLibApiMw.ReadPsn(sn);
	}

	public bool WritePsn(StringBuilder sn)
	{
		return HmdLibApiMw.WritePsn(sn);
	}

	public bool ReadImei(StringBuilder imei)
	{
		return HmdLibApiMw.ReadImei(imei);
	}

	public bool WriteImei(StringBuilder imei)
	{
		return HmdLibApiMw.WriteImei(imei);
	}

	public bool ReadImei2(StringBuilder imei)
	{
		return HmdLibApiMw.ReadImei2(imei);
	}

	public bool WriteImei2(StringBuilder imei)
	{
		return HmdLibApiMw.WriteImei2(imei);
	}

	public bool ReadMeid(StringBuilder meid)
	{
		return HmdLibApiMw.ReadMeid(meid);
	}

	public bool WriteMeid(StringBuilder meid)
	{
		return HmdLibApiMw.WriteMeid(meid);
	}

	public bool ReadWiFiAddr(StringBuilder addr)
	{
		return HmdLibApiMw.ReadWiFiAddr(addr);
	}

	public bool WriteWiFiAddr(StringBuilder addr)
	{
		return HmdLibApiMw.WriteWiFiAddr(addr);
	}

	public bool ReadBTAddr(StringBuilder addr)
	{
		return HmdLibApiMw.ReadBTAddr(addr);
	}

	public bool WriteBTAddr(StringBuilder addr)
	{
		return HmdLibApiMw.WriteBTAddr(addr);
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
