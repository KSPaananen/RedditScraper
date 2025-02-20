using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using RedditScraper.AppSettings;
using RedditScraper.Services;
using RedditScraper.Services.Interfaces;
using RedditScraper.ViewModels;
using System.Diagnostics;

namespace RedditScraper.Views;

public partial class MainWindow : Window
{
    private IAppService _appService;

    private bool mouseDown;
    private bool allowMoving;
    private PointerPoint originalPoint;

    public MainWindow()
    {
        _appService = new AppService();

        mouseDown = false;
        allowMoving = true;

        // Run startup method
        _appService.OnStartUp();

        // Get settings from appmanager
        Settings settings = SettingsManager.ReadSettings();

        // Initialize window and set properties
        InitializeComponent();

        this.DataContext = new MainWindowModel();

        // Set custom size & startup position
        if (WindowState != WindowState.Maximized && WindowState != WindowState.FullScreen)
        {
            // Get client values
            var clientSize = new Avalonia.Size(settings.Application.ClientSize.X, settings.Application.ClientSize.Y);
            var clientPosition = new PixelPoint(settings.Application.ClientPosition.X, settings.Application.ClientPosition.Y);

            // Set application properties
            ClientSize = clientSize;
            Position = clientPosition;
        }
        
    }

    private void DockPanel_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!mouseDown) return;

        PointerPoint currentPoint = e.GetCurrentPoint(this);
        Position = new PixelPoint(Position.X + (int)(currentPoint.Position.X - originalPoint.Position.X),
        Position.Y + (int)(currentPoint.Position.Y - originalPoint.Position.Y));
    }

    private void DockPanel_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // For some reason window can be dragged from Labels, so invalidate clicks above the height of 30
        var yPos = e.GetCurrentPoint(this).Position.Y;

        if (WindowState == WindowState.Maximized || WindowState == WindowState.FullScreen || allowMoving == false || yPos > 30) return;

        mouseDown = true;
        originalPoint = e.GetCurrentPoint(this);
    }

    private void DockPanel_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        mouseDown = false;
    }

    // Put these two methods on all menu lists & items to avoid weird behaviour
    private void MenuItem_PointerEntered(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        allowMoving = false;
    }

    private void MenuItem_PointerExited(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        allowMoving = true;
    }

    private void MenuItem_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var button = (sender as MenuItem);

        if (button == null)
            return;

        switch (button.Name)
        {
            case "BtnResetWindow":
                this.ClientSize = new Avalonia.Size(1000, 600);
                break;
            case "BtnGithub":
                // Set target website
                string target = "https://github.com/KSPaananen/RedditScraper";

                // Start browser with target website
                System.Diagnostics.Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = target });
                break;
        }

    }

    private void Window_Closing(object? sender, Avalonia.Controls.WindowClosingEventArgs e)
    {
        Settings settings = SettingsManager.ReadSettings();

        // Save client position & size to settings
        settings.Application.ClientPosition.X = Position.X;
        settings.Application.ClientPosition.Y = Position.Y;

        settings.Application.ClientSize.X = (int)ClientSize.Width;
        settings.Application.ClientSize.Y = (int)ClientSize.Height;

        SettingsManager.SaveSettings(settings);
    }


}
