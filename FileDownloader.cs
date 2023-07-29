using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NecroLink
{
    public class FileDownloader
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;
        public event EventHandler<DownloadCompletedEventArgs> DownloadCompleted;
        private readonly int _speedLimit;

        public FileDownloader(int speedLimit)
        {
            _speedLimit = speedLimit;
            ProgressChanged += (sender, e) => { };
            DownloadCompleted += (sender, e) => { };
        }

public async Task<DownloadResult> TryDownloadAsync(string url, string destinationPath, CancellationToken cancellationToken, IProgress<int> progress)
{
    var result = new DownloadResult();
    int retryCount = 0;

    while (retryCount < 3) // Maximum of 3 retries
    {
        try
        {
            Logger.Info($"Starting download from {url}");

            BufferPool pool = new (8192, 100);
            byte[] buffer = pool.Rent();

            var client = new HttpClient(new HttpClientHandler() { AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate }, false);
            var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            var streamToReadFrom = await response.Content.ReadAsStreamAsync(cancellationToken);
            var streamToWriteTo = File.Open(destinationPath, FileMode.Create);

            var totalBytes = response.Content.Headers.ContentLength.GetValueOrDefault();
            var totalBytesRead = 0L;
            var bytesRead = 0;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            while ((bytesRead = await streamToReadFrom.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) != 0)
            {
                await streamToWriteTo.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                totalBytesRead += bytesRead;

                if (stopwatch.ElapsedMilliseconds > 1000)
                {
                    var speed = totalBytesRead / stopwatch.Elapsed.TotalSeconds;
                    Logger.Info($"Download speed: {speed} bytes/second");

                    pool.Return(buffer);
                    buffer = pool.Rent();

                    progress?.Report((int)((double)totalBytesRead / totalBytes * 100));

                    stopwatch.Restart();

                    if (_speedLimit > 0)
                    {
                        var delay = (int)(totalBytesRead / (_speedLimit / 1000.0));
                        await Task.Delay(delay, cancellationToken);
                    }

                    stopwatch.Restart();
                }
            }

            pool.Return(buffer);

            ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(100, null));

            Logger.Info($"Finished download from {url}. Total time: {stopwatch.Elapsed.TotalSeconds} seconds");

            result.Success = true;

            // If the download is successful, break the loop
            if (result.Success)
            {
                break;
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            Logger.Error(ex, $"Download from {url} failed. Attempt: {retryCount + 1}");
        }

        // If the download failed, increment the retry count and try again
        if (!result.Success)
        {
            retryCount++;
            await Task.Delay(2000); // Wait for 2 seconds before retrying
        }
    }

    // If after 3 attempts the download is still unsuccessful, you might want to skip to the next download
    if (!result.Success && retryCount >= 3)
    {
        Logger.Error($"Download from {url} failed after 3 attempts. Skipping to next download.");
    }

    DownloadCompleted?.Invoke(this, new DownloadCompletedEventArgs(result));
    return result;
}

    }

    public class FileDownloaderPool
    {
        private readonly Stack<FileDownloader> pool;

        public FileDownloaderPool(int poolSize, int speedLimit)
        {
            this.pool = new Stack<FileDownloader>(poolSize);

            for (int i = 0; i < poolSize; i++)
            {
                pool.Push(new FileDownloader(speedLimit));
            }
        }

        public FileDownloader Rent()
        {
            if (pool.Count > 0)
            {
                return pool.Pop();
            }
            else
            {
                // If the pool is empty, create a new FileDownloader
                // You could also throw an exception or block until a FileDownloader is returned to the pool
                return new FileDownloader(0);
            }
        }

        public void Return(FileDownloader fileDownloader)
        {
            pool.Push(fileDownloader);
        }
    }

    public class DownloadResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class DownloadCompletedEventArgs : EventArgs
    {
        public DownloadResult Result { get; }

        public DownloadCompletedEventArgs(DownloadResult result)
        {
            Result = result;
        }
    }

    public class ProgressChangedEventArgs : EventArgs
    {
        public int ProgressPercentage { get; set; }
        public object UserState { get; set; }

        public ProgressChangedEventArgs(int progressPercentage, object userState)
        {
            ProgressPercentage = progressPercentage;
            UserState = userState;
        }
    }
}