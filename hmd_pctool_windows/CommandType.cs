namespace hmd_pctool_windows;

public enum CommandType
{
	NoRequired,
	Flash,
	RebootEdl,
	ReadItem,
	WriteItem,
	PhoneEdit,
	SimLock,
	SimUnlock,
	OtaUpdate,
	UnlockFrp,
	FactoryResets,
	StartPhoneEdit,
	StartSimlock,
	StopPhoneEditSimlock,
	FrpErase,
	GetSku,
	LockBootloader,
	BootToSystem
}
