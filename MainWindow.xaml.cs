#nullable enable
using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DOASCalculatorWinUI.ViewModels;

namespace DOASCalculatorWinUI
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; } = new MainViewModel();

        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = "DOAS Sizing Calculator (WinUI 3)";
        }

        private void Calculate_Click(object? sender, RoutedEventArgs? e)
        {
            ViewModel.Calculate();
        }

        private void Units_Click(object sender, RoutedEventArgs e)
        {
            var item = sender as ToggleMenuFlyoutItem;
            if (item == null) return;
            
            bool toIp = (item == MenuIP);
            ViewModel.IsIp = toIp;
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Exit();
    }
}