using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using ReactiveUI;
using RedditScraper.AppSettings;
using RedditScraper.Services;
using RedditScraper.Services.Interfaces;
using RedditScraper.Views;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RedditScraper.ViewModels;
public class MainViewModel : INotifyPropertyChanged
{
    private Task? task { get; set; }
    public Settings settings { get; set; }
    public ICommand Start { get; }
    public ICommand Data { get; }
    public ICommand BrowserMode { get; }
    public ICommand UseTor { get; }
    public ICommand UseDB { get; }
    public ICommand UseCSV { get; }

    public MainViewModel()
    {
        settings = SettingsManager.ReadSettings();

        // Read saved values from file
        SetupGuiValues();

        // Create cancellation token for Start commands Task
        CancellationTokenSource cts = new CancellationTokenSource();
        CancellationToken ct = cts.Token;

        Start = ReactiveCommand.Create(() =>
        {
            IScraperService scraper = new ScraperService(this);

            if (PostLink != "" && ProcessRunning == false)
            {
                task = Task.Run(async () =>
                {
                    // Set ProcessRunning true to show "Stop" button
                    ProcessRunning = true;

                    // Scrape with user provided values
                    await scraper.Scrape(PostLink, ct);

                }, cts.Token);
            }
            else if (task != null && ProcessRunning == true)
            {
                // Send cancellation request & create bew CancellationTokenSource
                // Re-using old cts just wont let the task run again
                cts.Cancel();
                cts = new CancellationTokenSource();

                // Dispose task
                task.Dispose();

                // Provide feedback to the front end
                ConsoleContent = $"Process cancelled";

                // Set ProcessRunning to false
                ProcessRunning = false;

            }
            else if (PostLink == "")
            {
                ConsoleContent = "Link to a post required";
            }
        });

        Data = ReactiveCommand.Create(() =>
        {
            // Open data folder using file explorer
            System.Diagnostics.Process.Start("explorer.exe", $"{AppDomain.CurrentDomain.BaseDirectory}Data");
        });

        BrowserMode = ReactiveCommand.Create(() =>
        {
            // Refresh settings object
            settings = SettingsManager.ReadSettings();
        });

        UseTor = ReactiveCommand.Create(() =>
        {
            // Refresh settings object
            settings = SettingsManager.ReadSettings();
        });

        UseDB = ReactiveCommand.Create(() =>
        {
            // Refresh settings object
            settings = SettingsManager.ReadSettings();
        });

        UseCSV = ReactiveCommand.Create(() =>
        {
            // Refresh settings object
            settings = SettingsManager.ReadSettings();
        });
    }

    private bool browserModeChecked;
    public bool BrowserModeChecked
    {
        get => browserModeChecked;
        set
        {
            browserModeChecked = value;

            if (value == true)
                settings.Scraper.BrowserMode = "Desktop";
            else
                settings.Scraper.BrowserMode = "Mobile";

            SettingsManager.SaveSettings(settings);
        }
    }

    private bool useTorChecked;
    public bool UseTorChecked
    {
        get => useTorChecked;
        set
        {
            useTorChecked = value;

            // Prevent value from being true if tors location path is not in settings
            if (settings.Enviroment.TorPath == "" || settings.Enviroment.TorPath == null)
            {
                useTorChecked = false;

                // Display dialog about tor
                CreateDialog();

                return;
            }

            settings.Scraper.Proxy.UseProxy = useTorChecked;

            SettingsManager.SaveSettings(settings);
        }
    }

    private bool useCSVChecked = false;
    public bool UseCSVChecked
    {
        get => useCSVChecked;
        set
        {
            useCSVChecked = value;

            settings.Scraper.Data.UseCSV = useCSVChecked;
            SettingsManager.SaveSettings(settings);
        }
    }

    private bool useDbChecked = false;
    public bool UseDbChecked
    {
        get => useDbChecked;
        set
        {
            useDbChecked = value;

            settings.Scraper.Data.UseMongoDB = useDbChecked;
            SettingsManager.SaveSettings(settings);

            ConnectionStringVisibility = useDbChecked;
        }
    }

    private bool connectionStringVisibility = false;
    public bool ConnectionStringVisibility
    {
        get => connectionStringVisibility;
        set
        {
            connectionStringVisibility = value;

            Dispatcher.UIThread.Post(() => NotifyPropertyChanged(nameof(ConnectionStringVisibility)), DispatcherPriority.MaxValue);
            var result = Dispatcher.UIThread.InvokeAsync(() => NotifyPropertyChanged(nameof(ConnectionStringVisibility)), DispatcherPriority.MaxValue);
        }
    }

    private string connectionString = "";
    public string ConnectionString
    {
        get => connectionString;
        set
        {
            connectionString = value;
            settings.Enviroment.ConnectionString = connectionString;
            SettingsManager.SaveSettings(settings);
        }
    }

    private string separator = ";";
    public string Separator
    {
        get => separator;
        set
        {
            // Separator should only be one character long
            if (value == null || value.Length > 1)
                return;

            separator = value;

            if (value.Length != 0)
            {
                // Save to settings
                settings = SettingsManager.ReadSettings();
                settings.Scraper.Separator = separator;
                SettingsManager.SaveSettings(settings);
            }
        }
    }

    private string postLink = "";
    public string PostLink
    {
        get => postLink;
        set
        {
            postLink = value;
        }

    }

    private string consoleContent = "";
    public string ConsoleContent
    {
        get => consoleContent;
        set
        {
            if (value == null)
                return;

            consoleContent += $"{DateTime.Now.ToString("HH:mm:ss")} > {value}{Environment.NewLine}";

            Dispatcher.UIThread.Post(() => NotifyPropertyChanged(nameof(ConsoleContent)), DispatcherPriority.MaxValue);
            var result = Dispatcher.UIThread.InvokeAsync(() => NotifyPropertyChanged(nameof(ConsoleContent)), DispatcherPriority.MaxValue);
        }
    }

    private bool processRunning = false;
    public bool ProcessRunning
    {
        get => processRunning;
        set
        {
            processRunning = value;

            Dispatcher.UIThread.Post(() => NotifyPropertyChanged(nameof(ProcessRunning)), DispatcherPriority.MaxValue);
            var result = Dispatcher.UIThread.InvokeAsync(() => NotifyPropertyChanged(nameof(ProcessRunning)), DispatcherPriority.MaxValue);
        }
    }

    private async void CreateDialog()
    {
        // Prompt user with window to install tor
        if (Avalonia.Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            DialogWindow dialog = new DialogWindow();
            await dialog.ShowDialog(desktop.MainWindow!);
        }
    }

    private void SetupGuiValues()
    {
        Settings settings = SettingsManager.ReadSettings();

        // Browsermode button
        if (settings.Scraper.BrowserMode == "Desktop" && settings.Enviroment.TorPath != "")
            this.BrowserModeChecked = true;
        else
            this.BrowserModeChecked = false;

        this.UseTorChecked = settings.Scraper.Proxy.UseProxy;

        this.UseCSVChecked = settings.Scraper.Data.UseCSV;

        this.UseDbChecked = settings.Scraper.Data.UseMongoDB;

        this.Separator = settings.Scraper.Separator;

        this.ConnectionString = settings.Enviroment.ConnectionString;
    }

    // Update UI everytime a property changes
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


}
