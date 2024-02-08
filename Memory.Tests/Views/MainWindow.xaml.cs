using System;
using System.Windows;
using Memory.Tests.ViewModels;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace Memory.Tests.Views;

public partial class MainWindow : INavigationWindow
{
    public MainWindowViewModel ViewModel { get; }
    
    public MainWindow(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this);
        InitializeComponent();
    }

    #region INavigationWindow Methods
    
    public INavigationView GetNavigation()
    {
        throw new NotImplementedException();
    }

    public bool Navigate(Type pageType)
    {
        throw new NotImplementedException();
    }

    public void SetServiceProvider(IServiceProvider serviceProvider)
    {
        throw new NotImplementedException();
    }

    public void SetPageService(IPageService pageService)
    {
        throw new NotImplementedException();
    }

    public void ShowWindow()
    {
        Show();
    }

    public void CloseWindow()
    {
        Close();
        Application.Current.Shutdown();
    }
    
    #endregion
}