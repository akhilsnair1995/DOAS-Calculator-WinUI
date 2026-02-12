#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace DOASCalculatorWinUI
{
    public sealed partial class MainWindow : Window
    {
        private bool _isIp = false;
        private bool _isInternalUpdate = false;

        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = "DOAS Sizing Calculator (WinUI 3)";
            Calculate_Click(null, null);
        }

        private void Input_Changed(object sender, object e) => Calculate_Click(null, null);

        private void Calculate_Click(object sender, RoutedEventArgs? e)
        {
            if (NumOaFlow == null || _isInternalUpdate) return;

            try {
                var inputs = new SystemInputs {
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
                    
                    DoubleWheelEnabled = TogDw.IsOn,
                    DwSens = NumDwEff.Value,
                    HpEnabled = TogHp.IsOn,
                    HpEff = NumHpEff.Value,

                    OffCoilTemp = _isIp ? Units.FtoC(NumOffCoil.Value) : NumOffCoil.Value,
                    ReheatEnabled = TogReheat.IsOn,
                    TargetSupplyTemp = _isIp ? Units.FtoC(NumSupplyTemp.Value) : NumSupplyTemp.Value,
                    SupOaEsp = _isIp ? NumSupEsp.Value / 0.00401463 : NumSupEsp.Value,
                    ExtEaEsp = _isIp ? NumExtEsp.Value / 0.00401463 : NumExtEsp.Value,
                    FanEff = NumFanEff.Value
                };

                var results = DOASEngine.Process(inputs);
                
                if (_isIp) {
                    ResCooling.Text = $"{Units.KwToMbh(results.TotalCooling):F1} MBH";
                    ResCoolingBreakdown.Text = $"S: {Units.KwToMbh(results.SensibleCooling):F1} | L: {Units.KwToMbh(results.LatentCooling):F1} MBH";
                    ResHeating.Text = $"{Units.KwToMbh(results.TotalHeating):F1} MBH";
                    ResReheat.Text = $"{Units.KwToMbh(results.ReheatLoad):F1} MBH";
                    ResFanPower.Text = $"{results.TotalFanPowerKW:F2} kW";
                    ResFanBreakdown.Text = $"S: {results.SupFanPowerKW:F2} | E: {results.ExtFanPowerKW:F2} kW";
                } else {
                    ResCooling.Text = $"{results.TotalCooling:F1} kW";
                    ResCoolingBreakdown.Text = $"S: {results.SensibleCooling:F1} | L: {results.LatentCooling:F1} kW";
                    ResHeating.Text = $"{results.TotalHeating:F1} kW";
                    ResReheat.Text = $"{results.ReheatLoad:F1} kW";
                    ResFanPower.Text = $"{results.TotalFanPowerKW:F2} kW";
                    ResFanBreakdown.Text = $"S: {results.SupFanPowerKW:F2} | E: {results.ExtFanPowerKW:F2} kW";
                }
                
                var schedule = new List<object>();
                foreach(var s in results.Steps) schedule.Add(new { Component = s.Component, Entering = _isIp ? s.Entering.ToIpString() : s.Entering.ToString(), Leaving = _isIp ? s.Leaving.ToIpString() : s.Leaving.ToString() });
                ListSchedule.ItemsSource = schedule;

                DrawChart(results);
            } catch { }
        }

        private void DrawChart(SystemResults results)
        {
            ChartCanvas.Children.Clear();
            Point Map(double t, double w)
            {
                double valW = w * 1000.0;
                double y = 590.0 - (valW * 18.0);
                double x;

                if (_isIp)
                {
                    double tf = Units.CtoF(t);
                    double xBottom = 145.9 + (tf - 30.0) * 7.59;
                    double slope = -0.0675 + (tf - 30.0) * 0.000709;
                    x = xBottom + slope * (y - 590.0);
                }
                else
                {
                    double xBottom = 196.5 + t * 12.65;
                    double slope = 0.001257 * t - 0.0630;
                    x = xBottom + slope * (y - 590.0);
                }
                return new Point(x, y);
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
                ChartCanvas.Children.Add(new Line { X1 = p1.X, Y1 = p1.Y, X2 = p2.X, Y2 = p2.Y, Stroke = colors[i % colors.Length], StrokeThickness = 3, StrokeEndLineCap = PenLineCap.Triangle });
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

            NumAltitude.Header = toIp ? "Altitude (ft)" : "Altitude (m)";
            NumOaFlow.Header = toIp ? "OA Flow (CFM)" : "OA Flow (L/s)";
            NumOaDb.Header = toIp ? "OA DB (°F)" : "OA DB (°C)";
            NumOaWb.Header = toIp ? "OA WB (°F)" : "OA WB (°C)";
            NumEaFlow.Header = toIp ? "EA Flow (CFM)" : "EA Flow (L/s)";
            NumEaDb.Header = toIp ? "EA DB (°F)" : "EA DB (°C)";
            NumOffCoil.Header = toIp ? "Off-Coil (°F)" : "Off-Coil (°C)";
            NumSupplyTemp.Header = toIp ? "Supply (°F)" : "Supply (°C)";
            NumSupEsp.Header = toIp ? "Sup ESP (in.wg)" : "Sup ESP (Pa)";
            NumExtEsp.Header = toIp ? "Ext ESP (in.wg)" : "Ext ESP (Pa)";

            ImgChart.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(toIp ? "ms-appx:///Images/FlyCarpetPsyChart_IP.png" : "ms-appx:///Images/FlyCarpetPsyChart_SI.png"));

            _isInternalUpdate = false;
            Calculate_Click(null, null);
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Exit();
    }
}