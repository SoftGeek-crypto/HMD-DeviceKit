using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace hmd_pctool_windows;

internal class HmdOemSc : HmdOemInterface
{
	public class HmdLibApiSc
	{
		[DllImport("HMDSimlock.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_init")]
		public static extern bool Init();

		[DllImport("HMDSimlock.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_deinit")]
		public static extern bool Deinit();

		[DllImport("HMDSimlock.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_Simlock")]
		public static extern bool Simlock(string filepath, StringBuilder key1, StringBuilder key2);

		[DllImport("HMDSimlock.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_SimUnlock")]
		public static extern bool SimUnlock(string key1, string key2);

		[DllImport("HMDSimlock.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_ReadPsn")]
		public static extern bool ReadPsn(StringBuilder sn);

		[DllImport("HMDSimlock.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_WritePsn")]
		public static extern bool WritePsn(StringBuilder sn);

		[DllImport("HMDSimlock.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_ReadImei")]
		public static extern bool ReadImei(StringBuilder imei);

		[DllImport("HMDSimlock.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_WriteImei")]
		public static extern bool WriteImei(StringBuilder imei);

		[DllImport("HMDSimlock.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_ReadImei2")]
		public static extern bool ReadImei2(StringBuilder imei);

		[DllImport("HMDSimlock.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_WriteImei2")]
		public static extern bool WriteImei2(StringBuilder imei);

		[DllImport("HMDSimlock.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_ReadMeid")]
		public static extern bool ReadMeid(StringBuilder meid);

		[DllImport("HMDSimlock.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_WriteMeid")]
		public static extern bool WriteMeid(StringBuilder meid);

		[DllImport("HMDSimlock.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_ReadWiFiAddr")]
		public static extern bool ReadWiFiAddr(StringBuilder addr);

		[DllImport("HMDSimlock.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_WriteWiFiAddr")]
		public static extern bool WriteWiFiAddr(StringBuilder addr);

		[DllImport("HMDSimlock.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_ReadBTAddr")]
		public static extern bool ReadBTAddr(StringBuilder addr);

		[DllImport("HMDSimlock.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_WriteBTAddr")]
		public static extern bool WriteBTAddr(StringBuilder addr);
	}

	private CommandApi commandApi;

	private const string DLLNAME = "HMDSimlock.dll";

	private bool initSuccess;

	private Action<bool> mCallback;

	public string GetDllName()
	{
		return "HMDSimlock:enter_calibration";
	}

	public bool Init()
	{
		return true;
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
		Console.WriteLine("Enter meta mode by command fastboot oem reboot meta success");
		Console.WriteLine("Wait 10 sec to call HmdLibrary_sc Init()");
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
		return HmdLibApiSc.Simlock(filepath, key1, key2);
	}

	public bool SimUnlock(string key1, string key2)
	{
		bool result = HmdLibApiSc.SimUnlock(key1, key2);
		Console.WriteLine("SimUnlock return :" + result);
		return result;
	}

	public bool ReadPsn(StringBuilder sn)
	{
		return HmdLibApiSc.ReadPsn(sn);
	}

	public bool WritePsn(StringBuilder sn)
	{
		return HmdLibApiSc.WritePsn(sn);
	}

	public bool ReadImei(StringBuilder imei)
	{
		return HmdLibApiSc.ReadImei(imei);
	}

	public bool WriteImei(StringBuilder imei)
	{
		return HmdLibApiSc.WriteImei(imei);
	}

	public bool ReadImei2(StringBuilder imei)
	{
		return HmdLibApiSc.ReadImei2(imei);
	}

	public bool WriteImei2(StringBuilder imei)
	{
		return HmdLibApiSc.WriteImei2(imei);
	}

	public bool ReadMeid(StringBuilder meid)
	{
		return HmdLibApiSc.ReadMeid(meid);
	}

	public bool WriteMeid(StringBuilder meid)
	{
		return false;
	}

	public bool ReadWiFiAddr(StringBuilder addr)
	{
		return HmdLibApiSc.ReadWiFiAddr(addr);
	}

	public bool WriteWiFiAddr(StringBuilder addr)
	{
		return HmdLibApiSc.WriteWiFiAddr(addr);
	}

	public bool ReadBTAddr(StringBuilder addr)
	{
		return HmdLibApiSc.ReadBTAddr(addr);
	}

	public bool WriteBTAddr(StringBuilder addr)
	{
		return HmdLibApiSc.WriteBTAddr(addr);
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
