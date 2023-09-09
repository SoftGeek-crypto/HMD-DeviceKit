using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace hmd_pctool_windows;

public class HttpClientDownloadWithProgress : IDisposable
{
	public delegate void ProgressChangedHandler(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage);

	private readonly string _downloadUrl;

	private readonly string _destinationFilePath;

	private HttpClient _httpClient;

	public event ProgressChangedHandler ProgressChanged;

	public HttpClientDownloadWithProgress(string downloadUrl, string destinationFilePath)
	{
		_downloadUrl = downloadUrl;
		_destinationFilePath = destinationFilePath;
	}

	public async Task StartDownload()
	{
		_httpClient = new HttpClient
		{
			Timeout = TimeSpan.FromDays(1.0)
		};
		HttpResponseMessage response = await _httpClient.GetAsync(_downloadUrl, (HttpCompletionOption)1);
		try
		{
			await DownloadFileFromHttpResponseMessage(response);
		}
		finally
		{
			((IDisposable)response)?.Dispose();
		}
	}

	private async Task DownloadFileFromHttpResponseMessage(HttpResponseMessage response)
	{
		response.EnsureSuccessStatusCode();
		long? totalBytes = response.Content.Headers.ContentLength;
		using Stream contentStream = await response.Content.ReadAsStreamAsync();
		await ProcessContentStream(totalBytes, contentStream);
	}

	private async Task ProcessContentStream(long? totalDownloadSize, Stream contentStream)
	{
		long totalBytesRead = 0L;
		long readCount = 0L;
		byte[] buffer = new byte[8192];
		bool isMoreToRead = true;
		using FileStream fileStream = new FileStream(_destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true);
		do
		{
			int bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);
			if (bytesRead == 0)
			{
				isMoreToRead = false;
				TriggerProgressChanged(totalDownloadSize, totalBytesRead);
				continue;
			}
			await fileStream.WriteAsync(buffer, 0, bytesRead);
			totalBytesRead += bytesRead;
			readCount++;
			if (readCount % 100 == 0)
			{
				TriggerProgressChanged(totalDownloadSize, totalBytesRead);
			}
		}
		while (isMoreToRead);
	}

	private void TriggerProgressChanged(long? totalDownloadSize, long totalBytesRead)
	{
		if (this.ProgressChanged != null)
		{
			double? progressPercentage = null;
			if (totalDownloadSize.HasValue)
			{
				progressPercentage = Math.Round((double)totalBytesRead / (double)totalDownloadSize.Value * 100.0, 2);
			}
			this.ProgressChanged(totalDownloadSize, totalBytesRead, progressPercentage);
		}
	}

	public void Dispose()
	{
		HttpClient httpClient = _httpClient;
		if (httpClient != null)
		{
			((HttpMessageInvoker)httpClient).Dispose();
		}
	}

	public static async Task startDownload(string APP_ID, string fileFormat)
	{
		DialogResult result = MessageBox.Show("Click Yes to proceed?", "Download File", MessageBoxButtons.YesNo);
		if (result == DialogResult.Yes)
		{
			DownloadForm downloader = new DownloadForm((await AzureNativeClient.Instance.GetFileUrl(APP_ID)).Message, APP_ID, fileFormat);
			downloader.Show();
		}
	}
}
