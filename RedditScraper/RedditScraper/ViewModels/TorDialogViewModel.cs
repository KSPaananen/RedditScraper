using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using ReactiveUI;
using RedditScraper.AppSettings;
using RedditScraper.Services;
using RedditScraper.Services.Interfaces;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Application = Avalonia.Application;

namespace RedditScraper.ViewModels
{
    public class TorDialogViewModel : INotifyPropertyChanged
    {
        private IFileService _fileService;

        public ICommand Download { get; }
        public ICommand Cancel { get; }
        public ICommand OpenLink { get; }

        public TorDialogViewModel()
        {
            _fileService = new FileService();

            Download = ReactiveCommand.Create(() =>
            {
                DownloadAndInstallTor();
            });

            Cancel = ReactiveCommand.Create(() =>
            {
                // This is terrible, but works for now
                var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.Windows : null;

                mainWindow![1].Close();

            });

            OpenLink = ReactiveCommand.Create(() =>
            {
                // Start browser with target website
                System.Diagnostics.Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = Link });
            });
        }

        public string Title { get; set; } = $"Oops!";
        public string Link { get; set; } = $"www.torproject.org/";
        public string Body1 { get; set; } = $"Looks like Tor's not installed on your machine or the program hasn't located it yet! {Environment.NewLine}{Environment.NewLine}You can download Tor from";
        public string Body2 { get; set; } = $"or click {@"""Download & install"""} to automatically install it.";
        public string InstallState { get; set; } = "";

        private int progressValue;

        public int ProgressValue
        {
            get => progressValue;
            set
            {
                if (value == 0)
                {
                    // Hide progress bar
                }

                progressValue = value;

                Dispatcher.UIThread.InvokeAsync(() => NotifyPropertyChanged(nameof(ProgressValue)));
            }
        }

        private bool feedBackVisibility;
        public bool FeedBackVisibility
        {
            get => feedBackVisibility;
            set
            {
                feedBackVisibility = value;

                Dispatcher.UIThread.InvokeAsync(() => NotifyPropertyChanged(nameof(ProgressValue)));
            }
        }

        public async void DownloadAndInstallTor()
        {
            // Reveal feedback elements
            FeedBackVisibility = true;

            // Update GUI values
            InstallState = "Downloading";
            AnimateProgressBar(0);

            // Download installer
            string url = "https://www.torproject.org/dist/torbrowser/13.0.15/tor-browser-windows-x86_64-portable-13.0.15.exe";
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "torbrowser-install.exe");

            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage result = await client.GetAsync(url))
                {
                    result.EnsureSuccessStatusCode();

                    using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await result.Content.CopyToAsync(fs);
                    }
                }
            }

            // Update status message on dialog
            InstallState = "Installing";
            AnimateProgressBar(50);

            // Run installer
            System.Diagnostics.Process.Start(path);

            // Clean temp files
            await Task.Run(() =>
            {
                // Wait for installer to finish
                while (Process.GetProcessesByName("torbrowser-install").Length > 0)
                {
                    Thread.Sleep(3000);
                }

                InstallState = "Cleaning temp files";
                AnimateProgressBar(75);

                // Clean temp files
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            });

            // Update status message on dialog
            InstallState = "Done";
            AnimateProgressBar(100);

            // Update tor path in settings
            Settings settings = SettingsManager.ReadSettings();
            settings.Enviroment.TorPath = _fileService.LocateTor();
            SettingsManager.SaveSettings(settings);

            // Hide feedback element
            FeedBackVisibility = false;
        }

        private async void AnimateProgressBar(int value)
        {
            await Task.Run(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                for (int i = ProgressValue; i != value; i++)
                {
                    ProgressValue = i;
                    Thread.Sleep(100);
                }
            });

        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }
}
