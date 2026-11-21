using System.Windows;
using Hmi.ViewModels;

namespace Hmi.Wpf;

/// <summary>应用主窗口：承载左侧导航、页面内容与底部状态栏</summary>
public partial class MainWindow : Window
{
    public MainWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
