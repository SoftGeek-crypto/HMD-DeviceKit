using System;
using Newtonsoft.Json;

namespace hmd_pctool_windows;

public class DeviceInfo
{
	[JsonProperty("SKUID")]
	public string SKUID;

	[JsonProperty("Product")]
	public string Product;

	[JsonProperty("Version")]
	public string Version;

	[JsonProperty("ProductTAcode")]
	public string ProductTAcode;

	[JsonProperty("ProductModel")]
	public string ProductModel;

	[JsonProperty("MNC")]
	public string MNC;

	[JsonProperty("MCC")]
	public string MCC;

	[JsonProperty("IMEI")]
	public string IMEI;

	[JsonProperty("SU")]
	public string SU;

	[JsonProperty("DataCable")]
	public string DataCable;

	[JsonProperty("OEM")]
	public string OEM;

	[JsonProperty("OSPlatform")]
	public string OSPlatform;

	[JsonProperty("OSVersion")]
	public string OSVersion;

	[JsonProperty("BasebandVersion")]
	public string BasebandVersion;

	[JsonProperty("AntiTheftStatus")]
	public string AntiTheftStatus = hmd_pctool_windows.AntiTheftStatus.Enabled.ToString();

	public DateTime CreateTime;
}
