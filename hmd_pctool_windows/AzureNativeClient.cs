using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace hmd_pctool_windows;

internal class AzureNativeClient
{
	public string UserName = "";

	private HttpClient httpClient = new HttpClient();

	private AuthenticationContext authContext = null;

	private string aadInstance = "https://login.microsoftonline.com/{0}";

	private string tenant = "live.com";

	private string clientId = "320bada1-5e32-4b75-824f-e7825a88d51e";

	private Uri redirectUri = new Uri("https://hmdglobal.com/DeviceKitsAuthClient");

	private string authority;

	private string resourceId = "https://hmdglobal.com/a4bf6bfd-18fe-40cc-a1f0-04c5bf10404d";

	private string graphResourceId = "https://graph.microsoft.com/";

	private string deviceKitGroupId = "d8a84ed0-59dd-4510-aaaa-bb1187799886";

	private string baseAddress = "http://devicekitsprod.trafficmanager.net";

	private bool isLogin = true;

	public static AzureNativeClient Instance = new AzureNativeClient();

	public static string token { get; set; }

	public AzureNativeClient()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected O, but got Unknown
		authority = string.Format(CultureInfo.InvariantCulture, aadInstance, tenant);
		authContext = new AuthenticationContext(authority);
	}

	public ServerResponse GetChiperResponse(string nounce, string sn, HmdPermissionType type, string securityVersion)
	{
		Task<ServerResponse> longTermTask = Task.Run(async () => await getChiperResponse(nounce, sn, type, securityVersion));
		return GetServerResponse(30000, longTermTask);
	}

	public ServerResponse GetUnlockSign(string token, string version, AntiTheftStatus antiTheftStatus)
	{
		Task<ServerResponse> longTermTask = Task.Run(async () => await getUnlockSign(token, version, antiTheftStatus));
		return GetServerResponse(30000, longTermTask);
	}

	public async Task<ServerResponse> GetUserMode()
	{
		UriBuilder builder = new UriBuilder(baseAddress + "/api/devauth/GetUserMode");
		return await GetServerResponseAsync(builder);
	}

	public async Task<ServerResponse> IsValidUser()
	{
		UriBuilder builder = new UriBuilder(baseAddress + "/api/devauth/isValidUser");
		return await GetServerResponseAsync(builder);
	}

	public async Task<ServerResponse> CheckOTP(string otp)
	{
		UriBuilder builder = new UriBuilder(baseAddress + "/api/devauth")
		{
			Port = -1
		};
		NameValueCollection query = HttpUtility.ParseQueryString(builder.Query);
		query["otp"] = otp;
		builder.Query = query.ToString();
		return await GetServerResponseAsync(builder);
	}

	public async Task<ServerResponse> FlashLog(string sn, string flashfile, string logfile, string version, string status)
	{
		UriBuilder builder = new UriBuilder(baseAddress + "/api/devutil/flashlog")
		{
			Port = -1
		};
		NameValueCollection query = HttpUtility.ParseQueryString(builder.Query);
		query["psn"] = sn;
		query["flashFile"] = flashfile;
		query["logFile"] = logfile;
		query["version"] = version;
		query["status"] = status;
		builder.Query = query.ToString();
		return await GetServerResponseAsync(builder);
	}

	public async Task<ServerResponse> TestCaseLog(string sn, string model, string passed, string failed)
	{
		UriBuilder builder = new UriBuilder(baseAddress + "/api/devutil/testCaseLog")
		{
			Port = -1
		};
		NameValueCollection query = HttpUtility.ParseQueryString(builder.Query);
		query["sn"] = sn;
		query["model"] = model;
		query["passed"] = passed;
		query["failed"] = failed;
		builder.Query = query.ToString();
		return await GetServerResponseAsync(builder);
	}

	public async Task<ServerResponse> SendEmail()
	{
		ServerResponse ret = new ServerResponse();
		if (!isLogin)
		{
			ret.IsSuccessed = false;
			ret.FailReason = "Please login first";
		}
		else
		{
			try
			{
				AuthenticationResult result = await authContext.AcquireTokenSilentAsync(resourceId, clientId);
				httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
				UriBuilder builder = new UriBuilder(baseAddress + "/api/devauth/requestemail");
				builder.ToString();
				HttpResponseMessage response = await httpClient.GetAsync(builder.ToString());
				if (response.IsSuccessStatusCode)
				{
					ret = JsonConvert.DeserializeObject<ServerResponse>(JToken.Parse(await response.Content.ReadAsStringAsync()).ToString());
				}
				else
				{
					ret.IsSuccessed = false;
					ret.FailReason = response.ReasonPhrase;
				}
			}
			catch (Exception ex)
			{
				Exception e = ex;
				ret.IsSuccessed = false;
				LogUtility.E("122: AzClient: exception at send mail", "");
				ret.FailReason = e.Message;
			}
		}
		return ret;
	}

	private ServerResponse GetServerResponse(int timeOut, Task<ServerResponse> longTermTask)
	{
		ServerResponse serverResponse = new ServerResponse();
		TimeSpan timeout = TimeSpan.FromMilliseconds(timeOut);
		if (!longTermTask.Wait(timeout))
		{
			serverResponse.IsSuccessed = false;
			serverResponse.FailReason = "Server connection time out";
		}
		else
		{
			serverResponse = longTermTask.Result;
		}
		return serverResponse;
	}

	private async Task<ServerResponse> GetServerResponseAsync(UriBuilder builder)
	{
		ServerResponse ret = new ServerResponse();
		if (!isLogin)
		{
			ret.IsSuccessed = false;
			ret.FailReason = "Not a valid user";
		}
		else
		{
			AuthenticationResult result;
			try
			{
				result = await authContext.AcquireTokenSilentAsync(resourceId, clientId);
			}
			catch (AdalException)
			{
				ret.IsSuccessed = false;
				ret.FailReason = "Fail to acquire access token from server";
				LogUtility.E("166: AzClient: Fail to acquire access token from server", "");
				return ret;
			}
			try
			{
				httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
				token = result.AccessToken;
				HttpResponseMessage response = await httpClient.GetAsync(builder.ToString());
				if (response.IsSuccessStatusCode)
				{
					return JsonConvert.DeserializeObject<ServerResponse>(JToken.Parse(await response.Content.ReadAsStringAsync()).ToString());
				}
				ret.IsSuccessed = false;
				ret.FailReason = response.ReasonPhrase;
				LogUtility.E("184: AzClient: Fail Reason", response.ReasonPhrase);
			}
			catch (Exception ex)
			{
				ret.IsSuccessed = false;
				ret.FailReason = ex.Message;
				LogUtility.E("191: AzClient ", "Exception at get server response");
			}
		}
		return ret;
	}

	private async Task<ServerResponse> getChiperResponse(string nounce, string sn, HmdPermissionType type, string securityVersion)
	{
		UriBuilder builder = new UriBuilder(baseAddress + "/api/devauth")
		{
			Port = -1
		};
		NameValueCollection query = HttpUtility.ParseQueryString(builder.Query);
		query["nounce"] = nounce;
		query["sn"] = sn;
		int num = (int)type;
		query["type"] = num.ToString() ?? "";
		query["securityVersion"] = securityVersion;
		query["toolVersion"] = Assembly.GetExecutingAssembly().GetName().Version.ToString();
		builder.Query = query.ToString();
		return await GetServerResponseAsync(builder);
	}

	public async Task<ServerResponse> IsUpdateAvailable(string version)
	{
		UriBuilder builder = new UriBuilder(baseAddress + "/api/devutil/isupdateavailable")
		{
			Port = -1
		};
		NameValueCollection query = HttpUtility.ParseQueryString(builder.Query);
		query["version"] = version;
		builder.Query = query.ToString();
		return await GetServerResponseAsync(builder);
	}

	private async Task<ServerResponse> getUnlockSign(string token, string securityVersion, AntiTheftStatus status)
	{
		UriBuilder builder = new UriBuilder(baseAddress + "/api/devauth/getUnlockSign")
		{
			Port = -1
		};
		NameValueCollection query = HttpUtility.ParseQueryString(builder.Query);
		query["token"] = token;
		query["antiTheftStatus"] = status.ToString().ToLower();
		query["securityVersion"] = securityVersion;
		query["toolVersion"] = Assembly.GetExecutingAssembly().GetName().Version.ToString();
		builder.Query = query.ToString();
		return await GetServerResponseAsync(builder);
	}

	public async Task<bool> CheckAdGroup()
	{
		if (!isLogin)
		{
			return false;
		}
		try
		{
			AuthenticationResult result = await authContext.AcquireTokenSilentAsync(graphResourceId, clientId);
			httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
			httpClient.BaseAddress = new Uri("https://graph.microsoft.com/v1.0/");
			StringContent content = new StringContent("{ \"groupIds\":[ \"" + deviceKitGroupId + "\"] }", Encoding.UTF8, "application/json");
			HttpResponseMessage response = await httpClient.PostAsync("me/checkMemberGroups", (HttpContent)(object)content);
			if (response.IsSuccessStatusCode)
			{
				HttpContent responseContent = response.Content;
				try
				{
					return (await responseContent.ReadAsStringAsync()).Contains(deviceKitGroupId);
				}
				finally
				{
					((IDisposable)responseContent)?.Dispose();
				}
			}
			MessageBox.Show("Fail to check user group, reason: " + response.ReasonPhrase);
			return false;
		}
		catch (Exception e)
		{
			MessageBox.Show("Fail to check user group, reason: " + e.Message);
			return false;
		}
	}

	public async Task<string> Login(string account, string password)
	{
		try
		{
			AuthenticationResult result;
			if (account == null)
			{
				if (!Program.isConsole())
				{
					result = await authContext.AcquireTokenAsync(resourceId, clientId, redirectUri, new PlatformParameters(PromptBehavior.Always));
				}
				else
				{
					Console.WriteLine("start console login");
					result = await authContext.AcquireTokenAsync(resourceId, clientId, redirectUri, new PlatformParameters(PromptBehavior.Always, new CustomWebUi()));
				}
			}
			else
			{
				result = await authContext.AcquireTokenAsync(resourceId, clientId, new UserPasswordCredential(account, password));
			}
			isLogin = true;
			UserName = result.UserInfo.DisplayableId;
			return "OK";
		}
		catch (AdalException ex3)
		{
			AdalException ex2 = ex3;
			string message2 = "Fail ";
			if (ex2.ErrorCode == "access_denied")
			{
				message2 += ", Reason: access denied";
			}
			else
			{
				message2 = message2 + ", Reason: " + ex2.ErrorCode;
				if (ex2.InnerException != null)
				{
					message2 = message2 + "\nInner Exception : " + ex2.InnerException.Message;
				}
			}
			Console.WriteLine("Error " + message2);
			return message2;
		}
		catch (Exception ex4)
		{
			Exception ex = ex4;
			Console.WriteLine("Error " + ex.Message);
			return "Fail , Reason:" + ex.Message;
		}
	}

	internal async Task<ServerResponse> GetFileUrl(string file)
	{
		UriBuilder builder = new UriBuilder(baseAddress + "/api/devutil/downloadUrl")
		{
			Port = -1
		};
		NameValueCollection query = HttpUtility.ParseQueryString(builder.Query);
		query["appid"] = file;
		builder.Query = query.ToString();
		return await GetServerResponseAsync(builder);
	}

	internal async Task<ServerResponse> FqcAuthStart()
	{
		UriBuilder builder = new UriBuilder(baseAddress + "/api/devutil/fqcAuthStart")
		{
			Port = -1
		};
		NameValueCollection query = HttpUtility.ParseQueryString(builder.Query);
		builder.Query = query.ToString();
		return await GetServerResponseAsync(builder);
	}

	internal async Task<ServerResponse> logFqcOffline()
	{
		UriBuilder builder = new UriBuilder(baseAddress + "/api/devutil/fqcAuthStart?keyType=Offline")
		{
			Port = -1
		};
		NameValueCollection query = HttpUtility.ParseQueryString(builder.Query);
		builder.Query = query.ToString();
		return await GetServerResponseAsync(builder);
	}
}
