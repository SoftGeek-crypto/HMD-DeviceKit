using System;
using System.ComponentModel;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using RestSharp;

namespace hmd_pctool_windows.Components;

public class DevicePanel : UserControl
{
	private class SREApiReturn
	{
		public string File_Name { get; set; }

		public string Country { get; set; }

		public string PSN { get; set; }

		public DateTime ContextDate { get; set; }

		public string DownloadURL { get; set; }

		public string Status { get; set; }
	}

	private delegate void UpdateUI(Control control, object value);

	private string romPath = string.Empty;

	private bool isIgnorePermission = false;

	private bool isEraseUserdata = false;

	private bool isEraseFrp = false;

	private bool isRunTest = false;

	private string targetSku = string.Empty;

	private Device device;

	private StringBuilder logStringBuilder;

	private LogForm logForm;

	private string tag;

	private SREApiReturn apiReturned = new SREApiReturn();

	private string deviceSN;

	private DateTime contextDate = DateTime.Now;

	private string countryISO = null;

	public bool isUserGivenIso;

	public static string path = string.Empty;

	private bool isRunningCommand = false;

	private CommandType runningCommandType = CommandType.NoRequired;

	private bool testMode = false;

	private int testTimes = -1;

	private int testRunningTimes = 0;

	private int testErrorTimes = 0;

	private static readonly HttpClient client = new HttpClient();

	private IContainer components = null;

	private Label labelSnTitle;

	private Label labelSn;

	private ProgressBar progressFlash;

	private GreenButton buttonCancel;

	private GreenButton buttonFlash;

	private Button buttonRom;

	private Button buttonShowLog;

	private CheckBox checkBoxReset;

	private CheckBox checkBoxIgnorePermission;

	private Label labelRomPath;

	private ComboBox comboBoxSku;

	private Label labelPercent;

	private Label labelRunTest;

	private Label labelOta;

	private Button buttonSku;

	private Label labelIMEI;

	private Label labelIMEITitle;

	private CheckBox checkBoxEraseFrp;

	private Button btnSetISO;

	private MaskedTextBox maskTxtBox;

	private Button button1;

	private Button btnCopyURL;

	private MaskedTextBox txtBxDownloadURL;

	private Button btnGetLatest;

	private MaskedTextBox txtBxFileName;

	private Panel panel1;

	private CheckBox chkBxTests;

	public Device PanelDevice => device;

	public bool IsFlashing => isRunningCommand && runningCommandType == CommandType.Flash;

	public bool IsRunning => isRunningCommand;

	public static string[] GetSKUIDs()
	{
		return new string[13]
		{
			"600WW", "600ID", "600RU", "600EEA", "600CLA", "600TEL", "600EEE", "600VPR", "600VPO", "600FD",
			"600IN", "600M0", "6000F"
		};
	}

	public DevicePanel()
	{
		InitializeComponent();
	}

	public DevicePanel(Device device)
		: this()
	{
		this.device = device;
		if (this.device != null)
		{
			this.device.DeviceEventHandler += DevicePanelEventHandler;
			tag = "DevicePanel-" + this.device.SN;
		}
		logStringBuilder = new StringBuilder();
	}

	private void DevicePanelEventHandler(object sender, DeviceEventArgs args)
	{
		DeviceEventType eventType = args.EventType;
		switch (eventType)
		{
		case DeviceEventType.OnFlashStatus:
			ProcessFlashStatus(args);
			break;
		case DeviceEventType.OnCommandFail:
		case DeviceEventType.OnCommandSuccess:
			if (runningCommandType == CommandType.GetSku)
			{
				UpdateSku(eventType == DeviceEventType.OnCommandSuccess);
			}
			isRunningCommand = false;
			runningCommandType = CommandType.NoRequired;
			break;
		case DeviceEventType.UpdateLog:
			UpdateLogFromLog(args.StringArg);
			break;
		case DeviceEventType.OnReadItem:
		{
			DeviceItemType deviceItemType = (DeviceItemType)args.What;
			Invoke((MethodInvoker)delegate
			{
				if (deviceItemType == DeviceItemType.ReadOnlyImei)
				{
					labelIMEI.Text = args.StringArg;
				}
			});
			break;
		}
		case DeviceEventType.OnUnlockFrpStatus:
		case DeviceEventType.OnGetVar:
		case DeviceEventType.OnWriteItem:
			break;
		}
	}

	private void ProcessFlashStatus(DeviceEventArgs args)
	{
		FlashStatus what = (FlashStatus)args.What;
		if (args.What == 0 || args.What == 1)
		{
			if (base.Parent != null || base.Parent.GetType() != typeof(FlowLayoutPanel))
			{
				CheckBox checkBox = (base.Parent as FlowLayoutPanel).Controls["chkBxCFlash"] as CheckBox;
				checkBox.Enabled = false;
			}
		}
		else
		{
			CheckBox checkBox2 = (base.Parent as FlowLayoutPanel).Controls["chkBxCFlash"] as CheckBox;
			checkBox2.Enabled = true;
		}
		switch (what)
		{
		case FlashStatus.Started:
			if (!string.IsNullOrEmpty(args.StringArg) && args.StringArg.Equals("ota"))
			{
				labelOta.Visible = true;
			}
			break;
		case FlashStatus.InProgress:
		{
			int intArg = args.IntArg;
			SetProgress(intArg);
			break;
		}
		case FlashStatus.Fail:
			isRunningCommand = false;
			runningCommandType = CommandType.NoRequired;
			UpdateLogFromLog(args.StringArg);
			if (testMode)
			{
				progressFlash.Value = 0;
				labelPercent.Text = "0 %";
				SetFlashUIStatus(isFlash: false);
				testErrorTimes++;
				if (testTimes == testRunningTimes)
				{
					labelRunTest.Text = $"Test {testTimes} times, success {testTimes - testErrorTimes} times, error {testErrorTimes}";
				}
			}
			else
			{
				DialogResult dialogResult = MessageBox.Show(args.StringArg, device.SN);
				if (dialogResult == DialogResult.OK)
				{
					progressFlash.Value = 0;
					labelPercent.Text = "0 %";
					SetFlashUIStatus(isFlash: false);
				}
			}
			UpdateLogFromLog("--------------------------");
			break;
		case FlashStatus.Success:
			isRunningCommand = false;
			runningCommandType = CommandType.NoRequired;
			UpdateLogFromLog(args.StringArg);
			if (testMode)
			{
				SetFlashUIStatus(isFlash: false);
				if (testTimes == testRunningTimes)
				{
					labelRunTest.Text = $"Test {testTimes} times, success {testTimes - testErrorTimes} times, error {testErrorTimes}";
				}
			}
			else
			{
				MessageBox.Show("Flash Success....", device.SN, MessageBoxButtons.OK, MessageBoxIcon.Asterisk, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
				SetFlashUIStatus(isFlash: false);
			}
			break;
		case FlashStatus.Stopped:
			break;
		}
	}

	private void DevicePanel_Load(object sender, EventArgs e)
	{
		ComboBox.ObjectCollection items = comboBoxSku.Items;
		object[] sKUIDs = GetSKUIDs();
		items.AddRange(sKUIDs);
		if (device != null)
		{
			labelSn.Text = device.SN;
			device.ReadItem(DeviceItemType.ReadOnlyImei);
		}
		labelPercent.Text = "0%";
		comboBoxSku.Enabled = false;
		buttonFlash.Enabled = false;
		buttonCancel.Enabled = false;
		if (Program.UserMode == RrltUserMode.Tester)
		{
			chkBxTests.Visible = true;
		}
	}

	private void UpdateLogFromLog(string log)
	{
		if (logForm != null)
		{
			logForm.UpdateLog(log);
		}
		if (!string.IsNullOrEmpty(log))
		{
			logStringBuilder.AppendLine(log);
		}
	}

	private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
	{
		targetSku = comboBoxSku.SelectedItem.ToString();
		if (device != null && device is HmdDevice)
		{
			(device as HmdDevice).SetTargetSku(targetSku);
		}
	}

	private void CheckBoxReset_CheckedChanged(object sender, EventArgs e)
	{
		isEraseUserdata = checkBoxReset.Checked;
	}

	private void checkBoxEraseFrp_CheckedChanged(object sender, EventArgs e)
	{
		isEraseFrp = checkBoxEraseFrp.Checked;
	}

	private void CheckBoxIgnorePermission_CheckedChanged(object sender, EventArgs e)
	{
		if (checkBoxIgnorePermission.Checked)
		{
			DialogResult dialogResult = MessageBox.Show("Ignore permission fail may causes flash fail or other issues.\nDo you still want to ignore permission fail? ", device.SN, MessageBoxButtons.YesNo);
			if (dialogResult == DialogResult.Yes)
			{
				checkBoxIgnorePermission.Checked = true;
				isIgnorePermission = true;
			}
			else
			{
				checkBoxIgnorePermission.Checked = false;
				isIgnorePermission = false;
			}
		}
	}

	private void chkBxTests_CheckedChanged(object sender, EventArgs e)
	{
		isRunTest = chkBxTests.Checked;
	}

	private void ButtonRom_Click(object sender, EventArgs e)
	{
		if (HmdDevice.IsCFlashEnabled && !string.IsNullOrEmpty(path))
		{
			romPath = path;
			MessageBox.Show("Continuous flashing is enabled..." + Environment.NewLine + "Same file will be used to flash" + Environment.NewLine + "FilePath: " + path);
			if (!string.IsNullOrEmpty(romPath))
			{
				labelRomPath.Text = romPath;
				buttonFlash.Enabled = true;
				buttonCancel.Enabled = false;
				labelRomPath.Refresh();
			}
			return;
		}
		OpenFileDialog openFileDialog = new OpenFileDialog();
		openFileDialog.Title = "Select ROM file";
		openFileDialog.InitialDirectory = ".\\";
		openFileDialog.Filter = "zip files (*.*)|*.zip";
		if (openFileDialog.ShowDialog() == DialogResult.OK)
		{
			romPath = openFileDialog.FileName;
			path = openFileDialog.FileName;
			if (!string.IsNullOrEmpty(romPath))
			{
				labelRomPath.Text = romPath;
				buttonFlash.Enabled = true;
				buttonCancel.Enabled = false;
				labelRomPath.Refresh();
			}
		}
	}

	public void ApplyRom(string romPath)
	{
		this.romPath = romPath;
		if (!string.IsNullOrEmpty(this.romPath))
		{
			labelRomPath.Text = this.romPath;
			buttonFlash.Enabled = true;
			buttonCancel.Enabled = false;
			labelRomPath.Refresh();
		}
	}

	private void ButtonFlash_Click(object sender, EventArgs e)
	{
		if (string.IsNullOrEmpty(romPath))
		{
			MessageBox.Show("Please select a rom first.", device.SN);
			return;
		}
		SetFlashUIStatus(isFlash: true);
		LogUtility.CreateFlashLog(AzureNativeClient.Instance.UserName, device.SN, labelRomPath.Text, base.ProductVersion);
		progressFlash.Value = 0;
		labelPercent.Text = "0 %";
		if (device != null && device is HmdDevice)
		{
			(device as HmdDevice).SetIsEraseUserData(isEraseUserdata);
			(device as HmdDevice).SetIsEraseFRP(isEraseFrp);
			(device as HmdDevice).SetIsIgnorePermissionDuringFlash(isIgnorePermission);
			(device as HmdDevice).SetIsRunTests(isRunTest, isAllTests: false);
		}
		LogUtility.D(tag, $"Trigger flash, erase userdata = {isEraseUserdata}, ignore permission = {isIgnorePermission}");
		if (!TriggerCommand(CommandType.Flash))
		{
			SetFlashUIStatus(isFlash: false);
		}
	}

	private void ButtonShowLog_Click(object sender, EventArgs e)
	{
		if (logForm == null && device != null)
		{
			logForm = new LogForm("Log-" + device.SN, logStringBuilder);
			logForm.FormClosing += FormClosing;
			logForm.Show();
		}
		else
		{
			logForm.BringToFront();
		}
	}

	private void FormClosing(object sender, FormClosingEventArgs e)
	{
		logForm = null;
	}

	private void ButtonCancel_Click(object sender, EventArgs e)
	{
		if (isRunningCommand && runningCommandType == CommandType.Flash)
		{
			DialogResult dialogResult = MessageBox.Show("Cancel flashing progress will casus device fail or other issue.\nDo you still want to cancel image flashing?", device.SN, MessageBoxButtons.YesNo);
			if (dialogResult == DialogResult.Yes && device != null)
			{
				CancelFlash();
			}
		}
	}

	private void ButtonSku_Click(object sender, EventArgs e)
	{
		buttonSku.Enabled = false;
		UpdateCurrentSkuId();
	}

	private void UpdateProgress(Control control, object value)
	{
		if (control != null && value != null)
		{
			if (control.InvokeRequired)
			{
				UpdateUI method = UpdateProgress;
				control.Invoke(method, control, value);
			}
			else
			{
				ProgressBar progressBar = control as ProgressBar;
				progressBar.Value = (int)value;
				progressBar.CreateGraphics().DrawString(progressBar.Value + "%", new Font("Arial", 8.25f, FontStyle.Regular), Brushes.Black, new PointF(progressBar.Width / 2 - 10, progressBar.Height / 2 - 7));
			}
		}
	}

	private void SetProgress(int value)
	{
		if (value >= progressFlash.Value)
		{
			progressFlash.Value = value;
			progressFlash.Refresh();
			labelPercent.Text = $"{value} %";
		}
	}

	private bool TriggerCommand(CommandType type)
	{
		if (isRunningCommand)
		{
			MessageBox.Show("Last command is running, abort.", device.SN);
			return false;
		}
		if (device == null)
		{
			MessageBox.Show("Device is missing, Please connect device again.", device.SN);
			return false;
		}
		device.ExitFastbootProcess();
		isRunningCommand = true;
		runningCommandType = type;
		switch (type)
		{
		case CommandType.Flash:
			device.Flash(romPath);
			break;
		case CommandType.GetSku:
			device.GetSku();
			break;
		}
		return true;
	}

	public void CancelFlash()
	{
		if (device != null && isRunningCommand && runningCommandType == CommandType.Flash)
		{
			device.CancelWork();
		}
	}

	private void UpdateCurrentSkuId()
	{
		TriggerCommand(CommandType.GetSku);
	}

	public void UnRegisterEvent()
	{
		if (device != null)
		{
			device.DeviceEventHandler -= DevicePanelEventHandler;
		}
	}

	public void Exit()
	{
		UnRegisterEvent();
		device.ExitFastbootProcess();
	}

	private void SetFlashUIStatus(bool isFlash)
	{
		checkBoxReset.Enabled = !isFlash;
		checkBoxIgnorePermission.Enabled = !isFlash;
		checkBoxEraseFrp.Enabled = !isFlash;
		chkBxTests.Enabled = !isFlash;
		if (device != null && device is HmdDevice)
		{
			string sKU = (device as HmdDevice).SKU;
			if (!string.IsNullOrEmpty(sKU))
			{
				comboBoxSku.Enabled = !isFlash;
			}
		}
		buttonFlash.Enabled = !isFlash;
		buttonCancel.Enabled = isFlash;
		buttonRom.Enabled = !isFlash;
		if (labelOta.Visible)
		{
			labelOta.Visible = false;
		}
	}

	public void CloseLogForm()
	{
		if (logForm != null)
		{
			logForm.Close();
		}
	}

	private void UpdateSku(bool isSkuGet)
	{
		buttonSku.Enabled = false;
		if (isSkuGet)
		{
			string sKU = (device as HmdDevice).SKU;
			if (string.IsNullOrEmpty(sKU))
			{
			}
			for (int i = 0; i < GetSKUIDs().Length; i++)
			{
				if (sKU.ToUpper().Equals(GetSKUIDs()[i].ToUpper()))
				{
					comboBoxSku.SelectedIndex = i;
					break;
				}
			}
			comboBoxSku.Enabled = true;
		}
		else
		{
			comboBoxSku.Enabled = false;
		}
	}

	public void LaunchTestMode(int times)
	{
		testMode = true;
		testTimes = times;
		new Thread(RunTest).Start();
	}

	private async void RunTest()
	{
		if (!buttonFlash.Enabled)
		{
			return;
		}
		string label = "Running test";
		for (int i = 0; i < testTimes; i++)
		{
			testRunningTimes = i + 1;
			UpdateLogFromLog($"Start {testRunningTimes} time test >>>>>>>>>>");
			BeginInvoke((Action)delegate
			{
				labelRunTest.Text = label + $" #{testRunningTimes}";
				labelRunTest.Visible = true;
				labelRunTest.Refresh();
				ButtonFlash_Click(null, null);
			});
			await lockR();
			UpdateLogFromLog($"<<<<<<<<<< Finish {testRunningTimes} time test ");
			Thread.Sleep(10000);
		}
	}

	private async Task<bool> lockR()
	{
		do
		{
			Thread.Sleep(1000);
		}
		while (IsRunning);
		return true;
	}

	public string assignSn(Device device)
	{
		this.device = device;
		deviceSN = this.device.SN;
		deviceSN = deviceSN.Trim();
		return deviceSN;
	}

	private SREApiReturn GenerateDownload_URl(string selectSN)
	{
		try
		{
			string baseUrl = ConfigurationManager.AppSettings["SreEndPointUrl"].ToString();
			string s = ConfigurationManager.AppSettings["SrePassword"].ToString();
			StringBuilder stringBuilder = new StringBuilder();
			StringWriter textWriter = new StringWriter(stringBuilder);
			contextDate = contextDate.AddHours(-6.0);
			using (JsonWriter jsonWriter = new JsonTextWriter(textWriter))
			{
				jsonWriter.Formatting = Formatting.Indented;
				jsonWriter.WriteStartObject();
				jsonWriter.WritePropertyName("Psn");
				jsonWriter.WriteValue(selectSN);
				jsonWriter.WritePropertyName("Country");
				jsonWriter.WriteValue(countryISO);
				jsonWriter.WritePropertyName("ContextDate");
				jsonWriter.WriteValue(contextDate);
				jsonWriter.WriteEndObject();
			}
			string value = stringBuilder.ToString();
			RestClient restClient = new RestClient(baseUrl);
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
			RestRequest restRequest = new RestRequest(Method.POST);
			string value2 = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(s));
			restRequest.AddHeader("authorization", value2);
			restRequest.AddHeader("Ocp-Apim-Subscription-Key", "138be4d45aab4d42b22b506581c3cf60");
			restRequest.AddHeader("content-type", "application/json");
			restRequest.AddHeader("cache-control", "no-cache");
			restRequest.AddHeader("keep-alive", "false");
			restRequest.AddParameter("application/json", value, ParameterType.RequestBody);
			IRestResponse restResponse = restClient.Execute(restRequest);
			string text = Convert.ToString(restResponse.Content);
			if (text.Contains("Error"))
			{
				MessageBox.Show("We couldn't find the file for the provided SN");
			}
			SREApiReturn sREApiReturn = JsonConvert.DeserializeObject<SREApiReturn>(restResponse.Content);
			apiReturned = sREApiReturn;
			return apiReturned;
		}
		catch (Exception ex)
		{
			MessageBox.Show("Failed Exception:" + ex.Message);
			return apiReturned = null;
		}
	}

	private void btnGetLatest_Click(object sender, EventArgs e)
	{
		assignSn(device);
		Thread thread = new Thread((ThreadStart)delegate
		{
			MessageBox.Show("Please wait we are generating the latest url");
		});
		thread.Start();
		Thread thread2 = new Thread((ThreadStart)delegate
		{
			GenerateDownload_URl(deviceSN);
		});
		thread2.Start();
		thread2.Join();
		txtBxFileName.Text = apiReturned.File_Name;
		txtBxDownloadURL.Text = apiReturned.DownloadURL;
		maskTxtBox.Text = apiReturned.Country;
		if (apiReturned.File_Name != null && apiReturned.DownloadURL != null)
		{
			MessageBox.Show(apiReturned.Status.Replace("$", Environment.NewLine));
		}
	}

	private void btnCopyURL_Click(object sender, EventArgs e)
	{
		if (txtBxDownloadURL.Text.Length != 0)
		{
			Clipboard.SetText(txtBxDownloadURL.Text);
			MessageBox.Show("Copied URL to clipboard");
		}
		else
		{
			MessageBox.Show("Please generate URL first!!!");
		}
	}

	private void btnSetISO_Click(object sender, EventArgs e)
	{
		if (maskTxtBox.Text.Length == 2)
		{
			countryISO = maskTxtBox.Text;
			isUserGivenIso = true;
			MessageBox.Show("Success!!! Now click 'get latest' to get latest file details");
		}
		else
		{
			maskTxtBox.Clear();
			MessageBox.Show("Please enter 2 digit country code !!!");
		}
		countryISO = maskTxtBox.Text;
	}

	private void button1_Click(object sender, EventArgs e)
	{
		Clipboard.Clear();
		apiReturned.ContextDate = DateTime.Now;
		apiReturned.DownloadURL = null;
		apiReturned.File_Name = null;
		apiReturned.PSN = null;
		apiReturned.Country = null;
		countryISO = null;
		txtBxDownloadURL.Clear();
		txtBxFileName.Clear();
		maskTxtBox.Clear();
		MessageBox.Show("Cleared! Click on 'Get Latest' to generate latest file details");
	}

	private void labelSn_Click(object sender, EventArgs e)
	{
		Clipboard.SetText(labelSn.Text);
		MessageBox.Show("Copied to Clipboard -> Ctrl + V to paste the SN");
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		this.labelSnTitle = new System.Windows.Forms.Label();
		this.labelSn = new System.Windows.Forms.Label();
		this.progressFlash = new System.Windows.Forms.ProgressBar();
		this.buttonRom = new System.Windows.Forms.Button();
		this.buttonShowLog = new System.Windows.Forms.Button();
		this.checkBoxReset = new System.Windows.Forms.CheckBox();
		this.checkBoxIgnorePermission = new System.Windows.Forms.CheckBox();
		this.labelRomPath = new System.Windows.Forms.Label();
		this.comboBoxSku = new System.Windows.Forms.ComboBox();
		this.labelPercent = new System.Windows.Forms.Label();
		this.labelRunTest = new System.Windows.Forms.Label();
		this.labelOta = new System.Windows.Forms.Label();
		this.buttonSku = new System.Windows.Forms.Button();
		this.labelIMEI = new System.Windows.Forms.Label();
		this.labelIMEITitle = new System.Windows.Forms.Label();
		this.checkBoxEraseFrp = new System.Windows.Forms.CheckBox();
		this.btnSetISO = new System.Windows.Forms.Button();
		this.maskTxtBox = new System.Windows.Forms.MaskedTextBox();
		this.button1 = new System.Windows.Forms.Button();
		this.btnCopyURL = new System.Windows.Forms.Button();
		this.txtBxDownloadURL = new System.Windows.Forms.MaskedTextBox();
		this.btnGetLatest = new System.Windows.Forms.Button();
		this.txtBxFileName = new System.Windows.Forms.MaskedTextBox();
		this.buttonFlash = new hmd_pctool_windows.Components.GreenButton();
		this.buttonCancel = new hmd_pctool_windows.Components.GreenButton();
		this.panel1 = new System.Windows.Forms.Panel();
		this.chkBxTests = new System.Windows.Forms.CheckBox();
		this.panel1.SuspendLayout();
		base.SuspendLayout();
		this.labelSnTitle.AutoSize = true;
		this.labelSnTitle.Font = new System.Drawing.Font("Calibri", 10f);
		this.labelSnTitle.Location = new System.Drawing.Point(1, 7);
		this.labelSnTitle.Margin = new System.Windows.Forms.Padding(5, 5, 0, 0);
		this.labelSnTitle.Name = "labelSnTitle";
		this.labelSnTitle.Size = new System.Drawing.Size(27, 17);
		this.labelSnTitle.TabIndex = 1;
		this.labelSnTitle.Text = "SN:";
		this.labelSn.BackColor = System.Drawing.Color.Transparent;
		this.labelSn.Font = new System.Drawing.Font("Calibri", 10f);
		this.labelSn.ForeColor = System.Drawing.Color.MediumSeaGreen;
		this.labelSn.Location = new System.Drawing.Point(41, 7);
		this.labelSn.Margin = new System.Windows.Forms.Padding(5, 5, 0, 0);
		this.labelSn.Name = "labelSn";
		this.labelSn.Size = new System.Drawing.Size(246, 17);
		this.labelSn.TabIndex = 2;
		this.labelSn.Text = "SN";
		this.labelSn.Click += new System.EventHandler(labelSn_Click);
		this.progressFlash.Location = new System.Drawing.Point(82, 79);
		this.progressFlash.Margin = new System.Windows.Forms.Padding(20, 5, 5, 11);
		this.progressFlash.Name = "progressFlash";
		this.progressFlash.Size = new System.Drawing.Size(334, 22);
		this.progressFlash.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
		this.progressFlash.TabIndex = 5;
		this.buttonRom.Font = new System.Drawing.Font("Calibri", 9f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.buttonRom.Location = new System.Drawing.Point(4, 79);
		this.buttonRom.Margin = new System.Windows.Forms.Padding(4, 4, 4, 0);
		this.buttonRom.Name = "buttonRom";
		this.buttonRom.Size = new System.Drawing.Size(75, 22);
		this.buttonRom.TabIndex = 8;
		this.buttonRom.Text = "Select Rom";
		this.buttonRom.UseVisualStyleBackColor = true;
		this.buttonRom.Click += new System.EventHandler(ButtonRom_Click);
		this.buttonShowLog.Font = new System.Drawing.Font("Calibri", 9f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.buttonShowLog.Location = new System.Drawing.Point(596, 5);
		this.buttonShowLog.Margin = new System.Windows.Forms.Padding(3, 1, 3, 3);
		this.buttonShowLog.Name = "buttonShowLog";
		this.buttonShowLog.Size = new System.Drawing.Size(40, 25);
		this.buttonShowLog.TabIndex = 9;
		this.buttonShowLog.Text = "Log";
		this.buttonShowLog.UseVisualStyleBackColor = true;
		this.buttonShowLog.Click += new System.EventHandler(ButtonShowLog_Click);
		this.checkBoxReset.AutoSize = true;
		this.checkBoxReset.Font = new System.Drawing.Font("Calibri", 9f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.checkBoxReset.Location = new System.Drawing.Point(461, 3);
		this.checkBoxReset.Name = "checkBoxReset";
		this.checkBoxReset.Size = new System.Drawing.Size(108, 18);
		this.checkBoxReset.TabIndex = 10;
		this.checkBoxReset.Text = "Erase userdata";
		this.checkBoxReset.UseVisualStyleBackColor = true;
		this.checkBoxReset.CheckedChanged += new System.EventHandler(CheckBoxReset_CheckedChanged);
		this.checkBoxIgnorePermission.AutoSize = true;
		this.checkBoxIgnorePermission.Font = new System.Drawing.Font("Calibri", 9f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.checkBoxIgnorePermission.Location = new System.Drawing.Point(461, 36);
		this.checkBoxIgnorePermission.Name = "checkBoxIgnorePermission";
		this.checkBoxIgnorePermission.Size = new System.Drawing.Size(149, 18);
		this.checkBoxIgnorePermission.TabIndex = 11;
		this.checkBoxIgnorePermission.Text = "Ignore Permission Fail";
		this.checkBoxIgnorePermission.UseVisualStyleBackColor = true;
		this.checkBoxIgnorePermission.CheckedChanged += new System.EventHandler(CheckBoxIgnorePermission_CheckedChanged);
		this.labelRomPath.BackColor = System.Drawing.Color.Transparent;
		this.labelRomPath.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25f);
		this.labelRomPath.ForeColor = System.Drawing.Color.MediumSeaGreen;
		this.labelRomPath.Location = new System.Drawing.Point(82, 42);
		this.labelRomPath.Margin = new System.Windows.Forms.Padding(0, 3, 3, 0);
		this.labelRomPath.Name = "labelRomPath";
		this.labelRomPath.Size = new System.Drawing.Size(334, 38);
		this.labelRomPath.TabIndex = 12;
		this.labelRomPath.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
		this.comboBoxSku.Font = new System.Drawing.Font("Calibri", 9f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.comboBoxSku.FormattingEnabled = true;
		this.comboBoxSku.Location = new System.Drawing.Point(368, 7);
		this.comboBoxSku.Margin = new System.Windows.Forms.Padding(1);
		this.comboBoxSku.Name = "comboBoxSku";
		this.comboBoxSku.Size = new System.Drawing.Size(80, 22);
		this.comboBoxSku.TabIndex = 13;
		this.comboBoxSku.SelectedIndexChanged += new System.EventHandler(ComboBox1_SelectedIndexChanged);
		this.labelPercent.Font = new System.Drawing.Font("Calibri", 9f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.labelPercent.Location = new System.Drawing.Point(421, 79);
		this.labelPercent.Margin = new System.Windows.Forms.Padding(0, 0, 0, 11);
		this.labelPercent.Name = "labelPercent";
		this.labelPercent.Size = new System.Drawing.Size(40, 22);
		this.labelPercent.TabIndex = 15;
		this.labelPercent.Text = "100 %";
		this.labelPercent.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
		this.labelRunTest.Font = new System.Drawing.Font("Calibri", 9f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.labelRunTest.ForeColor = System.Drawing.Color.Red;
		this.labelRunTest.Location = new System.Drawing.Point(85, 51);
		this.labelRunTest.Name = "labelRunTest";
		this.labelRunTest.Size = new System.Drawing.Size(331, 25);
		this.labelRunTest.TabIndex = 16;
		this.labelRunTest.Text = "Running test #";
		this.labelRunTest.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.labelRunTest.Visible = false;
		this.labelOta.Font = new System.Drawing.Font("Calibri", 9f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.labelOta.ForeColor = System.Drawing.Color.FromArgb(65, 214, 171);
		this.labelOta.Location = new System.Drawing.Point(1, 51);
		this.labelOta.Margin = new System.Windows.Forms.Padding(5, 5, 0, 0);
		this.labelOta.Name = "labelOta";
		this.labelOta.Size = new System.Drawing.Size(30, 22);
		this.labelOta.TabIndex = 17;
		this.labelOta.Text = "OTA";
		this.labelOta.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.labelOta.Visible = false;
		this.buttonSku.Location = new System.Drawing.Point(304, 7);
		this.buttonSku.Name = "buttonSku";
		this.buttonSku.Size = new System.Drawing.Size(60, 24);
		this.buttonSku.TabIndex = 18;
		this.buttonSku.Text = "Get sku";
		this.buttonSku.UseVisualStyleBackColor = true;
		this.buttonSku.Click += new System.EventHandler(ButtonSku_Click);
		this.labelIMEI.BackColor = System.Drawing.Color.Transparent;
		this.labelIMEI.Font = new System.Drawing.Font("Calibri", 10f);
		this.labelIMEI.Location = new System.Drawing.Point(41, 26);
		this.labelIMEI.Margin = new System.Windows.Forms.Padding(5, 5, 0, 0);
		this.labelIMEI.Name = "labelIMEI";
		this.labelIMEI.Size = new System.Drawing.Size(246, 17);
		this.labelIMEI.TabIndex = 20;
		this.labelIMEITitle.AutoSize = true;
		this.labelIMEITitle.Font = new System.Drawing.Font("Calibri", 10f);
		this.labelIMEITitle.Location = new System.Drawing.Point(1, 26);
		this.labelIMEITitle.Margin = new System.Windows.Forms.Padding(5, 5, 0, 0);
		this.labelIMEITitle.Name = "labelIMEITitle";
		this.labelIMEITitle.Size = new System.Drawing.Size(39, 17);
		this.labelIMEITitle.TabIndex = 19;
		this.labelIMEITitle.Text = "IMEI:";
		this.checkBoxEraseFrp.AutoSize = true;
		this.checkBoxEraseFrp.Font = new System.Drawing.Font("Calibri", 9f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.checkBoxEraseFrp.Location = new System.Drawing.Point(461, 20);
		this.checkBoxEraseFrp.Name = "checkBoxEraseFrp";
		this.checkBoxEraseFrp.Size = new System.Drawing.Size(78, 18);
		this.checkBoxEraseFrp.TabIndex = 21;
		this.checkBoxEraseFrp.Text = "Erase FRP";
		this.checkBoxEraseFrp.UseVisualStyleBackColor = true;
		this.checkBoxEraseFrp.CheckedChanged += new System.EventHandler(checkBoxEraseFrp_CheckedChanged);
		this.btnSetISO.BackColor = System.Drawing.Color.DarkCyan;
		this.btnSetISO.Font = new System.Drawing.Font("Calibri", 9f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnSetISO.ForeColor = System.Drawing.Color.White;
		this.btnSetISO.Location = new System.Drawing.Point(5, 119);
		this.btnSetISO.Margin = new System.Windows.Forms.Padding(4, 4, 4, 0);
		this.btnSetISO.Name = "btnSetISO";
		this.btnSetISO.Size = new System.Drawing.Size(77, 20);
		this.btnSetISO.TabIndex = 53;
		this.btnSetISO.Text = "Set ISO";
		this.btnSetISO.UseVisualStyleBackColor = false;
		this.btnSetISO.Click += new System.EventHandler(btnSetISO_Click);
		this.maskTxtBox.Location = new System.Drawing.Point(85, 119);
		this.maskTxtBox.Name = "maskTxtBox";
		this.maskTxtBox.Size = new System.Drawing.Size(55, 20);
		this.maskTxtBox.TabIndex = 51;
		this.button1.BackColor = System.Drawing.Color.DarkCyan;
		this.button1.Font = new System.Drawing.Font("Calibri", 9f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.button1.ForeColor = System.Drawing.Color.White;
		this.button1.Location = new System.Drawing.Point(555, 119);
		this.button1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 0);
		this.button1.Name = "button1";
		this.button1.Size = new System.Drawing.Size(81, 20);
		this.button1.TabIndex = 50;
		this.button1.Text = "Clear Fields";
		this.button1.UseVisualStyleBackColor = false;
		this.button1.Click += new System.EventHandler(button1_Click);
		this.btnCopyURL.BackColor = System.Drawing.Color.DarkCyan;
		this.btnCopyURL.Font = new System.Drawing.Font("Calibri", 9f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnCopyURL.ForeColor = System.Drawing.Color.White;
		this.btnCopyURL.Location = new System.Drawing.Point(555, 145);
		this.btnCopyURL.Margin = new System.Windows.Forms.Padding(4, 4, 4, 0);
		this.btnCopyURL.Name = "btnCopyURL";
		this.btnCopyURL.Size = new System.Drawing.Size(80, 20);
		this.btnCopyURL.TabIndex = 49;
		this.btnCopyURL.Text = "Copy URL";
		this.btnCopyURL.UseVisualStyleBackColor = false;
		this.btnCopyURL.Click += new System.EventHandler(btnCopyURL_Click);
		this.txtBxDownloadURL.Location = new System.Drawing.Point(5, 145);
		this.txtBxDownloadURL.Name = "txtBxDownloadURL";
		this.txtBxDownloadURL.Size = new System.Drawing.Size(547, 20);
		this.txtBxDownloadURL.TabIndex = 48;
		this.btnGetLatest.BackColor = System.Drawing.Color.DarkCyan;
		this.btnGetLatest.Font = new System.Drawing.Font("Calibri", 9f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnGetLatest.ForeColor = System.Drawing.Color.White;
		this.btnGetLatest.Location = new System.Drawing.Point(147, 119);
		this.btnGetLatest.Margin = new System.Windows.Forms.Padding(4, 4, 4, 0);
		this.btnGetLatest.Name = "btnGetLatest";
		this.btnGetLatest.Size = new System.Drawing.Size(78, 20);
		this.btnGetLatest.TabIndex = 47;
		this.btnGetLatest.Text = "Get Latest";
		this.btnGetLatest.UseVisualStyleBackColor = false;
		this.btnGetLatest.Click += new System.EventHandler(btnGetLatest_Click);
		this.txtBxFileName.Location = new System.Drawing.Point(232, 119);
		this.txtBxFileName.Name = "txtBxFileName";
		this.txtBxFileName.Size = new System.Drawing.Size(320, 20);
		this.txtBxFileName.TabIndex = 46;
		this.buttonFlash.BackColor = System.Drawing.Color.White;
		this.buttonFlash.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(65, 214, 171);
		this.buttonFlash.FlatAppearance.BorderSize = 2;
		this.buttonFlash.FlatAppearance.MouseDownBackColor = System.Drawing.Color.White;
		this.buttonFlash.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(65, 214, 171);
		this.buttonFlash.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.buttonFlash.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.buttonFlash.Location = new System.Drawing.Point(471, 74);
		this.buttonFlash.Margin = new System.Windows.Forms.Padding(0, 0, 5, 0);
		this.buttonFlash.Name = "buttonFlash";
		this.buttonFlash.Size = new System.Drawing.Size(80, 30);
		this.buttonFlash.TabIndex = 7;
		this.buttonFlash.Text = "Flash";
		this.buttonFlash.UseVisualStyleBackColor = false;
		this.buttonFlash.Click += new System.EventHandler(ButtonFlash_Click);
		this.buttonCancel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(65, 214, 171);
		this.buttonCancel.FlatAppearance.BorderSize = 2;
		this.buttonCancel.FlatAppearance.MouseDownBackColor = System.Drawing.Color.White;
		this.buttonCancel.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(65, 214, 171);
		this.buttonCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.buttonCancel.Font = new System.Drawing.Font("Calibri", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.buttonCancel.Location = new System.Drawing.Point(554, 74);
		this.buttonCancel.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
		this.buttonCancel.Name = "buttonCancel";
		this.buttonCancel.Size = new System.Drawing.Size(80, 30);
		this.buttonCancel.TabIndex = 6;
		this.buttonCancel.Text = "Cancel";
		this.buttonCancel.UseVisualStyleBackColor = true;
		this.buttonCancel.Click += new System.EventHandler(ButtonCancel_Click);
		this.panel1.BackColor = System.Drawing.Color.WhiteSmoke;
		this.panel1.Controls.Add(this.chkBxTests);
		this.panel1.Controls.Add(this.labelRunTest);
		this.panel1.Controls.Add(this.labelSnTitle);
		this.panel1.Controls.Add(this.labelSn);
		this.panel1.Controls.Add(this.progressFlash);
		this.panel1.Controls.Add(this.buttonCancel);
		this.panel1.Controls.Add(this.buttonFlash);
		this.panel1.Controls.Add(this.buttonRom);
		this.panel1.Controls.Add(this.buttonShowLog);
		this.panel1.Controls.Add(this.checkBoxEraseFrp);
		this.panel1.Controls.Add(this.checkBoxReset);
		this.panel1.Controls.Add(this.labelIMEI);
		this.panel1.Controls.Add(this.checkBoxIgnorePermission);
		this.panel1.Controls.Add(this.labelIMEITitle);
		this.panel1.Controls.Add(this.labelRomPath);
		this.panel1.Controls.Add(this.buttonSku);
		this.panel1.Controls.Add(this.comboBoxSku);
		this.panel1.Controls.Add(this.labelOta);
		this.panel1.Controls.Add(this.labelPercent);
		this.panel1.Location = new System.Drawing.Point(3, 3);
		this.panel1.Name = "panel1";
		this.panel1.Size = new System.Drawing.Size(638, 109);
		this.panel1.TabIndex = 54;
		this.chkBxTests.AutoSize = true;
		this.chkBxTests.Font = new System.Drawing.Font("Calibri", 9f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.chkBxTests.Location = new System.Drawing.Point(461, 53);
		this.chkBxTests.Name = "chkBxTests";
		this.chkBxTests.Size = new System.Drawing.Size(78, 18);
		this.chkBxTests.TabIndex = 22;
		this.chkBxTests.Text = "Run Tests";
		this.chkBxTests.UseVisualStyleBackColor = true;
		this.chkBxTests.Visible = false;
		this.chkBxTests.CheckedChanged += new System.EventHandler(chkBxTests_CheckedChanged);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.Color.Silver;
		base.Controls.Add(this.panel1);
		base.Controls.Add(this.btnSetISO);
		base.Controls.Add(this.maskTxtBox);
		base.Controls.Add(this.button1);
		base.Controls.Add(this.btnCopyURL);
		base.Controls.Add(this.txtBxDownloadURL);
		base.Controls.Add(this.btnGetLatest);
		base.Controls.Add(this.txtBxFileName);
		base.Name = "DevicePanel";
		base.Size = new System.Drawing.Size(644, 172);
		base.Load += new System.EventHandler(DevicePanel_Load);
		this.panel1.ResumeLayout(false);
		this.panel1.PerformLayout();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
