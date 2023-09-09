using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace hmd_pctool_windows;

internal class HmdOemIris : HmdOemInterface
{
	public class HmdLibApiIris
	{
		[DllImport("HmdLibrary_Iris.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_init")]
		public static extern bool Init();

		[DllImport("HmdLibrary_Iris.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_deinit")]
		public static extern bool Deinit();

		[DllImport("HmdLibrary_Iris.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_Simlock")]
		public static extern bool Simlock(string filepath, StringBuilder key1, StringBuilder key2);

		[DllImport("HmdLibrary_Iris.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_SimUnlock")]
		public static extern bool SimUnlock(string key1, string key2);

		[DllImport("HmdLibrary_Iris.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_ReadPsn")]
		public static extern bool ReadPsn(StringBuilder sn);

		[DllImport("HmdLibrary_Iris.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_WritePsn")]
		public static extern bool WritePsn(StringBuilder sn);

		[DllImport("HmdLibrary_Iris.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_ReadImei")]
		public static extern bool ReadImei(StringBuilder imei);

		[DllImport("HmdLibrary_Iris.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_WriteImei")]
		public static extern bool WriteImei(StringBuilder imei);

		[DllImport("HmdLibrary_Iris.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_ReadImei2")]
		public static extern bool ReadImei2(StringBuilder imei);

		[DllImport("HmdLibrary_Iris.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_WriteImei2")]
		public static extern bool WriteImei2(StringBuilder imei);

		[DllImport("HmdLibrary_Iris.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_ReadMeid")]
		public static extern bool ReadMeid(StringBuilder meid);

		[DllImport("HmdLibrary_Iris.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_WriteMeid")]
		public static extern bool WriteMeid(StringBuilder meid);

		[DllImport("HmdLibrary_Iris.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_ReadWiFiAddr")]
		public static extern bool ReadWiFiAddr(StringBuilder addr);

		[DllImport("HmdLibrary_Iris.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_WriteWiFiAddr")]
		public static extern bool WriteWiFiAddr(StringBuilder addr);

		[DllImport("HmdLibrary_Iris.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_ReadBTAddr")]
		public static extern bool ReadBTAddr(StringBuilder addr);

		[DllImport("HmdLibrary_Iris.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "HMDLib_WriteBTAddr")]
		public static extern bool WriteBTAddr(StringBuilder addr);
	}

	private CommandApi commandApi;

	private const string DLLNAME = "HmdLibrary_Iris.dll";

	private bool initSuccess;

	private Action<bool> mCallback;

	public string GetDllName()
	{
		return "HmdLibrary_Iris:oem enter_calibration";
	}

	public bool Init()
	{
		return HmdLibApiIris.Init();
	}

	public bool Deinit()
	{
		return HmdLibApiIris.Deinit();
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
		return HmdLibApiIris.Simlock(filepath, key1, key2);
	}

	public bool SimUnlock(string key1, string key2)
	{
		return HmdLibApiIris.SimUnlock(key1, key2);
	}

	public bool ReadPsn(StringBuilder sn)
	{
		return HmdLibApiIris.ReadPsn(sn);
	}

	public bool WritePsn(StringBuilder sn)
	{
		return HmdLibApiIris.WritePsn(sn);
	}

	public bool ReadImei(StringBuilder imei)
	{
		return HmdLibApiIris.ReadImei(imei);
	}

	public bool WriteImei(StringBuilder imei)
	{
		return HmdLibApiIris.WriteImei(imei);
	}

	public bool ReadImei2(StringBuilder imei)
	{
		return HmdLibApiIris.ReadImei2(imei);
	}

	public bool WriteImei2(StringBuilder imei)
	{
		return HmdLibApiIris.WriteImei2(imei);
	}

	public bool ReadMeid(StringBuilder meid)
	{
		return HmdLibApiIris.ReadMeid(meid);
	}

	public bool WriteMeid(StringBuilder meid)
	{
		return false;
	}

	public bool ReadWiFiAddr(StringBuilder addr)
	{
		return HmdLibApiIris.ReadWiFiAddr(addr);
	}

	public bool WriteWiFiAddr(StringBuilder addr)
	{
		return HmdLibApiIris.WriteWiFiAddr(addr);
	}

	public bool ReadBTAddr(StringBuilder addr)
	{
		return HmdLibApiIris.ReadBTAddr(addr);
	}

	public bool WriteBTAddr(StringBuilder addr)
	{
		return HmdLibApiIris.WriteBTAddr(addr);
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
