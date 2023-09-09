using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Extensibility;

namespace hmd_pctool_windows;

internal class CustomWebUi : ICustomWebUi
{
	public Task<Uri> AcquireAuthorizationCodeAsync(Uri authorizationUri, Uri redirectUri)
	{
		Console.WriteLine("please use browser to open " + authorizationUri.AbsoluteUri);
		Console.WriteLine("Then get the post form response from browser and enter here (it should looks like 'code=xxxx&state=xxxx&session_state=xxxx': ");
		Console.SetIn(new StreamReader(Console.OpenStandardInput(), Console.InputEncoding, detectEncodingFromByteOrderMarks: false, 1024));
		string ret = Console.ReadLine();
		return Task.Run(async () => new Uri(redirectUri?.ToString() + "?" + ret));
	}
}
