﻿using NLog;
using System;
using System.IO;
using NecroLink;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;

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
        }

        public async Task<DownloadResult> TryDownloadAsync(string url, string destinationPath, CancellationToken cancellationToken)
        {
            var result = new DownloadResult();

            try
            {
                Logger.Info($"Starting download from {url}");

                // Create a buffer pool
                BufferPool pool = new BufferPool(8192, 100);
                byte[] buffer = pool.Rent();

                using (var client = new HttpClient(new HttpClientHandler() { AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate }, false))
                using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                using (var streamToReadFrom = await response.Content.ReadAsStreamAsync())
                using (var streamToWriteTo = File.Open(destinationPath, FileMode.Create))
                {
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

                            // Return the old buffer to the pool and rent a new one
                            pool.Return(buffer);
                            buffer = pool.Rent();
                            ProgressChanged?.Invoke(this, new ProgressChangedEventArgs((int)((double)totalBytesRead / totalBytes * 100), null));
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
                    // Trigger the ProgressChanged event one final time after the download has completed
                    ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(100, null));
                    Logger.Info($"Finished download from {url}. Total time: {stopwatch.Elapsed.TotalSeconds} seconds");
                    result.Success = true;
                }

            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                Logger.Error(ex, $"Download from {url} failed");
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
        public string ErrorMessage { get; set; }
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