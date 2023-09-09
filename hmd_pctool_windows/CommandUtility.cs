using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace hmd_pctool_windows;

public class CommandUtility
{
	public enum TargetStatus
	{
		Ok,
		FileNotFound,
		FileNotIntegrity
	}

	public class ReturnString
	{
		public static readonly string Finished = "Finished.";

		public static readonly string Failed = "FAILED";

		public static readonly string Error = "error";

		public static readonly string UpdatePackage = "update package";

		public static readonly string Permission = "permission";

		public static readonly string Bootloader = "(bootloader)";

		public static readonly string Okay = "OKAY";

		public static readonly string UnknownCommand = "unknown command";

		public static readonly string Adb = "adb";
	}

	private static readonly string tag = "CommandUtility";

	private static readonly bool isDebugMode = false;

	private static readonly bool isDumpSha1 = false;

	private static readonly string exePath = Path.Combine("/", Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase.Remove(0, "file:\\\\\\".Length)));

	private static readonly string[] WINDOWS_INFO = new string[5]
	{
		"fastboot.exe",
		"CE64B4D869F8FD618DE2858176E1B0E98AA64143",
		"adb.exe",
		"FB3045C7B380097FBF0F65FDF97D59B30E5F160B",
		Path.Combine("/", Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase.Remove(0, "file:\\\\\\".Length)))
	};

	private static readonly string[] MAC_INFO = new string[5]
	{
		"fastboot",
		"CF210D807348D357A1F1A4751275883B5F39BAD2",
		"adb",
		"",
		Path.Combine("/", Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase.Remove(0, "file:\\\\\\".Length)))
	};

	private static readonly string[] LINUX_INFO = new string[5] { "fastboot", "", "adb", "", "/opt/hmd/bin/" };

	private static readonly string[] OS_INFO = (Program.isWindows() ? WINDOWS_INFO : (Program.isMacOS() ? MAC_INFO : LINUX_INFO));

	public static readonly string DefaultFastboot = Path.Combine(OS_INFO[4], OS_INFO[0]);

	private static readonly string[] fastbootSha1Array = new string[1] { OS_INFO[1] };

	public static readonly string DefaultAdb = Path.Combine(OS_INFO[4], OS_INFO[2]);

	private static readonly string[] adbSha1Array = new string[1] { OS_INFO[3] };

	private static TargetStatus CheckTargetStatus(string targetPath, string[] sha1Array)
	{
		if (sha1Array[0].Equals(""))
		{
			return TargetStatus.Ok;
		}
		if (File.Exists(targetPath))
		{
			string fileSHA1HashString = DigestUtility.GetFileSHA1HashString(targetPath);
			if (isDumpSha1 || Program.GlobalDebugFlag)
			{
				foreach (string text in sha1Array)
				{
				}
			}
			if (isDebugMode)
			{
				return TargetStatus.Ok;
			}
			foreach (string text2 in sha1Array)
			{
				if (text2.Equals(fileSHA1HashString.ToUpper()))
				{
					return TargetStatus.Ok;
				}
			}
			return TargetStatus.FileNotIntegrity;
		}
		LogUtility.E(tag, "Can not found " + targetPath + ".");
		return TargetStatus.FileNotFound;
	}

	public static TargetStatus CheckAdbStatus(string adbPath)
	{
		return CheckTargetStatus(adbPath, adbSha1Array);
	}

	public static TargetStatus CheckFastbootStatus(string fastbootPath)
	{
		return CheckTargetStatus(fastbootPath, fastbootSha1Array);
	}

	public static bool IsOK(string stdout)
	{
		if (stdout.Contains(ReturnString.Okay))
		{
			return true;
		}
		return false;
	}

	public static string GetOemValue(string stdout)
	{
		return GetValueByKey(stdout, "=");
	}

	public static string GetBootloaderValue(string stdout)
	{
		if (stdout.IndexOf(ReturnString.Finished, StringComparison.OrdinalIgnoreCase) >= 0)
		{
			try
			{
				int num;
				if ((num = stdout.LastIndexOf(ReturnString.Bootloader)) >= 0)
				{
					string text = stdout.Substring(num + ReturnString.Bootloader.Length).Replace("\n\n", "\r");
					int length = text.IndexOf("\n");
					return text.Substring(0, length).Trim();
				}
			}
			catch (Exception ex) when (ex is ArgumentException)
			{
			}
		}
		return CommandApi.ErrorCode.OperationFail.ToString();
	}

	public static string GetBootloaderJson(string stdout)
	{
		if (stdout.IndexOf(ReturnString.Finished, StringComparison.OrdinalIgnoreCase) >= 0)
		{
			try
			{
				StringBuilder stringBuilder = new StringBuilder();
				string[] array = stdout.Replace("\r\n", "\n").Split('\n');
				foreach (string text in array)
				{
					if (text.Contains(ReturnString.Bootloader))
					{
						stringBuilder.AppendLine(text.Replace(ReturnString.Bootloader, "").Trim());
					}
				}
				if (stringBuilder.Length > 0)
				{
					return stringBuilder.ToString();
				}
				return string.Empty;
			}
			catch (Exception ex) when (ex is ArgumentException)
			{
			}
		}
		return CommandApi.ErrorCode.OperationFail.ToString();
	}

	public static string GetVarValue(string stdout)
	{
		return GetValueByKey(stdout, ":");
	}

	private static string GetValueByKey(string stdout, string key)
	{
		if (stdout.IndexOf(ReturnString.Finished, StringComparison.OrdinalIgnoreCase) >= 0)
		{
			try
			{
				int num = stdout.IndexOf(key);
				string text;
				if (num > 0)
				{
					text = stdout.Substring(num + 1).Replace("\r\n", "\n");
				}
				else
				{
					stdout = stdout.Replace(ReturnString.Bootloader, string.Empty);
					text = stdout.Replace("\r\n", "\n");
				}
				int num2 = text.IndexOf("\n");
				return (num2 <= 0) ? text.Trim() : text.Substring(0, num2).Trim();
			}
			catch (Exception ex)
			{
				if (!(ex is ArgumentException))
				{
				}
			}
		}
		return CommandApi.ErrorCode.OperationFail.ToString();
	}

	public static string GetCommand(params string[] args)
	{
		int num = args.Length;
		string text = string.Empty;
		for (int i = 0; i < num; i++)
		{
			if (i > 0)
			{
				text += " ";
			}
			text += args[i];
		}
		return text;
	}
}
