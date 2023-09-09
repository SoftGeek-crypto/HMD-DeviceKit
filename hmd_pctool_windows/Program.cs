using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using hmd_pctool_windows.Components;
using NLog;
using NLog.Targets;

namespace hmd_pctool_windows;

internal static class Program
{
	public static BorderlessForm loginAzureForm;

	public static BorderlessForm functionSelectForm;

	public static BorderlessForm MultiFirmwareUpdateForm;

	public static BorderlessForm detectDeviceForm;

	public static bool GlobalDebugFlag;

	public static BorderlessForm FrpUnlockForm;

	public static HmdOemInterface oemInterface;

	public static RrltUserMode UserMode;

	private static bool _isWindows;

	private static bool _isMacOS;

	private static bool _isLinux;

	private static bool _isBaseTests;

	private static bool _isExit;

	private static string _username;

	private static string _password;

	private static string _otp;

	private static string _flashimage;

	private static bool _isConsole;

	private static string _swskuid;

	private static string _script;

	private static string _device;

	private static bool _erasedata;

	private static bool _testharness;

	private static bool _isAllTests;

	private static void initPlatform()
	{
		string environmentVariable = Environment.GetEnvironmentVariable("windir");
		if (!string.IsNullOrEmpty(environmentVariable) && environmentVariable.Contains("\\") && Directory.Exists(environmentVariable))
		{
			_isWindows = true;
		}
		else if (File.Exists("/proc/sys/kernel/ostype"))
		{
			string text = File.ReadAllText("/proc/sys/kernel/ostype");
			if (text.StartsWith("Linux", StringComparison.OrdinalIgnoreCase))
			{
				_isLinux = true;
			}
			else
			{
				_isMacOS = true;
			}
		}
		else if (File.Exists("/System/Library/CoreServices/SystemVersion.plist"))
		{
			_isMacOS = true;
		}
		else
		{
			_isWindows = true;
		}
	}

	private static Task<int> RunScript()
	{
		return Task.Run(async delegate
		{
			Console.WriteLine("started");
			if (!(await AzureNativeClient.Instance.Login(_username, _password)).Equals("OK"))
			{
				Console.WriteLine("Authentication failed");
				return -1;
			}
			if (_otp != null)
			{
				Console.WriteLine("start otp " + _otp);
				if (!(await AzureNativeClient.Instance.CheckOTP(_otp)).IsSuccessed)
				{
					Console.WriteLine("OTP failed");
					return -1;
				}
			}
			Console.WriteLine("authenticated");
			CommandApi commandApi = new CommandApi("program");
			StringBuilder sb = new StringBuilder();
			CommandApi.ErrorCode ret = commandApi.GetDevices(sb);
			if (ret != 0 && ret != CommandApi.ErrorCode.NoReturn)
			{
				Console.WriteLine(ret.ToString());
				return 0;
			}
			if (!string.IsNullOrEmpty(sb.ToString()))
			{
				List<string> list = new List<string>();
				list.AddRange(sb.ToString().Split(','));
				foreach (string sn in list)
				{
					HmdDevice device = new HmdDevice(sn);
					if (device.RequestHmdPermission(HmdPermissionType.Flash) == 0 && (_otp == null || device.RequestHmdPermission(HmdPermissionType.Repair) == 0))
					{
						TextReader textReader = File.OpenText(_script);
						using Process process = new Process();
						while (true)
						{
							string text;
							string line = (text = textReader.ReadLine());
							if (text == null)
							{
								break;
							}
							if (!line.StartsWith("#"))
							{
								if (line.StartsWith("DKFLASH"))
								{
									device.FlashRomImage(line.Substring(8));
								}
								else
								{
									Console.WriteLine("run: fastboot " + line);
									process.StartInfo.FileName = CommandUtility.DefaultFastboot;
									process.StartInfo.Arguments = line;
									process.StartInfo.UseShellExecute = false;
									process.StartInfo.RedirectStandardOutput = true;
									process.StartInfo.CreateNoWindow = true;
									process.StartInfo.RedirectStandardError = true;
									process.Start();
									string fastbootret = process.StandardError.ReadToEnd();
									Console.WriteLine("run: " + fastbootret);
									if (process.ExitCode != 0)
									{
										return 0;
									}
									Thread.Sleep(500);
								}
							}
						}
					}
				}
			}
			return 0;
		});
	}

	private static Task<int> RunTestHarness()
	{
		return Task.Run(async delegate
		{
			Console.WriteLine("started");
			if (!(await AzureNativeClient.Instance.Login(_username, _password)).Equals("OK"))
			{
				Console.WriteLine("Authentication failed");
				return -1;
			}
			Console.WriteLine("authenticated");
			CommandApi commandApi = new CommandApi("program");
			StringBuilder sb = new StringBuilder();
			CommandApi.ErrorCode ret = commandApi.GetDevices(sb);
			if (ret != 0 && ret != CommandApi.ErrorCode.NoReturn)
			{
				Console.WriteLine(ret.ToString());
				return -1;
			}
			string homePath = ((Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX) ? Environment.GetEnvironmentVariable("HOME") : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%"));
			if (!string.IsNullOrEmpty(sb.ToString()))
			{
				List<string> list = new List<string>();
				list.AddRange(sb.ToString().Split(','));
				foreach (string sn in list)
				{
					HmdDevice device = new HmdDevice(sn);
					if (device.RequestHmdPermission(HmdPermissionType.Flash) == 0)
					{
						using Process process = new Process();
						string adbfile = homePath + "/.android/adbkey.pub";
						string text = File.ReadAllText(adbfile);
						string token = device.GetAutomationToken(text.Substring(0, 20));
						string path = Path.GetTempPath() + sn;
						Console.WriteLine("create temp under " + path + " " + text);
						if (Directory.Exists(path))
						{
							Directory.Delete(path, recursive: true);
						}
						Directory.CreateDirectory(path);
						Directory.CreateDirectory(path + "/custom");
						File.WriteAllText(path + "/custom/testharness_token", text.Substring(0, 20) + ";" + token + "\n" + text);
						process.StartInfo.FileName = CommandUtility.DefaultFastboot;
						process.StartInfo.Arguments = "-W " + path;
						process.StartInfo.UseShellExecute = false;
						process.StartInfo.RedirectStandardOutput = true;
						process.StartInfo.CreateNoWindow = true;
						process.StartInfo.RedirectStandardError = true;
						process.Start();
						string fastbootret = process.StandardError.ReadToEnd();
						process.WaitForExit();
						Console.WriteLine("run: " + fastbootret);
						if (process.ExitCode != 0)
						{
							process.Close();
							return 0;
						}
						Thread.Sleep(500);
						process.Close();
					}
				}
			}
			return 0;
		});
	}

	private static Task<int> FlashRom()
	{
		return Task.Run(async delegate
		{
			Console.WriteLine("started");
			if (!(await AzureNativeClient.Instance.Login(_username, _password)).Equals("OK"))
			{
				Console.WriteLine("Authentication failed");
				return -1;
			}
			if (_otp != null)
			{
				Console.WriteLine("start otp " + _otp);
				if (!(await AzureNativeClient.Instance.CheckOTP(_otp)).IsSuccessed)
				{
					Console.WriteLine("OTP failed");
					return -1;
				}
			}
			Console.WriteLine("authenticated");
			if (_isAllTests || _isBaseTests)
			{
				Console.WriteLine("Checking permission to perfrom tests..");
				ServerResponse modeRet = await AzureNativeClient.Instance.GetUserMode();
				if (modeRet.IsSuccessed)
				{
					Enum.TryParse<RrltUserMode>(modeRet.Message, out var mode);
					if (mode.Equals(RrltUserMode.Tester))
					{
						Console.WriteLine("Can run tests -> Authorized.");
					}
					else
					{
						Console.WriteLine("Can't run tests -> doesn't have required permission.");
						_isAllTests = false;
						_isBaseTests = false;
					}
				}
			}
			CommandApi commandApi = new CommandApi("program");
			StringBuilder sb = new StringBuilder();
			if (_device == null)
			{
				CommandApi.ErrorCode ret = commandApi.GetDevices(sb);
				if (ret != 0 && ret != CommandApi.ErrorCode.NoReturn)
				{
					Console.WriteLine(ret.ToString());
					return 0;
				}
				_device = sb.ToString();
			}
			if (!string.IsNullOrEmpty(_device))
			{
				List<string> list = new List<string>();
				list.AddRange(_device.Split(','));
				foreach (string sn in list)
				{
					HmdDevice device = new HmdDevice(sn);
					if (_isBaseTests || _isAllTests)
					{
						device.SetIsRunTests(_isBaseTests, _isAllTests);
					}
					if (_flashimage != null)
					{
						Console.WriteLine("flashing " + _flashimage);
						device.SetIsEraseUserData(_erasedata);
						device.SetIsIgnorePermissionDuringFlash(isIgnore: true);
						device.FlashRomImage(_flashimage);
					}
					if (_swskuid != null)
					{
						Console.WriteLine("set target sku " + _swskuid);
						device.UpdateCurrentSkuId();
						device.UpdateSWSKUID(_swskuid);
					}
				}
			}
			else
			{
				Console.WriteLine("Please connect the device and try again.");
			}
			Console.WriteLine("Hit enter to EXIT");
			Console.ReadLine();
			return 0;
		});
	}

	[STAThread]
	private static void Main(string[] args)
	{
		using Mutex mutex = new Mutex(initiallyOwned: false, "HMD_PC_TOOL");
		initPlatform();
		if (!mutex.WaitOne(0, exitContext: false))
		{
			MessageBox.Show("Tool is already running", "Error");
			return;
		}
		if (args.Length != 0 && _isWindows)
		{
			string arguments = string.Join(" ", args);
			ProcessStartInfo startInfo = new ProcessStartInfo("DevicekitConsole.exe")
			{
				WorkingDirectory = Application.StartupPath,
				Arguments = arguments
			};
			Process.Start(startInfo);
			Application.Exit();
		}
		GetArgs(args);
		if (_isExit)
		{
			return;
		}
		if (_flashimage != null || _swskuid != null)
		{
			Console.WriteLine("flash " + _flashimage);
			_isConsole = true;
			FlashRom().Wait();
			return;
		}
		if (_script != null)
		{
			Console.WriteLine("script " + _script);
			_isConsole = true;
			RunScript().Wait();
			return;
		}
		if (_testharness)
		{
			Console.WriteLine("start testharness");
			_isConsole = true;
			RunTestHarness().Wait();
			return;
		}
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(defaultValue: false);
		if (!isWindows())
		{
			loginAzureForm = new LoginForm();
		}
		else
		{
			loginAzureForm = new LoginAzureForm();
		}
		functionSelectForm = new FunctionSelectForm();
		if (isOfflineVersion())
		{
			Application.Run(functionSelectForm);
		}
		else
		{
			Application.Run(loginAzureForm);
		}
	}

	public static bool isOfflineVersion()
	{
		return false;
	}

	public static bool isFactoryVersion()
	{
		return true;
	}

	public static bool isOtpEnable()
	{
		return true;
	}

	public static bool isWindows()
	{
		return _isWindows;
	}

	public static bool isMacOS()
	{
		return _isMacOS;
	}

	public static bool isLinux()
	{
		return _isLinux;
	}

	public static bool isConsole()
	{
		return _isConsole;
	}

	public static bool isTestSuite()
	{
		return _isBaseTests;
	}

	public static void GetArgs(string[] args)
	{
		if (args == null || args.Length == 0)
		{
			return;
		}
		if (args.Length == 1 && args[0].Equals("--help", StringComparison.OrdinalIgnoreCase))
		{
			Console.WriteLine("Maximize Window for better view. \n");
			Console.WriteLine("--flash  -> To flash the device, connect the device in fastboot mode.");
			Console.WriteLine("         -> Must Parameters --user, --password, --flash=c:\\example_folder\\file_path\\ ");
			Console.WriteLine("         -> Optional parameters (1) --otp      - otp is must if you want to perfrom SKU id Edit and Erase Userdata.");
			Console.WriteLine("         -> Optional parameters (2) --swskuid  - To write SKUID of the device (OTP needed)");
			Console.WriteLine("         -> Optional parameters (3) --device   - To mention the device by its SN.");
			Console.WriteLine("         -> Optional parameters (4) --log      - To log the details in a text file to the path we mention\n");
			Console.WriteLine("         -> Optional tags (1) --debug    - To run the application as debug mode");
			Console.WriteLine("         -> Optional tags (3) --erasedata- To erase the userdata (OTP needed)");
			Console.WriteLine("         -> Optional tags (4) --alltests - To run all the automated tests along the flash function.");
			Console.WriteLine("         -> Optional tags (5) --basetests - To run the automated tests along the flash function.\n            Except 1. FRP erase 2. Erase Userdata 3. Lock bootloader.");
			Console.WriteLine("\nOptional parameters: Input should be given along with the tags");
			Console.WriteLine("Example: --otp=123455   --device=SN1234567890\n");
			Console.WriteLine("Optional tags      : Just the parameters is enough");
			Console.WriteLine("Example: --erasedata    --runtests");
			Console.WriteLine("\nFlash Example : \n         ->\"HMD DeviceKit.exe\" --flash=c:\\Nokia 1.4\\software.zip (Authentication should be done before flash)");
			Console.WriteLine("\nFlash Example : \n         ->\"HMD DeviceKit.exe\" --user=\"username@hmdglobal.com\" --password=\"key@sample123\" --flash=c:\\Nokia 1.4\\software.zip");
			Console.WriteLine("\nFlash with Erasedata Example : \n         ->\"HMD DeviceKit.exe\" --user=\"username@hmdglobal.com\" --password=\"key@sample123\" --flash=c:\\Nokia 1.4\\software.zip --erasedata");
			Console.WriteLine("\nFlash with SET SKU ID Example: \n         ->\"HMD DeviceKit.exe\" --user=\"username@hmdglobal.com\" --password=\"key@sample123\" --flash=c:\\Nokia 1.4\\software.zip --swskuid=\"600XX\"");
			Console.WriteLine("\nFlash with running all tests : \n         ->\"HMD DeviceKit.exe\" --user=\"username@hmdglobal.com\" --password=\"key@sample123\" --flash=c:\\Nokia 1.4\\software.zip --alltests");
			Console.WriteLine("\nFor Queries: devicekit.support@hmdglobal.com \n");
			Console.WriteLine("Hit Enter to Exit.");
			Console.ReadLine();
			_isExit = true;
		}
		Console.WriteLine("Type --help to get the details Ex: \"HMD DeviceKit.exe\" --help");
		foreach (string text in args)
		{
			if (text.StartsWith("--"))
			{
				if (text.Substring(2).Equals("debug"))
				{
					GlobalDebugFlag = true;
				}
				else if (text.Substring(2, 3).Equals("log"))
				{
					string text2 = text.Substring(6);
					FileTarget target = new FileTarget("dynamicLogFile")
					{
						FileName = text2,
						Layout = LogManager.Configuration.Variables["Layout"]
					};
					LogManager.Configuration.AddTarget(target);
					LogManager.Configuration.AddRuleForAllLevels(target);
					LogManager.Configuration.Reload();
					LogManager.ReconfigExistingLoggers();
				}
				else if (text.Substring(2).StartsWith("user="))
				{
					_username = text.Substring(7);
				}
				else if (text.Substring(2).StartsWith("password="))
				{
					_password = text.Substring(11);
				}
				else if (text.Substring(2).StartsWith("otp="))
				{
					_otp = text.Substring(6);
				}
				else if (text.Substring(2).StartsWith("swskuid="))
				{
					_swskuid = text.Substring(10);
				}
				else if (text.Substring(2).StartsWith("flash="))
				{
					_flashimage = text.Substring(8);
				}
				else if (text.Substring(2).StartsWith("script="))
				{
					_script = text.Substring(9);
				}
				else if (text.Substring(2).StartsWith("device="))
				{
					_device = text.Substring(9);
				}
				else if (text.Substring(2).StartsWith("basetests"))
				{
					_isBaseTests = true;
				}
				else if (text.Substring(2).StartsWith("alltests"))
				{
					_isAllTests = true;
				}
				else if (text.Substring(2).StartsWith("erasedata"))
				{
					_erasedata = true;
				}
				else if (text.Substring(2).StartsWith("testharness"))
				{
					_testharness = true;
				}
			}
		}
	}
}
