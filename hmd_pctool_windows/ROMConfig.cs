using System.Collections.Generic;
using Newtonsoft.Json;

namespace hmd_pctool_windows;

public class ROMConfig
{
	public string ZipPath;

	public string BasePath;

	[JsonProperty("Device_ID")]
	public string DeviceID;

	[JsonProperty("SKU_ID")]
	public string SkuID;

	[JsonProperty("Rom_Version")]
	public string RomVersion;

	[JsonProperty("Anti_Rollback")]
	public int AntiRollback = -1;

	[JsonProperty("AntiRollback_HW")]
	public int AntiRollback_HW = -1;

	[JsonProperty("AntiRollback_vbmeta")]
	public int AntiRollback_vbmeta = -1;

	[JsonProperty("AntiRollback_vbmeta_system")]
	public long AntiRollback_vbmeta_system = -1L;

	[JsonProperty("Android_Version")]
	public string AndroidVersion;

	[JsonProperty("isOTA")]
	public bool isOTA = false;

	[JsonProperty("Rom_Path")]
	public string RomPath;

	[JsonProperty("Rom_Size")]
	public string RomSize;

	[JsonProperty("Sparse_Size")]
	public string SparseSize = null;

	[JsonProperty("SHA-256")]
	public string SHA256;

	[JsonProperty("Require_FDR")]
	public bool RequireFDR;

	[JsonProperty("Bootloader")]
	public string Bootloader;

	[JsonProperty("Aboot")]
	public string Aboot;

	[JsonProperty("Modem")]
	public string Modem;

	[JsonProperty("Radio")]
	public string Radio;

	[JsonProperty("System_Zip")]
	public string SystemZip;

	[JsonProperty("Requires")]
	public Dictionary<string, string> Requires;

	[JsonProperty("Pre_Partitions")]
	public Dictionary<string, string> PrePartitions;

	[JsonProperty("Required_Reboot_Partitions")]
	public Dictionary<string, string> RequiredRebootPartitions = null;

	[JsonProperty("Post_Partitions")]
	public Dictionary<string, string> PostPartitions;
}
