#nullable enable
using System;
using System.Collections.Generic;
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

        private async void Open_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".json");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                try
                {
                    string json = await Windows.Storage.FileIO.ReadTextAsync(file);
                    var data = System.Text.Json.JsonSerializer.Deserialize<ProjectData>(json);
                    if (data != null)
                    {
                        ViewModel.LoadProject(data);
                    }
                }
                catch (Exception ex)
                {
                    // Basic error handling
                }
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileSavePicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add("DOAS Project", new List<string>() { ".json" });
            picker.SuggestedFileName = "New DOAS Project";

            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                var data = ViewModel.GetProjectData();
                string json = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                await Windows.Storage.FileIO.WriteTextAsync(file, json);
            }
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