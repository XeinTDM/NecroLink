using System;
using System.IO;
using System.Windows;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Input;
using System.Security.Policy;
using NLog;
using System.Linq;
using System.Net.Http;

namespace NecroLink
{
    public partial class MainWindow : Window
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private CancellationTokenSource cancellationTokenSource;
        private readonly FileDownloaderPool downloaderPool;
        private int successfulDownloads = 0;
        private int unsuccessfulDownloads = 0;
        private int speedLimit = 0;

        public MainWindow()
        {
            cancellationTokenSource = new CancellationTokenSource();
            downloaderPool = new FileDownloaderPool(10, 1000);
            InitializeComponent();
        }

        private readonly List<(string url, string fileName, ProgressBar progressBar)> downloadQueue = new();

        private void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            // Add selected programs to queue instead of starting download immediately
            if (ChkBrave.IsChecked == true)
                AddToDownloadQueue("https://referrals.brave.com/latest/BraveBrowserSetup-BRV002.exe", "Brave-Setup.exe", progBrave);
            if (ChkOperaGX.IsChecked == true)
                AddToDownloadQueue("https://net.geo.opera.com/opera_gx/stable/windows", "OperaGX-Setup.exe", progOperaGX);
            if (ChkChrome.IsChecked == true)
                AddToDownloadQueue("https://dl.google.com/tag/s/appguid%3D%7B8A69D345-D564-463C-AFF1-A69D9E530F96%7D%26iid%3D%7B793E9CF9-78EF-EF57-1698-3AC202E97591%7D%26lang%3Den%26browser%3D4%26usagestats%3D1%26appname%3DGoogle%2520Chrome%26needsadmin%3Dprefers%26ap%3Dx64-stable-statsdef_1%26installdataindex%3Dempty/update2/installers/ChromeSetup.exe", "Chrome-Setup.exe", progChrome);
            if (ChkFireFox.IsChecked == true)
                AddToDownloadQueue("https://download.mozilla.org/?product=firefox-stub&os=win&lang=en-US", "FireFox-Setup.exe", progFireFox);
            if (ChkDiscord.IsChecked == true)
                AddToDownloadQueue("https://discordapp.com/api/download?platform=win", "Discord-Setup.exe", progDiscord);
            if (ChkSession.IsChecked == true)
                AddToDownloadQueue("https://getsession.org/windows", "Session-Setup.exe", progSession);
            if (ChkTelegram.IsChecked == true)
                AddToDownloadQueue("https://telegram.org/dl/desktop/win64", "Telegram-Setup.exe", progTelegram);
            if (ChkGit.IsChecked == true)
                AddToDownloadQueue("https://github.com/git-for-windows/git/releases/download/v2.41.0.windows.3/Git-2.41.0.3-64-bit.exe", "Git-Setup.exe", progGit);
            if (ChkRH.IsChecked == true)
                AddToDownloadQueue("http://www.angusj.com/resourcehacker/reshacker_setup.exe", "ResourceHacker-Setup.exe", progRH);
            if (ChkOpenVPN.IsChecked == true)
                AddToDownloadQueue("https://swupdate.openvpn.org/community/releases/OpenVPN-2.6.5-I001-amd64.msi", "OpenVPN-Setup.exe", progOpenVPN);
            if (ChkPython.IsChecked == true)
                AddToDownloadQueue("https://www.python.org/ftp/python/3.10.6/python-3.10.6-amd64.exe", "Python3.10.6-Setup.exe", progPython);
            if (ChkVSCode.IsChecked == true)
                AddToDownloadQueue("https://az764295.vo.msecnd.net/stable/74f6148eb9ea00507ec113ec51c489d6ffb4b771/VSCodeUserSetup-x64-1.80.1.exe", "VSCode-Setup.exe", progVSCode);
            if (ChkVSPro.IsChecked == true)
                AddToDownloadQueue("https://c2rsetup.officeapps.live.com/c2r/downloadVS.aspx?sku=professional&channel=Release&version=VS2022&source=VSLandingPage&includeRecommended=true&cid=2030:668f016c7eb44fc2b97096aebac643a6", "VSPro2022-Setup.exe", progVSPro);
            if (ChkSpotify.IsChecked == true)
                AddToDownloadQueue("https://download.scdn.co/SpotifySetup.exe", "Spotify-Setup.exe", progSpotify);
            if (ChkSteam.IsChecked == true)
                AddToDownloadQueue("https://cdn.cloudflare.steamstatic.com/client/installer/SteamSetup.exe", "Steam-Setup.exe", progSteam);
            if (ChkWinRAR.IsChecked == true)
                AddToDownloadQueue("https://www.win-rar.com/fileadmin/winrar-versions/winrar/winrar-x64-622.exe", "WinRAR-Setup.exe", progWinRAR);
            if (ChkVMware.IsChecked == true)
                AddToDownloadQueue("https://www.vmware.com/go/getplayer-win", "VMwarePlayer-Setup.exe", progVMware);
            if (ChkProtonVPN.IsChecked == true)
                AddToDownloadQueue("https://protonvpn.com/download/ProtonVPN_v3.0.7.exe", "ProtonVPN-Setup.exe", progProtonVPN);
            if (ChkCC.IsChecked == true)
                AddToDownloadQueue("https://prod-rel-ffc-ccm.oobesaas.adobe.com/adobe-ffc-external/core/v1/wam/download?sapCode=KCCC&productName=Creative%20Cloud&os=win&guid=bb2689f2-3c0a-4f08-a500-a0d79e87cb85&contextParams=%7B%22component%22%3A%22cc-home%22%2C%22visitor_guid%22%3A%22%22%2C%22campaign_id%22%3A%2224179%7C2021-10-cme-1%7C2023-04-kaizenSSOLoggedOut%22%2C%22browser%22%3Anull%2C%22context_guid%22%3A%220a9ddeac-b918-4552-b61c-90100a669526%22%2C%22variation_id%22%3A%2267650%3A24179%7Ctest%3A2021-10-cme-1%7Ccontrol%3A2023-04-kaizenSSOLoggedOut%22%2C%22experience_id%22%3A%22na%3A24179%7Cna%3A2021-10-cme-1%7Cna%3A2023-04-kaizenSSOLoggedOut%22%2C%22planCodeList%22%3A%22%22%2C%22updateCCD%22%3A%22true%22%2C%22secondarySapcodeList%22%3A%22%22%2C%22Product_ID_Promoted%22%3A%22KCCC%22%2C%22contextComName%22%3A%22Organic%3ACCH%22%2C%22contextSvcName%22%3A%22NO-WAM%22%2C%22contextOrigin%22%3A%22Organic%3ACCH%22%2C%22AMCV_9E1005A551ED61CA0A490D45%2540AdobeOrg%22%3A%22MCMID%7C80876646249214972340621457338339170611%22%2C%22mid%22%3A%22%22%2C%22aid%22%3A%22%22%2C%22AppMeasurementVersion%22%3A%222.23.0%22%2C%22kaizenTrialDuration%22%3A7%7D&wamFeature=nuj-live&environment=prod&api_key=CCHomeWeb1", "CreativeCloud-Setup.exe", progCC);
            if (ChkFigma.IsChecked == true)
                AddToDownloadQueue("https://www.figma.com/download/desktop/win", "Figma-Setup.exe", progFigma);


            StartNextDownload();
            CleanUpTempFiles();
        }

        private void AddToDownloadQueue(string url, string fileName, ProgressBar progressBar)
        {
            downloadQueue.Add((url, fileName, progressBar));
            lstDownloadQueue.Items.Add(fileName); // Display the file name in the queue
        }

        private async void StartNextDownload()
        {
            // Get the path to the desktop folder
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            // Combine with the "programs" folder name
            string programsFolderPath = Path.Combine(desktopPath, "programs");

            // Create the folder if it doesn't exist
            Directory.CreateDirectory(programsFolderPath);

            if (downloadQueue.Count == 0) return;

            // Specify maximum number of concurrent downloads
            const int maxConcurrentDownloads = 3;

            // Prepare a list of tasks
            var downloadTasks = downloadQueue.Select(item => DownloadItemAsync(item)).ToList();
            await Task.WhenAll(downloadTasks);

            while (downloadQueue.Count > 0 && downloadTasks.Count < maxConcurrentDownloads)
            {
                var (url, fileName, progressBar) = downloadQueue[0];
                downloadQueue.RemoveAt(0);
                lstDownloadQueue.Items.RemoveAt(0); // Remove the file name from the queue

                var downloader = new FileDownloader(speedLimit);
                downloader.ProgressChanged += (s, e) =>
                {
                    progressBar.Value = e.ProgressPercentage;
                };

                // Add this code at the end of the StartNextDownload method, after the while loop
                if (downloadQueue.Count == 0 && downloadTasks.Count == 0)
                {
                    MessageBox.Show($"Downloads completed. Successful: {successfulDownloads}, Unsuccessful: {unsuccessfulDownloads}");
                }

                downloadTasks.Add(downloader.TryDownloadAsync(url, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Programs", fileName), cancellationTokenSource.Token));
            }

            // When any task completes, remove it from the list and start a new download if any remain
            while (downloadTasks.Count > 0)
            {
                var completedTask = await Task.WhenAny(downloadTasks);
                downloadTasks.Remove(completedTask);

                if (downloadQueue.Count > 0)
                {
                    var (url, fileName, progressBar) = downloadQueue[0];
                    downloadQueue.RemoveAt(0);
                    lstDownloadQueue.Items.RemoveAt(0); // Remove the file name from the queue

                    var downloader = new FileDownloader(speedLimit);
                    downloader.ProgressChanged += (s, e) =>
                    {
                        progressBar.Value = e.ProgressPercentage;
                    };

                    downloadTasks.Add(downloader.TryDownloadAsync(url, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Programs", fileName), cancellationTokenSource.Token));
                }
                if (downloadTasks.Count == 0)
                {
                    ShowResultsNonBlocking($"Downloads completed. Successful: {successfulDownloads}, Unsuccessful: {unsuccessfulDownloads}");
                }
            }
        }

        private async Task DownloadItemAsync((string url, string fileName, ProgressBar progressBar) item)
        {
            var (url, fileName, progressBar) = item;

            DownloadResult result = await DownloadFileAsync(url, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Programs", fileName), progressBar);

            if (result.Success)
            {
                successfulDownloads++;
                Logger.Info($"Successfully downloaded {fileName}");
            }
            else
            {
                unsuccessfulDownloads++;
                Logger.Error($"Failed to download {fileName}. Error: {result.ErrorMessage}");
            }
        }

        public async Task<DownloadResult> DownloadFileAsync(string url, string destinationPath, ProgressBar progressBar)
        {
            FileDownloader downloader = downloaderPool.Rent();
            try
            {
                using var httpClient = new HttpClient();
                using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var totalReadBytes = 0L;
                var buffer = new byte[8192];
                var isMoreToRead = true;

                do
                {
                    var read = await response.Content.ReadAsByteArrayAsync();
                    totalReadBytes += read.Length;

                    await fileStream.WriteAsync(read);

                    if (totalBytes != -1)
                    {
                        var progressPercentage = ((double)totalReadBytes / totalBytes) * 100;
                        progressBar.Value = progressPercentage;
                    }

                    isMoreToRead = read.Length != 0;
                }
                while (isMoreToRead);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Download successful");
                    return new DownloadResult { Success = true };
                }
                else
                {
                    var errorMessage = $"Download failed: {response.StatusCode}";
                    Console.WriteLine(errorMessage);
                    return new DownloadResult { Success = false, ErrorMessage = errorMessage };
                }
            }
            finally
            {
                downloaderPool.Return(downloader);
            }
        }

        private static void ShowResultsNonBlocking(string message)
        {
            var resultsWindow = new ResultsWindow();
            resultsWindow.ShowMessage(message);
            resultsWindow.Show();
        }

        public static void CleanUpTempFiles()
        {
            // Get the path to the Temp directory
            string tempDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp");

            // Get a collection of all files in the Temp directory
            IEnumerable<string> tempFiles = Directory.EnumerateFiles(tempDirectory);

            // Create a ParallelOptions object that specifies the maximum degree of parallelism
            ParallelOptions options = new() { MaxDegreeOfParallelism = 4 }; // Adjust this number to your needs

            // Use Parallel.ForEach with the ParallelOptions object
            Parallel.ForEach(tempFiles, options, tempFile =>
            {
                try
                {
                    // Delete the file
                    File.Delete(tempFile);
                }
                catch (Exception ex)
                {
                    // Handle exceptions
                    Console.WriteLine($"Failed to clean up temp file {tempFile}: {ex.Message}");
                }
            });
        }

        private void ListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem listBoxItem)
            {
                DragDrop.DoDragDrop(listBoxItem, listBoxItem.DataContext, DragDropEffects.Move);
            }
        }

        private void ListBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(string)))
            {
                string? droppedData = e.Data.GetData(typeof(string)) as string;
                ListBox target = (ListBox)sender;

                int removedIdx = lstDownloadQueue.Items.IndexOf(droppedData);

                // Manage your actual data
                var removedItem = downloadQueue[removedIdx];
                downloadQueue.RemoveAt(removedIdx);
                downloadQueue.Insert(target.SelectedIndex, removedItem);

                // Manage the UI
                lstDownloadQueue.Items.RemoveAt(removedIdx);
                lstDownloadQueue.Items.Insert(target.SelectedIndex, droppedData);
            }
        }

        private void BtnApplyLimit_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtSpeedLimit.Text, out int limit))
            {
                // Convert from B/s to mb/s
                double speedLimitInMbps = (double)limit / 1048576;

                // Update the speedLimit variable with the value in Mbps
                speedLimit = (int)(speedLimitInMbps * 1024 * 1024); // Convert back to B/s
            }
        }

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            // Select all checkboxes
            ChkBrave.IsChecked = ChkOperaGX.IsChecked = ChkChrome.IsChecked = ChkFireFox.IsChecked = ChkDiscord.IsChecked = ChkSession.IsChecked = ChkTelegram.IsChecked = ChkGit.IsChecked = ChkRH.IsChecked = ChkOpenVPN.IsChecked = ChkPython.IsChecked = ChkVSCode.IsChecked = ChkVSPro.IsChecked = ChkSpotify.IsChecked = ChkSteam.IsChecked = ChkWinRAR.IsChecked = ChkVMware.IsChecked = ChkProtonVPN.IsChecked = ChkCC.IsChecked = ChkFigma.IsChecked = true;
        }

        private void BtnDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            // Deselect all checkboxes
            ChkBrave.IsChecked = ChkOperaGX.IsChecked = ChkChrome.IsChecked = ChkFireFox.IsChecked = ChkDiscord.IsChecked = ChkSession.IsChecked = ChkTelegram.IsChecked = ChkGit.IsChecked = ChkRH.IsChecked = ChkOpenVPN.IsChecked = ChkPython.IsChecked = ChkVSCode.IsChecked = ChkVSPro.IsChecked = ChkSpotify.IsChecked = ChkSteam.IsChecked = ChkWinRAR.IsChecked = ChkVMware.IsChecked = ChkProtonVPN.IsChecked = ChkCC.IsChecked = ChkFigma.IsChecked = false;
        }

        private void BtnCancelDownload_Click(object sender, RoutedEventArgs e)
        {
            // Cancel the download
            cancellationTokenSource.Cancel();
            // Create a new CancellationTokenSource for the next download
            cancellationTokenSource = new CancellationTokenSource();
        }

        private void ChkBrave_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ChkOperaGX_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ChkChrome_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ChkFireFox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ChkDiscord_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ChkSession_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ChkTelegram_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ChkGit_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ChkRH_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ChkOpenVPN_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ChkPython_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ChkVSCode_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ChkVCPro_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ChkSpotify_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ChkSteam_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ChkWinRAR_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ChkVMware_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ChkCC_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ChkFigma_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}