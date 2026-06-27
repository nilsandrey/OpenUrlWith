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
        Loaded += async (_, _) => await _viewModel.InitializeAsync();
    }
}
