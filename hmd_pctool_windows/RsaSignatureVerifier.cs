using System;
using System.IO;
using System.Security.Cryptography;

namespace hmd_pctool_windows;

internal class RsaSignatureVerifier : IDisposable
{
	private readonly RSA _rsa;

	public RsaSignatureVerifier(string pubKey)
	{
		_rsa = RSA.Create();
		_rsa = CreateRsaProviderFromPublicKey(pubKey);
	}

	public bool VerifySign(string fileToVerifyPath, string fileSignature)
	{
		using FileStream data = new FileStream(fileToVerifyPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		byte[] signature = Convert.FromBase64String(fileSignature);
		return _rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
	}

	public void Dispose()
	{
		_rsa.Dispose();
	}

	private RSACryptoServiceProvider CreateRsaProviderFromPublicKey(string publicKeyString)
	{
		byte[] b = new byte[15]
		{
			48, 13, 6, 9, 42, 134, 72, 134, 247, 13,
			1, 1, 1, 5, 0
		};
		byte[] array = new byte[15];
		byte[] array2 = Convert.FromBase64String(publicKeyString);
		int num = array2.Length;
		using MemoryStream input = new MemoryStream(array2);
		using BinaryReader binaryReader = new BinaryReader(input);
		byte b2 = 0;
		ushort num2 = 0;
		switch (binaryReader.ReadUInt16())
		{
		case 33072:
			binaryReader.ReadByte();
			break;
		case 33328:
			binaryReader.ReadInt16();
			break;
		default:
			return null;
		}
		array = binaryReader.ReadBytes(15);
		if (!CompareBytearrays(array, b))
		{
			return null;
		}
		switch (binaryReader.ReadUInt16())
		{
		case 33027:
			binaryReader.ReadByte();
			break;
		case 33283:
			binaryReader.ReadInt16();
			break;
		default:
			return null;
		}
		if (binaryReader.ReadByte() != 0)
		{
			return null;
		}
		switch (binaryReader.ReadUInt16())
		{
		case 33072:
			binaryReader.ReadByte();
			break;
		case 33328:
			binaryReader.ReadInt16();
			break;
		default:
			return null;
		}
		num2 = binaryReader.ReadUInt16();
		byte b3 = 0;
		byte b4 = 0;
		switch (num2)
		{
		case 33026:
			b3 = binaryReader.ReadByte();
			break;
		case 33282:
			b4 = binaryReader.ReadByte();
			b3 = binaryReader.ReadByte();
			break;
		default:
			return null;
		}
		byte[] value = new byte[4] { b3, b4, 0, 0 };
		int num3 = BitConverter.ToInt32(value, 0);
		if (binaryReader.PeekChar() == 0)
		{
			binaryReader.ReadByte();
			num3--;
		}
		byte[] modulus = binaryReader.ReadBytes(num3);
		if (binaryReader.ReadByte() != 2)
		{
			return null;
		}
		int count = binaryReader.ReadByte();
		byte[] exponent = binaryReader.ReadBytes(count);
		RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
		RSAParameters parameters = default(RSAParameters);
		parameters.Modulus = modulus;
		parameters.Exponent = exponent;
		rSACryptoServiceProvider.ImportParameters(parameters);
		return rSACryptoServiceProvider;
	}

	private bool CompareBytearrays(byte[] a, byte[] b)
	{
		if (a.Length != b.Length)
		{
			return false;
		}
		int num = 0;
		foreach (byte b2 in a)
		{
			if (b2 != b[num])
			{
				return false;
			}
			num++;
		}
		return true;
	}
}
