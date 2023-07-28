using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace NecroLink
{
    public class DownloadManager
    {
        private readonly FileDownloaderPool downloaderPool;

        public DownloadManager(int poolSize, int speedLimit)
        {
            downloaderPool = new FileDownloaderPool(poolSize, speedLimit);
        }

        public async Task<DownloadResult> DownloadFileAsync(string url, string destinationPath, ProgressBar progressBar)
        {
            FileDownloader downloader = downloaderPool.Rent();
            try
            {
                var progress = new Progress<int>(value => progressBar.Dispatcher.Invoke(() => progressBar.Value = value));
                return await downloader.TryDownloadAsync(url, destinationPath, CancellationToken.None, progress);
            }
            finally
            {
                downloaderPool.Return(downloader);
            }
        }
    }
}
