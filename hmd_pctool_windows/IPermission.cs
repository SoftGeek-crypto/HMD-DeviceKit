using System;

namespace hmd_pctool_windows;

public interface IPermission
{
	void RequestPermission(CommandType permissionType);

	void RequestPermissionAsync(CommandType permissionType, Delegate eventHandler);

	RrltUserMode GetRrltUserMode();
}
