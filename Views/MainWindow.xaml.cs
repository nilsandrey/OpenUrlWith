using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using OpenWithTool.ViewModels;

namespace OpenWithTool.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;

    public MainWindow(MainWindowViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
        DataContext = _viewModel;

        _viewModel.RequestClose += () => Close();
        _viewModel.OpenSettings += OpenSettingsWindow;
        _viewModel.OpenSitesSettings += OpenSitesSettingsWindow;
        _viewModel.RequestListFocus += () => FocusBrowserList();
        
        // Ensure the browser list gets focus when the window is loaded
        Loaded += (s, e) => FocusBrowserList();
    }

    private void FocusBrowserList()
    {
        // Set focus to the browser list for immediate keyboard navigation
        var listBox = FindName("BrowserListBox") as System.Windows.Controls.ListBox;
        listBox?.Focus();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        _viewModel.OnUserInteraction();
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        _viewModel.OnUserInteraction();
    }

    private void OpenSitesSettingsWindow()
    {
        try
        {
            var sitesSettingsWindow = _serviceProvider.GetRequiredService<SitesSettingsWindow>();
            sitesSettingsWindow.Owner = this;
            sitesSettingsWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening sites settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenSettingsWindow()
    {
        try
        {
            var settingsWindow = _serviceProvider.GetRequiredService<SettingsWindow>();
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening settings: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}