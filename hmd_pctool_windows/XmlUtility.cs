using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace hmd_pctool_windows;

public class XmlUtility
{
	private static readonly bool isDebug;

	public static string Serialize(object xmlObject)
	{
		if (xmlObject == null)
		{
			return null;
		}
		XmlSerializer xmlSerializer = new XmlSerializer(xmlObject.GetType());
		StringBuilder stringBuilder = new StringBuilder();
		StringWriter textWriter = new StringWriter(stringBuilder);
		xmlSerializer.Serialize(textWriter, xmlObject);
		return stringBuilder.ToString();
	}

	public static T Deserialize<T>(string xmlString)
	{
		if (string.IsNullOrEmpty(xmlString))
		{
			return default(T);
		}
		XmlDocument xmlDocument = new XmlDocument();
		try
		{
			xmlDocument.LoadXml(xmlString);
			XmlNodeReader xmlReader = new XmlNodeReader(xmlDocument.DocumentElement);
			XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
			object obj = xmlSerializer.Deserialize(xmlReader);
			return (T)obj;
		}
		catch
		{
			return default(T);
		}
	}

	public static bool SaveToXmlFile(string path, string filename, string xmlString)
	{
		if (path == null || xmlString == null)
		{
			return false;
		}
		if (!Directory.Exists(path))
		{
			Directory.CreateDirectory(path);
		}
		try
		{
			using (StreamWriter streamWriter = new StreamWriter(Path.Combine(path, filename)))
			{
				streamWriter.WriteLine(xmlString);
				streamWriter.Flush();
			}
			if (isDebug)
			{
				LogUtility.D("XmlUtility", "Export xml to " + path + " success.");
			}
			return true;
		}
		catch (Exception ex)
		{
			LogUtility.E("XmlUtility", "Export xml to " + path + " fail: " + ex.Message + "\n" + ex.StackTrace);
		}
		return false;
	}
}
