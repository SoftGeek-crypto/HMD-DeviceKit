using System.IO;
using Microsoft.VisualBasic.Devices;

namespace hmd_pctool_windows;

public class ComputerUtility
{
	private static readonly string tag = "ComputerUtility";

	private static readonly double reservePercentage = 0.1;

	private static readonly long flashSplitSize = 500L;

	private static readonly long defaultRequestMemoryForFlash = 100L;

	public static bool IsMemoryAvailableForSizeGB(ulong sizeInGagaByte)
	{
		return IsMemoryAvailableForSizeMB(sizeInGagaByte * 1024);
	}

	public static bool IsMemoryAvailableForFlash(long requestMemoryByte)
	{
		if (requestMemoryByte == -1)
		{
			return IsMemoryAvailableForSizeMB((ulong)defaultRequestMemoryForFlash);
		}
		return IsMemoryAvailableForSizeB((ulong)requestMemoryByte);
	}

	public static bool IsMemoryAvailableForSizeMB(ulong sizeInMagaByte)
	{
		return IsMemoryAvailableForSizeKB(sizeInMagaByte * 1024);
	}

	public static bool IsMemoryAvailableForSizeKB(ulong sizeInKiloByte)
	{
		return IsMemoryAvailableForSizeB(sizeInKiloByte * 1024);
	}

	public static bool IsMemoryAvailableForSizeB(ulong sizeInByte)
	{
		ComputerInfo computerInfo = new ComputerInfo();
		ulong availablePhysicalMemory = computerInfo.AvailablePhysicalMemory;
		if ((double)availablePhysicalMemory > (double)sizeInByte * (1.0 + reservePercentage))
		{
			return true;
		}
		return false;
	}

	public static bool IsMemoryAvailableForFlashFile(string imagePath)
	{
		return IsMemoryAvailableForFlashFile(imagePath, flashSplitSize);
	}

	public static bool IsMemoryAvailableForFlashFile(string imagePath, long requestMemory)
	{
		if (string.IsNullOrEmpty(imagePath))
		{
			return false;
		}
		FileInfo fileInfo = new FileInfo(imagePath);
		if (!fileInfo.Exists)
		{
			return false;
		}
		if (fileInfo.Length > requestMemory)
		{
			return IsMemoryAvailableForSizeMB((ulong)requestMemory);
		}
		return IsMemoryAvailableForSizeB((ulong)fileInfo.Length);
	}

	public static bool IsPathDriveAvailableForSizeGB(string path, ulong sizeInGegaByte)
	{
		return IsPathDriveAvailableForSizeMB(path, sizeInGegaByte * 1024);
	}

	public static bool IsPathDriveAvailableForSizeMB(string path, ulong sizeInMegaByte)
	{
		return IsPathDriveAvailableForSizeKB(path, sizeInMegaByte * 1024);
	}

	public static bool IsPathDriveAvailableForSizeKB(string path, ulong sizeInKiloByte)
	{
		return IsPathDriveAvailableForSizeByte(path, sizeInKiloByte * 1024);
	}

	public static bool IsPathDriveAvailableForSizeByte(string path, ulong sizeInByte)
	{
		if (string.IsNullOrEmpty(path))
		{
			return false;
		}
		long availableFreeSpaceFromPath = GetAvailableFreeSpaceFromPath(path);
		if (availableFreeSpaceFromPath < 0)
		{
			return false;
		}
		if ((double)availableFreeSpaceFromPath > (double)sizeInByte * (1.0 + reservePercentage))
		{
			return true;
		}
		return false;
	}

	public static long GetAvailableFreeSpaceFromPath(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return -1L;
		}
		return GetAvailableFreeSpaceFromDrive(Path.GetPathRoot(path));
	}

	public static long GetAvailableFreeSpaceFromDrive(string driveName)
	{
		DriveInfo[] drives = DriveInfo.GetDrives();
		foreach (DriveInfo driveInfo in drives)
		{
			if (driveInfo.IsReady && driveInfo.Name.Contains(driveName.ToUpper().Substring(0, 1)))
			{
				return driveInfo.AvailableFreeSpace;
			}
		}
		return -1L;
	}
}
