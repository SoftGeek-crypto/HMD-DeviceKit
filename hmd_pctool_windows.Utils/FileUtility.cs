using System.IO;

namespace hmd_pctool_windows.Utils;

internal class FileUtility
{
	public static bool IsFileExists(string filepath)
	{
		if (File.Exists(filepath))
		{
			return true;
		}
		return false;
	}
}
