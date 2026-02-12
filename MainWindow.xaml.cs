using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace DOASCalculatorWinUI
{
    public partial class MainWindow : Window
    {
        private bool _isIp = false;
        private bool _isInternalUpdate = false;
        private SystemResults? _lastResult;

        public MainWindow()
        {
            InitializeComponent();
            CmbSeason.SelectedIndex = 0;
            CmbCoilType.SelectedIndex = 0;
            CmbReheatType.SelectedIndex = 0;
            
            // Set initial values
            UpdateSeasonUI();
        }

        private void Calculate_Click(object sender, RoutedEventArgs? e)
        {
            if (_isInternalUpdate) return;
            try
            {
                var inputs = new SystemInputs
                {
                    IsHeatingMode = CmbSeason.SelectedIndex == 1,
                    OaFlow = GetVal(TxtOaFlow, "oa_flow"),
                    OaDb = GetVal(TxtOaDb, "temp"),
                    OaWb = GetVal(TxtOaWb, "temp"),
                    Altitude = GetVal(TxtAltitude, "alt"),
                    OffCoilTemp = GetVal(TxtOffCoil, "temp"),
                    WheelEnabled = ChkWh.IsChecked == true,
                    EconomizerEnabled = ChkEcon.IsChecked == true,
                    DoubleWheelEnabled = ChkDw.IsChecked == true,
                    HpEnabled = ChkHp.IsChecked == true,
                    ReheatEnabled = ChkReheat.IsChecked == true,
                    SupOaEsp = GetVal(TxtSupEsp, "pressure"),
                    ExtEaEsp = GetVal(TxtExtEsp, "pressure"),
                    FanEff = GetVal(TxtFanEff, "eff")
                };

                if (ChkWh.IsChecked == true)
                {
                    inputs.WheelSens = double.Parse(TxtWhSens.Text);
                    inputs.WheelLat = double.Parse(TxtWhLat.Text);
                    inputs.EaFlow = GetVal(TxtEaFlow, "oa_flow");
                    inputs.EaDb = GetVal(TxtEaDb, "temp");
                    inputs.EaRh = double.Parse(TxtEaRh.Text);
                }

                if (ChkDw.IsChecked == true) inputs.DwSens = double.Parse(TxtDwEff.Text);
                if (ChkHp.IsChecked == true) inputs.HpEff = double.Parse(TxtHpEff.Text);
                if (ChkReheat.IsChecked == true) inputs.TargetSupplyTemp = GetVal(TxtSupTemp, "temp");

                _lastResult = DOASEngine.Process(inputs);
                UpdateResultsUI();
            }
            catch { }
        }

        private double GetVal(TextBox t, string type)
        {
            if (!double.TryParse(t.Text, out double v)) return 0;
            if (!_isIp) return v;
            return type switch
            {
                "alt" => Units.FtToM(v),
                "oa_flow" => Units.CfmToLps(v),
                "temp" => Units.FtoC(v),
                "pressure" => Units.InWgToPa(v),
                _ => v
            };
        }

        private void UpdateResultsUI()
        {
            if (_lastResult == null) return;

            if (_isIp)
            {
                LblCooling.Text = $"Cooling: {Units.KwToMbh(_lastResult.TotalCooling):F1} MBH";
                LblHeating.Text = $"Heating: {Units.KwToMbh(_lastResult.TotalHeating):F1} MBH";
                LblCoolingBreakdown.Text = $"Sensible: {Units.KwToMbh(_lastResult.SensibleCooling):F1} | Latent: {Units.KwToMbh(_lastResult.LatentCooling):F1} MBH";
                LblReheat.Text = $"Reheat: {Units.KwToMbh(_lastResult.ReheatLoad):F1} MBH";
                LblDensity.Text = $"Density: {(_lastResult.AirDensity * 0.062428):F3} lb/ft³";
            }
            else
            {
                LblCooling.Text = $"Cooling: {_lastResult.TotalCooling:F1} kW";
                LblHeating.Text = $"Heating: {_lastResult.TotalHeating:F1} kW";
                LblCoolingBreakdown.Text = $"Sensible: {_lastResult.SensibleCooling:F1} | Latent: {_lastResult.LatentCooling:F1} kW";
                LblReheat.Text = $"Reheat: {_lastResult.ReheatLoad:F1} kW";
                LblDensity.Text = $"Density: {_lastResult.AirDensity:F3} kg/m³";
            }

            LblPower.Text = $"Absorbed: Sup {_lastResult.SupFanPowerKW:F2} / Ext {_lastResult.ExtFanPowerKW:F2} kW\n" +
                           $"Motor Size: Sup {_lastResult.SupMotorKW:F1} / Ext {_lastResult.ExtMotorKW:F1} kW\n" +
                           $"Internal PD: Sup {_lastResult.SupInternalPd:F0} / Ext {_lastResult.ExtInternalPd:F0} Pa";

            var gridData = new List<object>();
            for (int i = 0; i < _lastResult.Steps.Count; i++)
            {
                var step = _lastResult.Steps[i];
                gridData.Add(new
                {
                    Index = i + 1,
                    Component = step.Component,
                    Entering = _isIp ? step.Entering.ToIpString() : step.Entering.ToString(),
                    Leaving = _isIp ? step.Leaving.ToIpString() : step.Leaving.ToString()
                });
            }
            GridResults.ItemsSource = gridData;

            DrawChart(_lastResult);
        }

        private void DrawChart(SystemResults results)
        {
            ChartCanvas.Children.Clear();
            Point Map(double t, double w)
            {
                float xB, xT, xS, yS;
                double valW = w * 1000.0;
                xB = 196.5f + (float)t * 12.53f;
                xT = 162.45f + (float)t * 12.00f;
                yS = 590.0f - (float)valW * 18.0f;
                xS = xB + (xT - xB) * (yS - 590.0f) / (50.0f - 590.0f);
                return new Point(xS, yS);
            }

            Brush[] brushes = { Brushes.Green, Brushes.Purple, Brushes.DarkCyan, Brushes.Blue, Brushes.Magenta, Brushes.OrangeRed };

            for (int i = 0; i < results.Steps.Count; i++)
            {
                var step = results.Steps[i];
                var p1 = Map(step.Entering.T, step.Entering.W);
                var p2 = Map(step.Leaving.T, step.Leaving.W);

                Line line = new Line
                {
                    X1 = p1.X, Y1 = p1.Y,
                    X2 = p2.X, Y2 = p2.Y,
                    Stroke = brushes[i % brushes.Length],
                    StrokeThickness = 3,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                };
                ChartCanvas.Children.Add(line);
            }

            foreach (var pt in results.ChartPoints.Values)
            {
                var p = Map(pt.T, pt.W);
                Ellipse el = new Ellipse { Width = 8, Height = 8, Fill = Brushes.Black };
                Canvas.SetLeft(el, p.X - 4); Canvas.SetTop(el, p.Y - 4);
                ChartCanvas.Children.Add(el);
                TextBlock txt = new TextBlock { Text = pt.Name, FontWeight = FontWeights.Bold, FontSize = 12 };
                Canvas.SetLeft(txt, p.X + 8); Canvas.SetTop(txt, p.Y - 15);
                ChartCanvas.Children.Add(txt);
            }
        }

        private void CmbSeason_SelectionChanged(object sender, SelectionChangedEventArgs e) { UpdateSeasonUI(); }
        
        private void UpdateSeasonUI()
        {
            if (_isInternalUpdate || CmbSeason == null) return;
            _isInternalUpdate = true;
            bool isWinter = CmbSeason.SelectedIndex == 1;
            if (LblCooling != null) { LblCooling.Visibility = isWinter ? Visibility.Collapsed : Visibility.Visible; }
            if (LblHeating != null) { LblHeating.Visibility = isWinter ? Visibility.Visible : Visibility.Collapsed; }
            
            if (isWinter) {
                TxtOaDb.Text = _isIp ? "14" : "-10";
                TxtOaWb.Text = _isIp ? "12" : "-11";
                TxtOffCoil.Text = _isIp ? "55" : "13";
            } else {
                TxtOaDb.Text = _isIp ? "95" : "35";
                TxtOaWb.Text = _isIp ? "78" : "26";
                TxtOffCoil.Text = _isIp ? "54" : "12";
            }
            _isInternalUpdate = false;
            Calculate_Click(null, null);
        }

        private void CmbCoilType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PnlWaterCoil != null) PnlWaterCoil.Visibility = CmbCoilType.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
            Calculate_Click(null, null);
        }

        private void CmbReheatType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PnlReheatWater != null) PnlReheatWater.Visibility = CmbReheatType.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
            if (PnlReheatGas != null) PnlReheatGas.Visibility = CmbReheatType.SelectedIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
            Calculate_Click(null, null);
        }

        private void Units_Click(object sender, RoutedEventArgs e)
        {
            bool toIp = sender == MenuIP;
            if (_isIp == toIp) return;
            
            _isInternalUpdate = true;
            MenuSI.IsChecked = !toIp;
            MenuIP.IsChecked = toIp;

            // Convert all textboxes with tags
            foreach (var tb in FindVisualChildren<TextBox>(this))
            {
                if (tb.Tag is string tag && double.TryParse(tb.Text, out double val))
                {
                    if (tag == "eff") continue;
                    tb.Text = tag switch
                    {
                        "alt" => (toIp ? Units.MToFt(val) : Units.FtToM(val)).ToString("0.##"),
                        "oa_flow" => (toIp ? Units.LpsToCfm(val) : Units.CfmToLps(val)).ToString("0.##"),
                        "temp" => (toIp ? Units.CtoF(val) : Units.FtoC(val)).ToString("0.##"),
                        "temp_diff" => (toIp ? val * 1.8 : val / 1.8).ToString("0.##"),
                        "pressure" => (toIp ? Units.PaToInWg(val) : Units.InWgToPa(val)).ToString("0.##"),
                        "dx_eff" => (toIp ? Units.CopToEer(val) : Units.EerToCop(val)).ToString("0.##"),
                        _ => tb.Text
                    };
                }
            }
            
            _isIp = toIp;
            ImgChart.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(toIp ? "pack://application:,,,/Images/FlyCarpetPsyChart_IP.png" : "pack://application:,,,/Images/FlyCarpetPsyChart_SI.png"));
            _isInternalUpdate = false;
            Calculate_Click(null, null);
        }

        private void Exit_Click(object sender, RoutedEventArgs e) { Application.Current.Shutdown(); }

        private void SaveProject_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog { Filter = "DOAS Project (*.json)|*.json" };
            if (sfd.ShowDialog() == true)
            {
                var data = new ProjectData {
                    IsHeatingMode = CmbSeason.SelectedIndex == 1,
                    OaFlow = TxtOaFlow.Text, OaDb = TxtOaDb.Text, OaWb = TxtOaWb.Text, Altitude = TxtAltitude.Text,
                    WheelEnabled = ChkWh.IsChecked == true, EconomizerEnabled = ChkEcon.IsChecked == true,
                    WheelSens = TxtWhSens.Text, WheelLat = TxtWhLat.Text,
                    EaFlow = TxtEaFlow.Text, EaDb = TxtEaDb.Text, EaRh = TxtEaRh.Text,
                    DoubleWheelEnabled = ChkDw.IsChecked == true, DwSens = TxtDwEff.Text,
                    HpEnabled = ChkHp.IsChecked == true, HpEff = TxtHpEff.Text,
                    OffCoil = TxtOffCoil.Text, CoilTypeIndex = CmbCoilType.SelectedIndex, CoilDeltaT = TxtCoilDt.Text,
                    ReheatEnabled = ChkReheat.IsChecked == true, SupplyTemp = TxtSupTemp.Text, ReheatTypeIndex = CmbReheatType.SelectedIndex,
                    HwEwt = TxtHwEwt.Text, HwLwt = TxtHwLwt.Text, GasEff = TxtGasEff.Text,
                    SupOaEsp = TxtSupEsp.Text, ExtEaEsp = TxtExtEsp.Text, FanEff = TxtFanEff.Text, DxEff = TxtDxEff.Text,
                    IsIpUnits = _isIp
                };
                File.WriteAllText(sfd.FileName, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
            }
        }

        private void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog { Filter = "DOAS Project (*.json)|*.json" };
            if (ofd.ShowDialog() == true)
            {
                var data = JsonSerializer.Deserialize<ProjectData>(File.ReadAllText(ofd.FileName));
                if (data == null) return;
                _isInternalUpdate = true;
                CmbSeason.SelectedIndex = data.IsHeatingMode ? 1 : 0;
                TxtOaFlow.Text = data.OaFlow; TxtOaDb.Text = data.OaDb; TxtOaWb.Text = data.OaWb; TxtAltitude.Text = data.Altitude;
                ChkWh.IsChecked = data.WheelEnabled; ChkEcon.IsChecked = data.EconomizerEnabled;
                TxtWhSens.Text = data.WheelSens; TxtWhLat.Text = data.WheelLat;
                TxtEaFlow.Text = data.EaFlow; TxtEaDb.Text = data.EaDb; TxtEaRh.Text = data.EaRh;
                ChkDw.IsChecked = data.DoubleWheelEnabled; TxtDwEff.Text = data.DwSens;
                ChkHp.IsChecked = data.HpEnabled; TxtHpEff.Text = data.HpEff;
                TxtOffCoil.Text = data.OffCoil; CmbCoilType.SelectedIndex = data.CoilTypeIndex; TxtCoilDt.Text = data.CoilDeltaT;
                ChkReheat.IsChecked = data.ReheatEnabled; TxtSupTemp.Text = data.SupplyTemp; CmbReheatType.SelectedIndex = data.ReheatTypeIndex;
                TxtHwEwt.Text = data.HwEwt; TxtHwLwt.Text = data.HwLwt; TxtGasEff.Text = data.GasEff;
                TxtSupEsp.Text = data.SupOaEsp; TxtExtEsp.Text = data.ExtEaEsp; TxtFanEff.Text = data.FanEff; TxtDxEff.Text = data.DxEff;
                
                _isIp = !data.IsIpUnits; // Flip to trigger conversion in Units_Click
                Units_Click(data.IsIpUnits ? MenuIP : MenuSI, new RoutedEventArgs());
                
                _isInternalUpdate = false;
                Calculate_Click(null, null);
            }
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                if (child is T t) yield return t;
                foreach (T childOfChild in FindVisualChildren<T>(child)) yield return childOfChild;
            }
        }
    }
}