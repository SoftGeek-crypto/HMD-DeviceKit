using System;
using System.Threading.Tasks;

namespace hmd_pctool_windows;

internal class HmdAuthenticationHandler : IAuthentication
{
	public ServerResponse GetChiperResponse(HmdPermissionType pType, string serialNo, string cipherNounce, string version)
	{
		return AzureNativeClient.Instance.GetChiperResponse(cipherNounce, serialNo, pType, version);
	}

	public ServerResponse GetUnlockSign(string token, string version, AntiTheftStatus status)
	{
		return AzureNativeClient.Instance.GetUnlockSign(token, version, status);
	}

	public async Task<RrltUserMode> GetRrltUserMode()
	{
		ServerResponse response = await AzureNativeClient.Instance.GetUserMode();
		if (response.IsSuccessed)
		{
			Enum.TryParse<RrltUserMode>(response.Message, out var mode);
			return mode;
		}
		return RrltUserMode.None;
	}
}
