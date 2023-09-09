namespace hmd_pctool_windows;

internal class AuthenticationFactory
{
	public static IAuthentication GetAuthenticationHandler(Device device = null)
	{
		if (Program.isOfflineVersion())
		{
			return new OfflineAuthenticationHandler();
		}
		return new HmdAuthenticationHandler();
	}
}
