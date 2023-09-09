using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace hmd_pctool_windows;

internal class HmdOemProvider
{
	public static bool RunScript(string script)
	{
		try
		{
			script = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\" + script;
			LogUtility.D("RunScript", "script path: " + script);
			if (!File.Exists(script))
			{
				LogUtility.D("RunScript", "No custom script to execute.");
				return false;
			}
			TextReader textReader = File.OpenText(script);
			string text;
			while ((text = textReader.ReadLine()) != null)
			{
				if (!text.StartsWith("#"))
				{
					Process process = new Process();
					process.StartInfo.FileName = CommandUtility.DefaultFastboot;
					process.StartInfo.Arguments = text;
					process.StartInfo.UseShellExecute = false;
					process.StartInfo.RedirectStandardOutput = true;
					process.StartInfo.CreateNoWindow = true;
					process.StartInfo.RedirectStandardError = true;
					LogUtility.D("RunScript", "Execute \"fastboot " + text + "\"");
					process.Start();
					string log = process.StandardError.ReadToEnd();
					LogUtility.D("RunScript", log);
					if (process.ExitCode != 0)
					{
						throw new Exception("command " + text + " failed");
					}
					Thread.Sleep(500);
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			LogUtility.E("RunScript", ex.ToString());
			return false;
		}
	}
}
