using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using hmd_pctool_windows.Properties;

namespace hmd_pctool_windows;

public static class OutputXml
{
	public class Station
	{
		public string Timestamp;

		public string StationID;

		public string UserId;

		public string ErrorMsg;

		public string CrdTimestamp => Timestamp.Substring(0, 19).Replace("-", "").Replace(":", "")
			.Replace(" ", "");

		public Station()
			: this(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), Environment.MachineName)
		{
		}

		public Station(string userId)
			: this(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), Environment.MachineName, userId)
		{
		}

		public Station(string timestamp, string stationId)
		{
			Timestamp = timestamp;
			StationID = stationId;
		}

		public Station(string timestamp, string stationId, string userId)
		{
			Timestamp = timestamp;
			StationID = stationId;
			UserId = userId;
		}
	}

	public class XmlDevice
	{
		public string DataCable;

		public string OEM;

		public string Model;

		public string IMEI;

		public string Software;

		public string FlashResult;

		public string DataRemovalResult;

		public string EraseFrpResult;

		public string OSPlatform;

		public string OSVersion;

		public string BasebandVersion;

		public string Outcome;

		public string AntiTheftStatus;
	}

	public class FlashStatus
	{
		[XmlElement(ElementName = "FlashStation")]
		public Station FlashStation;

		[XmlElement(ElementName = "Device")]
		public XmlDevice Device;

		public bool ToVzwXmlFile(string path)
		{
			path = (string.IsNullOrEmpty(path) ? OutPath : path);
			string text = "NA";
			if (Device != null)
			{
				text = Device.IMEI;
			}
			string filename = text + "_" + FlashStation.Timestamp.Replace("-", "").Replace(":", "").Replace(".", "")
				.Replace(" ", "") + ".XML";
			return XmlUtility.SaveToXmlFile(path, filename, XmlUtility.Serialize(this));
		}

		public bool ToCdrXmlFile(string path)
		{
			path = (string.IsNullOrEmpty(path) ? OutPath : path);
			string text = "NA";
			string text2 = "NA";
			if (Device != null)
			{
				text = Device.IMEI;
				text2 = Device.OEM;
			}
			string filename = text2 + "_" + text + "_" + FlashStation.CrdTimestamp + ".XML";
			return XmlUtility.SaveToXmlFile(path, filename, getCrdXmlString(this));
		}

		private string getCrdXmlString(FlashStatus flashStatus)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
			stringBuilder.AppendLine("<REFLASH>");
			stringBuilder.AppendLine("<UNIT>");
			stringBuilder.AppendLine("<ESN>" + flashStatus.Device.IMEI + "</ESN>");
			stringBuilder.AppendLine("<TIMESTAMP>" + flashStatus.FlashStation.CrdTimestamp + "</TIMESTAMP>");
			stringBuilder.AppendLine("<RESULT>" + flashStatus.Device.FlashResult + "</RESULT>");
			stringBuilder.AppendLine("<SOFTWARE_NAME>HMD DeviceKit</SOFTWARE_NAME>");
			stringBuilder.AppendLine("<SOFTWARE_VER>" + flashStatus.Device.Software + "</SOFTWARE_VER>");
			stringBuilder.AppendLine("<OEM_NAME>" + flashStatus.Device.OEM + "</OEM_NAME>");
			stringBuilder.AppendLine("</UNIT>");
			stringBuilder.AppendLine("</REFLASH>");
			return stringBuilder.ToString();
		}
	}

	public class Unlockresult
	{
		[XmlElement(ElementName = "UnlockStation")]
		public Station UnlockStation;

		[XmlElement(ElementName = "Device")]
		public XmlDevice Device;

		public bool ToXmlFile(UnlockLogMode mode)
		{
			if (UnlockStation == null)
			{
				return false;
			}
			string text = "NA";
			if (Device != null)
			{
				text = Device.IMEI;
			}
			string path = ((mode == UnlockLogMode.Unlock) ? UnlockLogPath : InspectLogPath);
			string filename = text + "_" + UnlockStation.Timestamp.Replace("-", "").Replace(":", "").Replace(".", "")
				.Replace(" ", "") + ".XML";
			return XmlUtility.SaveToXmlFile(path, filename, XmlUtility.Serialize(this));
		}
	}

	private static readonly string OutRootPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

	public static string OutPath
	{
		get
		{
			Settings.Default.Reload();
			if (string.IsNullOrEmpty(Settings.Default.logPath))
			{
				Settings.Default.logPath = Path.Combine(OutRootPath, "HmdDeviceKit");
				Settings.Default.Save();
			}
			return Settings.Default.logPath;
		}
	}

	public static string UnlockLogPath => Path.Combine(OutPath, "Unlock");

	public static string InspectLogPath => Path.Combine(OutPath, "Inspect");
}
