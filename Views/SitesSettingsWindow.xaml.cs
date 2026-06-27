using System;
using System.Windows;
using OpenWithTool.ViewModels;

namespace OpenWithTool.Views;

public partial class SitesSettingsWindow : Window
{
    private readonly SitesSettingsWindowViewModel _viewModel;

    public SitesSettingsWindow(SitesSettingsWindowViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        _viewModel.RequestClose += () => Close();
        Loaded += SitesSettingsWindow_Loaded;
    }

    private async void SitesSettingsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Error loading sites settings: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
