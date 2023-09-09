using System;
using System.Text;

namespace hmd_pctool_windows;

internal interface HmdOemInterface
{
	string GetDllName();

	bool Init();

	bool Deinit();

	bool isSupportSlot();

	bool EnterPhoneEditMode(string serialNo, Action<bool> callback);

	void StopEnterPhoneEditMode();

	bool Simlock(string filepath, StringBuilder key1, StringBuilder key2);

	bool SimUnlock(string key1, string key2);

	bool ReadPsn(StringBuilder sn);

	bool WritePsn(StringBuilder sn);

	bool ReadImei(StringBuilder imei);

	bool WriteImei(StringBuilder imei);

	bool WriteImei(StringBuilder imei, StringBuilder signature);

	bool ReadImei2(StringBuilder imei);

	bool WriteImei2(StringBuilder imei);

	bool WriteImei2(StringBuilder imei, StringBuilder signature);

	bool ReadMeid(StringBuilder meid);

	bool WriteMeid(StringBuilder meid);

	bool ReadWiFiAddr(StringBuilder addr);

	bool WriteWiFiAddr(StringBuilder addr);

	bool ReadBTAddr(StringBuilder addr);

	bool WriteBTAddr(StringBuilder addr);

	void SetCommandApi(CommandApi commandApi);
}
