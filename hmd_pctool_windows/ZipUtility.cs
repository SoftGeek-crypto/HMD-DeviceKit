using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using Newtonsoft.Json;

namespace hmd_pctool_windows;

public static class ZipUtility
{
	public enum ZipError
	{
		NoError,
		FileNotExist,
		NoEnoughMemory,
		NoEnoughStorage,
		ErrorWithException,
		ErrorWithOutDefine
	}

	public delegate void UpdateUnzipProgress(long unzipSize, long totalSize);

	private class CheckItem
	{
		public string zipPath;

		public bool isValid;
	}

	private static readonly bool IsDebug = false;

	private static readonly string Tag = "ZipUtility";

	private static readonly ulong unzipMinimumMemoryRequire = 100uL;

	private static readonly object lockZip = new object();

	private static readonly object lockRequest = new object();

	private static ulong lockStorageSize = 0uL;

	public static ulong LockStorageSize
	{
		get
		{
			lock (lockZip)
			{
				return lockStorageSize;
			}
		}
	}

	public static ZipError UnzipFile(Device device, string zipFile, string targetPath, bool isCleanTarget)
	{
		if (!File.Exists(zipFile))
		{
			LogUtility.E(Tag, "File(" + zipFile + ") not found.");
			return ZipError.FileNotExist;
		}
		long totalFilesSize = GetTotalFilesSize(zipFile);
		bool flag = true;
		try
		{
			if (!RequestStorageSpace(device, targetPath, (ulong)totalFilesSize))
			{
				flag = false;
				LogUtility.E(Tag, "No enough storage spase for extract " + zipFile + ".");
				return ZipError.NoEnoughStorage;
			}
			if (IsDebug)
			{
				LogUtility.D(Tag, "UnzipFile " + zipFile + " to " + targetPath);
			}
			if (isCleanTarget && Directory.Exists(targetPath))
			{
				string text = (targetPath.EndsWith("\\") ? targetPath.Remove(targetPath.Length - 1, 1) : targetPath);
				text += ".tmp";
				Directory.Move(targetPath, text);
				Directory.Delete(text, recursive: true);
			}
			Directory.CreateDirectory(targetPath);
			if (IsDebug)
			{
				LogUtility.D(Tag, "Extract " + zipFile + " to " + targetPath);
			}
			ZipFile.ExtractToDirectory(zipFile, targetPath);
			return ZipError.NoError;
		}
		catch (Exception ex)
		{
			LogUtility.E(Tag, "Extract file error: " + ex.Message + "\n" + ex.StackTrace);
			return ZipError.ErrorWithException;
		}
		finally
		{
			if (flag)
			{
				UpdateLockStorageSize(isAdd: false, (ulong)totalFilesSize);
			}
		}
	}

	public static ZipError UnzipFile(Device device, string zipFile, string targetPath, UpdateUnzipProgress updateUnzipProgress, bool isCleanTarget)
	{
		if (!File.Exists(zipFile))
		{
			LogUtility.E(Tag, "File(" + zipFile + ") not found.");
			return ZipError.FileNotExist;
		}
		long totalFilesSize = GetTotalFilesSize(zipFile);
		bool flag = true;
		try
		{
			if (IsDebug)
			{
				LogUtility.D(Tag, "UnzipFile " + zipFile + " to " + targetPath);
			}
			if (isCleanTarget && Directory.Exists(targetPath))
			{
				string text = (targetPath.EndsWith("\\") ? targetPath.Remove(targetPath.Length - 1, 1) : targetPath);
				text += ".tmp";
				Directory.Move(targetPath, text);
				Directory.Delete(text, recursive: true);
			}
			Directory.CreateDirectory(targetPath);
			long num = 0L;
			if (!RequestStorageSpace(device, targetPath, (ulong)totalFilesSize))
			{
				flag = false;
				LogUtility.E(Tag, "No enough storage spcae for extract " + zipFile + ".");
				return ZipError.NoEnoughStorage;
			}
			ZipArchive val = ZipFile.OpenRead(zipFile);
			try
			{
				foreach (ZipArchiveEntry entry in val.Entries)
				{
					if (entry.FullName.Contains("/"))
					{
						continue;
					}
					string text2 = Path.Combine(targetPath, entry.Name);
					int num2 = 4096;
					using (Stream stream2 = new FileStream(text2, FileMode.Create, FileAccess.Write, FileShare.None, num2, useAsync: false))
					{
						using Stream stream = entry.Open();
						if (IsDebug)
						{
							LogUtility.D(Tag, "Extract " + zipFile + " to " + text2);
						}
						byte[] buffer = new byte[num2];
						long num3 = entry.Length;
						do
						{
							int num4 = stream.Read(buffer, 0, num2);
							num3 -= num4;
							stream2.Write(buffer, 0, num4);
							num += num4;
							updateUnzipProgress?.Invoke(num, totalFilesSize);
						}
						while (num3 > 0);
					}
					File.SetLastWriteTime(text2, entry.LastWriteTime.DateTime);
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
			return ZipError.NoError;
		}
		catch (Exception ex)
		{
			LogUtility.E(Tag, "Extract file error: " + ex.Message + "\n" + ex.StackTrace);
			return ZipError.ErrorWithException;
		}
		finally
		{
			if (flag)
			{
				UpdateLockStorageSize(isAdd: false, (ulong)totalFilesSize);
			}
		}
	}

	public static long GetTotalFilesSize(string zipFile)
	{
		long num = 0L;
		ZipArchive val = ZipFile.OpenRead(zipFile);
		try
		{
			foreach (ZipArchiveEntry entry in val.Entries)
			{
				num += entry.Length;
			}
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
		return num;
	}

	public static T ReadJsonInZip<T>(string zipPath, string targetFile, string sn)
	{
		try
		{
			Thread thread = new Thread(CheckZip);
			CheckItem checkItem = new CheckItem
			{
				zipPath = zipPath
			};
			thread.Start(checkItem);
			thread.Join(1000);
			thread.Abort();
			if (!checkItem.isValid)
			{
				return default(T);
			}
			ZipArchive val = ZipFile.OpenRead(zipPath);
			try
			{
				foreach (ZipArchiveEntry entry in val.Entries)
				{
					if (entry.Name.Equals(targetFile))
					{
						string text = sn + "-" + entry.Name;
						entry.ExtractToFile(Path.Combine(Path.GetTempPath(), text), overwrite: true);
						T result = JsonConvert.DeserializeObject<T>(File.ReadAllText(Path.GetTempPath() + text));
						File.Delete(Path.GetTempPath() + text);
						return result;
					}
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			LogUtility.E(Tag, "ReadJsonInZip error with exception: " + ex.Message + "\n" + ex.StackTrace);
		}
		return default(T);
	}

	public static List<string> ReadSignature(string zipPath, string targetFile, string sn)
	{
		try
		{
			Thread thread = new Thread(CheckZip);
			CheckItem checkItem = new CheckItem
			{
				zipPath = zipPath
			};
			thread.Start(checkItem);
			thread.Join(1000);
			thread.Abort();
			if (!checkItem.isValid)
			{
				return null;
			}
			ZipArchive val = ZipFile.OpenRead(zipPath);
			try
			{
				foreach (ZipArchiveEntry entry in val.Entries)
				{
					if (!entry.Name.Equals(targetFile))
					{
						continue;
					}
					string path = sn + "-" + entry.Name;
					entry.ExtractToFile(Path.Combine(Path.GetTempPath(), path), overwrite: true);
					List<string> list = new List<string>();
					foreach (string item in File.ReadLines(Path.Combine(Path.GetTempPath(), path)))
					{
						if (!item.Equals(""))
						{
							list.Add(item);
						}
					}
					return list;
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			LogUtility.E(Tag, "ReadSignature error with exception: " + ex.Message + "\n" + ex.StackTrace);
		}
		return null;
	}

	private static void CheckZip(object item)
	{
		try
		{
			ZipArchive val = ZipFile.OpenRead((item as CheckItem).zipPath);
			try
			{
				(item as CheckItem).isValid = true;
				return;
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		catch (Exception)
		{
		}
		(item as CheckItem).isValid = false;
	}

	public static string GetZipErrorMessage(ZipError ze)
	{
		return ze switch
		{
			ZipError.FileNotExist => "file not exist.", 
			ZipError.NoEnoughMemory => "no enough memory space.", 
			ZipError.NoEnoughStorage => "no enough storage space.", 
			_ => "The error can arise due to incorrect or corrupted ROM package.\nPlease check whether the ROM package is for deviceKit.\nIf so, please redownload the ROM package and try again.", 
		};
	}

	private static void UpdateLockStorageSize(bool isAdd, ulong size)
	{
		lock (lockZip)
		{
			if (isAdd)
			{
				lockStorageSize += size;
			}
			else
			{
				lockStorageSize -= size;
			}
		}
	}

	private static bool RequestStorageSpace(Device device, string targetPath, ulong requestSpace)
	{
		if (LockStorageSize != 0 && !ComputerUtility.IsPathDriveAvailableForSizeByte(targetPath, LockStorageSize - requestSpace))
		{
			return false;
		}
		ulong sizeInByte = LockStorageSize + requestSpace;
		if (ComputerUtility.IsPathDriveAvailableForSizeByte(targetPath, sizeInByte))
		{
			UpdateLockStorageSize(isAdd: true, requestSpace);
			return true;
		}
		int num = 900000;
		while (true)
		{
			if (device == null && num <= 0)
			{
				if (num <= 0)
				{
					return false;
				}
			}
			else if (device.IsWorkerCancel)
			{
				return false;
			}
			if (ComputerUtility.IsPathDriveAvailableForSizeByte(targetPath, sizeInByte))
			{
				break;
			}
			Thread.Sleep(30000);
			num -= 30000;
		}
		UpdateLockStorageSize(isAdd: true, requestSpace);
		return true;
	}
}
