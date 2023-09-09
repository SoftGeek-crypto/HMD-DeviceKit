using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using NLog;
using RestSharp;

namespace hmd_pctool_windows;

public class LogUtility
{
	public static string fileGen;

	[Conditional("DEBUG")]
	public static void T(string tag, string log)
	{
		PrintLog(tag, log, LogLevel.Trace);
	}

	public static void D(string tag, string log)
	{
		PrintLog(tag, log, LogLevel.Debug);
	}

	[Conditional("DEBUG")]
	public static void I(string tag, string log)
	{
		PrintLog(tag, log, LogLevel.Info);
	}

	[Conditional("DEBUG")]
	public static void W(string tag, string log)
	{
		PrintLog(tag, log, LogLevel.Warn);
	}

	public static void E(string tag, string log)
	{
		PrintLog(tag, log, LogLevel.Error);
	}

	public static void F(string tag, string log)
	{
		PrintLog(tag, log, LogLevel.Fatal);
	}

	public static void PrintLog(string tag, string log, LogLevel logLevel)
	{
		if (Program.GlobalDebugFlag)
		{
			Console.WriteLine(log);
		}
		if (string.IsNullOrEmpty(log))
		{
			return;
		}
		Logger logger = GetLogger(tag);
		string[] array = log.Trim().Replace("\r\n", "\n").Split('\n');
		if (array.Length > 1)
		{
			string[] array2 = array;
			foreach (string message in array2)
			{
				logger.Log(logLevel, message);
			}
		}
		else
		{
			logger.Log(logLevel, log);
		}
	}

	private static Logger GetLogger(string tag)
	{
		Logger currentClassLogger = LogManager.GetCurrentClassLogger();
		currentClassLogger.SetProperty("tag", tag);
		return currentClassLogger;
	}

	public static string fileGenerated(string psn, string user)
	{
		return DateTime.Now.ToString("ddMMMMyyyy_HH.mm_") + psn + "_" + user + "_FlashLog.txt";
	}

	public static string CreateFlashLog(string username, string sn, string fileUsed, string version)
	{
		string text = "C:/HMD_Global/FlashLog/temp/";
		if (Program.isLinux() || Program.isMacOS())
		{
			text = "FlashLog/temp";
		}
		string text2 = "Date:" + Convert.ToString(DateTime.Today);
		VerifyDir(text);
		fileGen = fileGenerated(sn, username);
		try
		{
			DeleteDir(text);
			StreamWriter streamWriter = new StreamWriter(text + fileGen, append: true);
			streamWriter.WriteLine("****Flash Initiated****");
			streamWriter.WriteLine("User:" + username + Environment.NewLine);
			streamWriter.WriteLine("Flashed File:" + fileUsed + Environment.NewLine);
			streamWriter.WriteLine("Initiated Time:" + text2 + Environment.NewLine);
			streamWriter.WriteLine("Mobile PSN:" + sn + Environment.NewLine);
			streamWriter.WriteLine("Tool Version:" + version + Environment.NewLine);
			streamWriter.WriteLine("****Flash Log****");
			streamWriter.Close();
		}
		catch (Exception)
		{
		}
		return fileGen;
	}

	public static void AppendFlashLog(string log)
	{
		try
		{
			string text = "C:/HMD_Global/FlashLog/temp/";
			if (Program.isLinux() || Program.isMacOS())
			{
				text = "FlashLog/temp";
			}
			StreamWriter streamWriter = new StreamWriter(text + fileGen, append: true);
			streamWriter.WriteLine(log);
			streamWriter.Close();
		}
		catch (Exception)
		{
		}
	}

	public static void MoveLogFile()
	{
		try
		{
			string path = "C:/HMD_Global/FlashLog/temp/";
			string text = "C:/HMD_Global/FlashLog/Completed/";
			if (Program.isLinux() || Program.isMacOS())
			{
				path = "FlashLog/temp";
				text = "FlashLog/Completed";
			}
			VerifyDir(text);
			string[] files = Directory.GetFiles(path);
			string[] array = files;
			foreach (string text2 in array)
			{
				File.Move(text2, text + Path.GetFileName(text2));
			}
		}
		catch
		{
		}
	}

	public static void uploadFlashLogBlob()
	{
		string path = "C:/HMD_Global/FlashLog/temp/";
		if (Program.isLinux() || Program.isMacOS())
		{
			path = "FlashLog/temp";
		}
		VerifyDir(path);
		string[] files = Directory.GetFiles(path);
		string[] array = files;
		foreach (string path2 in array)
		{
			string baseUrl = "https://prod-21.southeastasia.logic.azure.com:443/workflows/d11c5f413add48f892434d919124a720/triggers/manual/paths/invoke?api-version=2016-10-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=HildMrXpllTO8Txl-TLgGo089w5oOh8TPtywOzHOlaA";
			string fileName = Path.GetFileName(path2);
			string value = File.ReadAllText(path2);
			StringBuilder stringBuilder = new StringBuilder();
			StringWriter textWriter = new StringWriter(stringBuilder);
			using (JsonWriter jsonWriter = new JsonTextWriter(textWriter))
			{
				jsonWriter.Formatting = Formatting.Indented;
				jsonWriter.WriteStartObject();
				jsonWriter.WritePropertyName("fileName");
				jsonWriter.WriteValue(fileName);
				jsonWriter.WritePropertyName("fileContent");
				jsonWriter.WriteValue(value);
				jsonWriter.WriteEndObject();
			}
			string value2 = stringBuilder.ToString();
			RestClient restClient = new RestClient(baseUrl);
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
			RestRequest restRequest = new RestRequest(Method.POST);
			restRequest.AddHeader("content-type", "application/json");
			restRequest.AddHeader("cache-control", "no-cache");
			restRequest.AddHeader("keep-alive", "false");
			restRequest.AddParameter("application/json", value2, ParameterType.RequestBody);
			IRestResponse restResponse = restClient.Execute(restRequest);
			string text = Convert.ToString(restResponse.ResponseStatus);
		}
	}

	public static void WriteLog(string tag, string log)
	{
		string text = "C:/RALog/";
		if (Program.isLinux() || Program.isMacOS())
		{
			text = "RALog/";
		}
		VerifyDir(text);
		string text2 = "RALogInfo.txt";
		try
		{
			StreamWriter streamWriter = new StreamWriter(text + text2, append: true);
			streamWriter.WriteLine(DateTime.Today.ToString() + ":" + tag + ":" + log + ":" + Environment.NewLine);
			streamWriter.Close();
		}
		catch (Exception)
		{
		}
	}

	public static void VerifyDir(string path)
	{
		try
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(path);
			if (!directoryInfo.Exists)
			{
				directoryInfo.Create();
			}
		}
		catch
		{
		}
	}

	public static void DeleteDir(string path)
	{
		string[] files = Directory.GetFiles(path);
		string[] array = files;
		foreach (string path2 in array)
		{
			File.Delete(path2);
		}
	}
}
