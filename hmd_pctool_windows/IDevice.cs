namespace hmd_pctool_windows;

internal interface IDevice
{
	void GetAntiTheftStatus();

	void ReadItem(DeviceItemType type);

	void WriteItem(DeviceItemType type, string value);

	void GetUnlockFrpResult();

	void Flash(string path);

	void OtaUpdate(string path);

	void UnlockFrp();

	void StartPhoneEdit();

	void FactoryReset();

	void StartSimLock();

	void SimLock(string filePath);

	void SimUnlock(UnLockKeys unLockKeys);

	void SaveUnlockFrpResult();
}
