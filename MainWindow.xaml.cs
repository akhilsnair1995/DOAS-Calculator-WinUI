#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace DOASCalculatorWinUI
{
    public sealed partial class MainWindow : Window
    {
        private bool _isInternalUpdate = false;
        private bool _isIp = false;

        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = "DOAS Sizing Calculator (WinUI 3)";
            Calculate_Click(null, null);
        }

        private void Input_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isInternalUpdate) Calculate_Click(null, null);
        }

        private void Input_Changed(object sender, object e)
        {
            if (!_isInternalUpdate) Calculate_Click(null, null);
        }

        private void Calculate_Click(object sender, RoutedEventArgs? e)
        {
            if (CmbSeason == null) return;

            try
            {
                var inputs = new SystemInputs
                {
                    IsHeatingMode = CmbSeason.SelectedIndex == 1,
                    Altitude = _isIp ? Units.FtToM(NumAltitude.Value) : NumAltitude.Value,
                    OaFlow = _isIp ? Units.CfmToLps(NumOaFlow.Value) : NumOaFlow.Value,
                    OaDb = _isIp ? Units.FtoC(NumOaDb.Value) : NumOaDb.Value,
                    OaWb = _isIp ? Units.FtoC(NumOaWb.Value) : NumOaWb.Value,
                    EaFlow = _isIp ? Units.CfmToLps(NumEaFlow.Value) : NumEaFlow.Value,
                    EaDb = _isIp ? Units.FtoC(NumEaDb.Value) : NumEaDb.Value,
                    EaRh = NumEaRh.Value,
                    WheelEnabled = TogWheel.IsOn,
                    WheelSens = NumWheelSens.Value,
                    WheelLat = NumWheelLat.Value,
                    EconomizerEnabled = ChkEconomizer.IsChecked == true,
                    OffCoilTemp = _isIp ? Units.FtoC(NumOffCoil.Value) : NumOffCoil.Value,
                    ReheatEnabled = TogReheat.IsOn,
                    TargetSupplyTemp = _isIp ? Units.FtoC(NumSupplyTemp.Value) : NumSupplyTemp.Value,
                    SupOaEsp = _isIp ? NumSupEsp.Value / 0.00401463 : NumSupEsp.Value, // Simplified Pa/InWg
                    ExtEaEsp = _isIp ? NumExtEsp.Value / 0.00401463 : NumExtEsp.Value,
                    FanEff = NumFanEff.Value
                };

                var results = DOASEngine.Process(inputs);
                UpdateUI(results);
            }
            catch { }
        }

        private void UpdateUI(SystemResults results)
        {
            if (_isIp)
            {
                ResCooling.Text = $"{Units.KwToMbh(results.TotalCooling):F1} MBH";
                ResHeating.Text = $"{Units.KwToMbh(results.TotalHeating):F1} MBH";
                ResReheat.Text = $"{Units.KwToMbh(results.ReheatLoad):F1} MBH";
                ResFanPower.Text = $"{results.TotalFanPowerKW:F2} kW";
            }
            else
            {
                ResCooling.Text = $"{results.TotalCooling:F1} kW";
                ResHeating.Text = $"{results.TotalHeating:F1} kW";
                ResReheat.Text = $"{results.ReheatLoad:F1} kW";
                ResFanPower.Text = $"{results.TotalFanPowerKW:F2} kW";
            }

            // Populate Schedule
            var scheduleData = new List<object>();
            for (int i = 0; i < results.Steps.Count; i++)
            {
                var step = results.Steps[i];
                scheduleData.Add(new
                {
                    Index = i + 1,
                    Component = step.Component,
                    Entering = _isIp ? step.Entering.ToIpString() : step.Entering.ToString(),
                    Leaving = _isIp ? step.Leaving.ToIpString() : step.Leaving.ToString()
                });
            }
            GridSchedule.ItemsSource = scheduleData;

            DrawChart(results);
        }

        private void DrawChart(SystemResults results)
        {
            ChartCanvas.Children.Clear();

            Point Map(double t, double w)
            {
                double valW = w * 1000.0;
                float xB = 196.5f + (float)t * 12.53f;
                float xT = 162.45f + (float)t * 12.00f;
                float yS = 590.0f - (float)valW * 18.0f;
                float xS = xB + (xT - xB) * (yS - 590.0f) / (50.0f - 590.0f);
                return new Point(xS, yS);
            }

            Brush[] colors = { 
                new SolidColorBrush(Microsoft.UI.Colors.Green), 
                new SolidColorBrush(Microsoft.UI.Colors.Purple), 
                new SolidColorBrush(Microsoft.UI.Colors.DarkCyan), 
                new SolidColorBrush(Microsoft.UI.Colors.Blue), 
                new SolidColorBrush(Microsoft.UI.Colors.Magenta) 
            };

            for (int i = 0; i < results.Steps.Count; i++)
            {
                var step = results.Steps[i];
                var p1 = Map(step.Entering.T, step.Entering.W);
                var p2 = Map(step.Leaving.T, step.Leaving.W);

                Line line = new Line
                {
                    X1 = p1.X, Y1 = p1.Y,
                    X2 = p2.X, Y2 = p2.Y,
                    Stroke = colors[i % colors.Length],
                    StrokeThickness = 3,
                    StrokeEndLineCap = PenLineCap.Triangle
                };
                ChartCanvas.Children.Add(line);
            }

            foreach (var pt in results.ChartPoints.Values)
            {
                var p = Map(pt.T, pt.W);
                Ellipse el = new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(Microsoft.UI.Colors.Black) };
                Canvas.SetLeft(el, p.X - 4); Canvas.SetTop(el, p.Y - 4);
                ChartCanvas.Children.Add(el);
                
                TextBlock tb = new TextBlock { Text = pt.Name, FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.Bold };
                Canvas.SetLeft(tb, p.X + 8); Canvas.SetTop(tb, p.Y - 12);
                ChartCanvas.Children.Add(tb);
            }
        }

        private void Units_Click(object sender, RoutedEventArgs e)
        {
            var item = sender as ToggleMenuFlyoutItem;
            if (item == null) return;

            bool toIp = item == MenuIP;
            if (_isIp == toIp) { item.IsChecked = true; return; }

            _isInternalUpdate = true;
            _isIp = toIp;
            MenuSI.IsChecked = !toIp;
            MenuIP.IsChecked = toIp;

            // Update Labels
            LblAlt.Text = toIp ? "Altitude (ft)" : "Altitude (m)";
            LblOaFlow.Text = toIp ? "Flow (CFM)" : "Flow (L/s)";
            LblOaDb.Text = toIp ? "Dry Bulb (°F)" : "Dry Bulb (°C)";
            LblOaWb.Text = toIp ? "Wet Bulb (°F)" : "Wet Bulb (°C)";
            LblEaFlow.Text = toIp ? "Flow (CFM)" : "Flow (L/s)";
            LblEaDb.Text = toIp ? "Dry Bulb (°F)" : "Dry Bulb (°C)";
            LblOffCoil.Text = toIp ? "Off-Coil Temp (°F)" : "Off-Coil Temp (°C)";
            LblSupTemp.Text = toIp ? "Supply Temp (°F)" : "Supply Temp (°C)";
            LblSupEsp.Text = toIp ? "Supply ESP (in.wg)" : "Supply ESP (Pa)";
            LblExtEsp.Text = toIp ? "Extract ESP (in.wg)" : "Extract ESP (Pa)";

            // Convert Values
            NumAltitude.Value = toIp ? Units.MToFt(NumAltitude.Value) : Units.FtToM(NumAltitude.Value);
            NumOaFlow.Value = toIp ? Units.LpsToCfm(NumOaFlow.Value) : Units.CfmToLps(NumOaFlow.Value);
            NumOaDb.Value = toIp ? Units.CtoF(NumOaDb.Value) : Units.FtoC(NumOaDb.Value);
            NumOaWb.Value = toIp ? Units.CtoF(NumOaWb.Value) : Units.FtoC(NumOaWb.Value);
            NumEaFlow.Value = toIp ? Units.LpsToCfm(NumEaFlow.Value) : Units.CfmToLps(NumEaFlow.Value);
            NumEaDb.Value = toIp ? Units.CtoF(NumEaDb.Value) : Units.FtoC(NumEaDb.Value);
            NumOffCoil.Value = toIp ? Units.CtoF(NumOffCoil.Value) : Units.FtoC(NumOffCoil.Value);
            NumSupplyTemp.Value = toIp ? Units.CtoF(NumSupplyTemp.Value) : Units.FtoC(NumSupplyTemp.Value);
            NumSupEsp.Value = toIp ? Units.PaToInWg(NumSupEsp.Value) : Units.InWgToPa(NumSupEsp.Value);
            NumExtEsp.Value = toIp ? Units.PaToInWg(NumExtEsp.Value) : Units.InWgToPa(NumExtEsp.Value);

            ImgChart.Source = new BitmapImage(new Uri(toIp ? "ms-appx:///Images/FlyCarpetPsyChart_IP.png" : "ms-appx:///Images/FlyCarpetPsyChart_SI.png"));

            _isInternalUpdate = false;
            Calculate_Click(null, null);
        }

        private async void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            SetupPicker(picker);
            picker.FileTypeFilter.Add(".doas");
            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                // Logic to deserialize and load
            }
        }

        private async void SaveProject_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileSavePicker();
            SetupPicker(picker);
            picker.FileTypeChoices.Add("DOAS Project", new List<string> { ".doas" });
            picker.SuggestedFileName = "Project";
            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                // Logic to serialize and save
            }
        }

        private void ExportRtf_Click(object sender, RoutedEventArgs e)
        {
            // Logic to export RTF
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private void SetupPicker(object obj)
        {
            IntPtr windowHandle = WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(obj, windowHandle);
        }
    }
}