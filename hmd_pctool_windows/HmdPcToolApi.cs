using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace hmd_pctool_windows;

internal class HmdPcToolApi
{
	public delegate void UpdateLog(string log);

	[DllImport("hmd_pctool.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint getDeviceList(StringBuilder devices);

	[DllImport("hmd_pctool.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint updateZip(string path, UpdateLog updateLog);

	[DllImport("hmd_pctool.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint updateZipWipe(string path, UpdateLog updateLog);

	[DllImport("hmd_pctool.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint flashZip(string partition, string path, UpdateLog updateLog);

	[DllImport("hmd_pctool.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint flashZipSlot(string partition, string slot, string path, UpdateLog updateLog);

	[DllImport("hmd_pctool.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint setActiveDevice(string serialNo);

	[DllImport("hmd_pctool.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint getLevel(ref int level);

	[DllImport("hmd_pctool.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "getWallpapered")]
	private static extern uint getWallpaper(StringBuilder value);

	[DllImport("hmd_pctool.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "setWallpapered")]
	public static extern uint setWallpaper(string value);

	[DllImport("hmd_pctool.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	private static extern uint getSkuId(StringBuilder value);

	[DllImport("hmd_pctool.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint setSkuId(string value);

	[DllImport("hmd_pctool.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint setActiveSlot(string slot);

	[DllImport("hmd_pctool.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint simUnclock(string code);

	[DllImport("hmd_pctool.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint simlock();

	[DllImport("hmd_pctool.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint getSimlockStatus(ref bool isLocked);

	[DllImport("hmd_pctool.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint erase(string partition, UpdateLog updateLog);

	[DllImport("hmd_pctool.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint continueBoot();

	[DllImport("hmd_pctool.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint reboot();

	[DllImport("hmd_pctool.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint rebootEdl();

	[DllImport("hmd_pctool.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint rebootBootloader();

	[DllImport("hmd_pctool.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint getPermission(ref byte permission);

	[DllImport("hmd_pctool.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint authStart(StringBuilder nounce);

	[DllImport("hmd_pctool.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint getSecurityVersion(StringBuilder version);

	[DllImport("hmd_pctool.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint getDllName(StringBuilder version);

	[DllImport("hmd_pctool.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint requestPermission(HmdPermissionType pType, string response);

	[DllImport("hmd_pctool.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern uint getVar(string property, StringBuilder product);

	public static string getWallpaperID()
	{
		try
		{
			StringBuilder stringBuilder = new StringBuilder(128);
			getWallpaper(stringBuilder);
			return stringBuilder.ToString();
		}
		catch (Exception ex)
		{
			MessageBox.Show("Fail to get Wallpaper ID\nReason : " + ex.Message);
			return string.Empty;
		}
	}

	public static string getSkuID()
	{
		try
		{
			StringBuilder stringBuilder = new StringBuilder(128);
			getSkuId(stringBuilder);
			return stringBuilder.ToString();
		}
		catch (Exception ex)
		{
			MessageBox.Show("Fail to get SKU ID\nReason : " + ex.Message);
			return string.Empty;
		}
	}

	public static string getVarValue(string property)
	{
		try
		{
			StringBuilder stringBuilder = new StringBuilder(128);
			getVar(property.ToLower(), stringBuilder);
			return stringBuilder.ToString();
		}
		catch (Exception ex)
		{
			MessageBox.Show("Fail to get " + property + " value\nReason : " + ex.Message);
			return string.Empty;
		}
	}
}
