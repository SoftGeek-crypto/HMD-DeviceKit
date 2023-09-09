using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace hmd_pctool_windows;

public class DigestUtility
{
	public static byte[] GetFileMD5Bytes(string filePath)
	{
		if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
		{
			return null;
		}
		using FileStream inputStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
		using HashAlgorithm hashAlgorithm = MD5.Create();
		return hashAlgorithm.ComputeHash(inputStream);
	}

	public static string GetFileMD5HashString(string filePath)
	{
		if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
		{
			return null;
		}
		return GetHashString(GetFileMD5Bytes(filePath));
	}

	public static string GetFileMD5Base64String(string filePath)
	{
		if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
		{
			return null;
		}
		return GetBase64String(GetFileMD5Bytes(filePath));
	}

	public static byte[] GetFileSHA1Bytes(string filePath)
	{
		if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
		{
			return null;
		}
		using FileStream inputStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
		using HashAlgorithm hashAlgorithm = new SHA1Managed();
		return hashAlgorithm.ComputeHash(inputStream);
	}

	public static string GetFileSHA1HashString(string filePath)
	{
		if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
		{
			return null;
		}
		return GetHashString(GetFileSHA1Bytes(filePath));
	}

	public static string GetFileSHA1Base64String(string filePath)
	{
		if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
		{
			return null;
		}
		return GetBase64String(GetFileSHA1Bytes(filePath));
	}

	public static byte[] GetFileSHA256Bytes(string filePath)
	{
		if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
		{
			return null;
		}
		using FileStream inputStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
		using HashAlgorithm hashAlgorithm = new SHA256CryptoServiceProvider();
		return hashAlgorithm.ComputeHash(inputStream);
	}

	public static string GetFileSHA256HashString(string filePath)
	{
		if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
		{
			return null;
		}
		return GetHashString(GetFileSHA256Bytes(filePath));
	}

	public static string GetFileSHA256Base64String(string filePath)
	{
		if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
		{
			return null;
		}
		return GetBase64String(GetFileSHA256Bytes(filePath));
	}

	public static byte[] GetFileSHA512Bytes(string filePath)
	{
		if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
		{
			return null;
		}
		using FileStream inputStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
		using HashAlgorithm hashAlgorithm = new SHA512CryptoServiceProvider();
		return hashAlgorithm.ComputeHash(inputStream);
	}

	public static string GetFileSHA512HashString(string filePath)
	{
		if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
		{
			return null;
		}
		return GetHashString(GetFileSHA512Bytes(filePath));
	}

	public static string GetFileSHA512Base64String(string filePath)
	{
		if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
		{
			return null;
		}
		return GetBase64String(GetFileSHA512Bytes(filePath));
	}

	public static string GetHashString(byte[] hashByte)
	{
		if (hashByte == null || hashByte.Length == 0)
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (byte b in hashByte)
		{
			stringBuilder.Append(b.ToString("X2"));
		}
		return stringBuilder.ToString();
	}

	public static string GetBase64String(byte[] hashByte)
	{
		if (hashByte == null || hashByte.Length == 0)
		{
			return null;
		}
		return Convert.ToBase64String(hashByte);
	}
}
