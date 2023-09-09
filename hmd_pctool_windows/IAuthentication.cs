using System.Threading.Tasks;

namespace hmd_pctool_windows;

public interface IAuthentication
{
	ServerResponse GetChiperResponse(HmdPermissionType pType, string serialNo, string cipherNounce, string version);

	ServerResponse GetUnlockSign(string token, string cipherNounce, AntiTheftStatus status);

	Task<RrltUserMode> GetRrltUserMode();
}
