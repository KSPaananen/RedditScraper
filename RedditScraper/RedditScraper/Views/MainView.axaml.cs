using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using RedditScraper.AppSettings;
using RedditScraper.Services;
using RedditScraper.ViewModels;
using System;
using System.Collections.Generic;

namespace RedditScraper.Views;

public partial class MainView : UserControl
{
    private MainViewModel mainViewModel;

    public MainView()
    {
        InitializeComponent();

        this.DataContext = new MainViewModel();
        mainViewModel = (this.DataContext as MainViewModel)!;

    }
}
