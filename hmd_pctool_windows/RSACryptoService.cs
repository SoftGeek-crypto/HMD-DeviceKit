using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Math;

namespace hmd_pctool_windows;

public class RSACryptoService
{
	private RSACryptoServiceProvider _privateKeyRsaProvider;

	private RSACryptoServiceProvider _publicKeyRsaProvider;

	public RSACryptoService(string privateKey, string publicKey = null)
	{
		if (!string.IsNullOrEmpty(privateKey))
		{
			_privateKeyRsaProvider = CreateRsaProviderFromPrivateKey(privateKey);
		}
		if (!string.IsNullOrEmpty(publicKey))
		{
			_publicKeyRsaProvider = CreateRsaProviderFromPublicKey(publicKey);
		}
	}

	public string Decrypt(string cipherText)
	{
		if (_privateKeyRsaProvider == null)
		{
			throw new Exception("_privateKeyRsaProvider is null");
		}
		return Encoding.UTF8.GetString(_privateKeyRsaProvider.Decrypt(Convert.FromBase64String(cipherText), fOAEP: false));
	}

	public string Sign(string text)
	{
		if (_privateKeyRsaProvider == null)
		{
			throw new Exception("_privateKeyRsaProvider is null");
		}
		return Convert.ToBase64String(_privateKeyRsaProvider.SignData(Encoding.UTF8.GetBytes(text), "SHA256"));
	}

	private string GetHexString(byte[] byteArray)
	{
		StringBuilder stringBuilder = new StringBuilder(byteArray.Length * 2);
		for (int i = 0; i < byteArray.Length; i++)
		{
			stringBuilder.Append($"{byteArray[i]:X2}");
		}
		int capacity = stringBuilder.Capacity;
		return stringBuilder.ToString();
	}

	private byte[] encodePKCS(byte[] Message)
	{
		if (Message.Length > 253)
		{
			throw new ArgumentException("Message too long.");
		}
		byte[] array = new byte[255];
		array[0] = 1;
		for (int i = 1; i < 255 - Message.Length - 1; i++)
		{
			array[i] = byte.MaxValue;
		}
		array[255 - Message.Length - 1] = 0;
		int num = 0;
		for (int j = 255 - Message.Length; j < 255; j++)
		{
			array[j] = Message[num];
			num++;
		}
		return array;
	}

	public string PrivateEncryption(string data)
	{
		try
		{
			RSAParameters rSAParameters = _privateKeyRsaProvider.ExportParameters(includePrivateParameters: true);
			byte[] bytes = Encoding.UTF8.GetBytes(data);
			BigInteger bigInteger = new BigInteger(encodePKCS(bytes));
			BigInteger bigInteger2 = bigInteger.ModPow(new BigInteger(1, rSAParameters.D), new BigInteger(1, rSAParameters.Modulus));
			return Convert.ToBase64String(bigInteger2.ToByteArrayUnsigned());
		}
		catch (Exception)
		{
			return string.Empty;
		}
	}

	public bool Verify(string text, string signature)
	{
		if (_publicKeyRsaProvider == null)
		{
			throw new Exception("_publicKeyRsaProvider is null");
		}
		return _publicKeyRsaProvider.VerifyData(Encoding.UTF8.GetBytes(text), "SHA256", Convert.FromBase64String(signature));
	}

	public string Encrypt(string text)
	{
		if (_publicKeyRsaProvider == null)
		{
			throw new Exception("_publicKeyRsaProvider is null");
		}
		return Convert.ToBase64String(_publicKeyRsaProvider.Encrypt(Encoding.UTF8.GetBytes(text), fOAEP: false));
	}

	private RSACryptoServiceProvider CreateRsaProviderFromPrivateKey(string privateKey)
	{
		byte[] buffer = Convert.FromBase64String(privateKey);
		RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
		RSAParameters parameters = default(RSAParameters);
		using (BinaryReader binaryReader = new BinaryReader(new MemoryStream(buffer)))
		{
			byte b = 0;
			ushort num = 0;
			switch (binaryReader.ReadUInt16())
			{
			case 33072:
				binaryReader.ReadByte();
				break;
			case 33328:
				binaryReader.ReadInt16();
				break;
			default:
				throw new Exception("Unexpected value read binr.ReadUInt16()");
			}
			num = binaryReader.ReadUInt16();
			if (num != 258)
			{
				throw new Exception("Unexpected version");
			}
			if (binaryReader.ReadByte() != 0)
			{
				throw new Exception("Unexpected value read binr.ReadByte()");
			}
			parameters.Modulus = binaryReader.ReadBytes(GetIntegerSize(binaryReader));
			parameters.Exponent = binaryReader.ReadBytes(GetIntegerSize(binaryReader));
			parameters.D = binaryReader.ReadBytes(GetIntegerSize(binaryReader));
			parameters.P = binaryReader.ReadBytes(GetIntegerSize(binaryReader));
			parameters.Q = binaryReader.ReadBytes(GetIntegerSize(binaryReader));
			parameters.DP = binaryReader.ReadBytes(GetIntegerSize(binaryReader));
			parameters.DQ = binaryReader.ReadBytes(GetIntegerSize(binaryReader));
			parameters.InverseQ = binaryReader.ReadBytes(GetIntegerSize(binaryReader));
		}
		rSACryptoServiceProvider.ImportParameters(parameters);
		return rSACryptoServiceProvider;
	}

	private int GetIntegerSize(BinaryReader binr)
	{
		byte b = 0;
		byte b2 = 0;
		byte b3 = 0;
		int num = 0;
		b = binr.ReadByte();
		if (b != 2)
		{
			return 0;
		}
		b = binr.ReadByte();
		switch (b)
		{
		case 129:
			num = binr.ReadByte();
			break;
		case 130:
		{
			b3 = binr.ReadByte();
			b2 = binr.ReadByte();
			byte[] value = new byte[4] { b2, b3, 0, 0 };
			num = BitConverter.ToInt32(value, 0);
			break;
		}
		default:
			num = b;
			break;
		}
		while (binr.ReadByte() == 0)
		{
			num--;
		}
		binr.BaseStream.Seek(-1L, SeekOrigin.Current);
		return num;
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
