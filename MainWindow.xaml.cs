using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Serialization;
using Microsoft.Win32;
using System.Drawing.Printing;
using System.Drawing;
using Color = System.Drawing.Color;
using Brush = System.Drawing.Brush;
using Brushes = System.Drawing.Brushes;
using Pen = System.Drawing.Pen;
using Pens = System.Drawing.Pens;
using Point = System.Windows.Point;

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

        private void Input_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isInternalUpdate) Calculate_Click(null, null);
        }

        private void Calculate_Click(object sender, RoutedEventArgs? e)
        {
            if (_isInternalUpdate || CmbSeason == null) return;
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
                UpdateResultsUI(inputs);
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

        private void UpdateResultsUI(SystemInputs inputs)
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

            // Reheat Source Info
            if (inputs.ReheatEnabled)
            {
                if (CmbReheatType.SelectedIndex == 1) // Water
                {
                    double.TryParse(TxtHwEwt.Text, out double ewt); double.TryParse(TxtHwLwt.Text, out double lwt);
                    double dt = Math.Max(1, Math.Abs(ewt - lwt)) * (_isIp ? 5 / 9.0 : 1);
                    double flow = _lastResult.ReheatLoad / (4.186 * dt);
                    LblReheatDesc.Text = _isIp ? $"Water: {(flow * 15.85):F1} GPM" : $"Water: {flow:F2} L/s";
                }
                else if (CmbReheatType.SelectedIndex == 2) // Gas
                {
                    double.TryParse(TxtGasEff.Text, out double geff);
                    double flow = (_lastResult.ReheatLoad * 3600) / (46400 * (geff / 100.0));
                    LblReheatDesc.Text = $"Gas: {flow:F2} kg/hr";
                }
                else LblReheatDesc.Text = "Electric Reheat";
            }
            else LblReheatDesc.Text = "Reheat Disabled";

            // CHW Flow
            if (CmbCoilType.SelectedIndex == 1 && _lastResult.TotalCooling > 0)
            {
                double.TryParse(TxtCoilDt.Text, out double cdt);
                if (cdt > 0)
                {
                    if (_isIp) {
                        double gpm = (Units.KwToMbh(_lastResult.TotalCooling) * 2.0) / cdt;
                        LblChwFlow.Text = $"CHW Flow: {gpm:F2} GPM (ΔT {cdt:F1}°F)";
                    } else {
                        double lps = _lastResult.TotalCooling / (4.186 * cdt);
                        LblChwFlow.Text = $"CHW Flow: {lps:F2} L/s (ΔT {cdt:F1}°C)";
                    }
                    LblChwFlow.Visibility = Visibility.Visible;
                }
            } else LblChwFlow.Visibility = Visibility.Collapsed;

            // Warnings Logic
            List<string> warnings = new List<string>();
            if (ChkDw.IsChecked == true && ChkHp.IsChecked == true) warnings.Add("Warning: Combining Double Wheel and Heat Pipes may be over-engineering.");
            if (ChkWh.IsChecked == true && inputs.OaFlow > 0.1) {
                if (inputs.EaFlow / inputs.OaFlow < 0.70) warnings.Add("Warning: Heat recovery not recommended when exhaust air < 70% of fresh air.");
            }
            if (inputs.OaFlow > 37756) warnings.Add("Warning: Airflow exceeds 80,000 CFM (Standard models unavailable).");

            if (warnings.Count > 0) {
                LblWarnText.Text = string.Join("\n", warnings);
                PnlWarning.Visibility = Visibility.Visible;
            } else PnlWarning.Visibility = Visibility.Collapsed;

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

            System.Windows.Media.Brush[] brushes = { System.Windows.Media.Brushes.Green, System.Windows.Media.Brushes.Purple, System.Windows.Media.Brushes.DarkCyan, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Magenta, System.Windows.Media.Brushes.OrangeRed };

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
                Ellipse el = new Ellipse { Width = 8, Height = 8, Fill = System.Windows.Media.Brushes.Black };
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

            // Update Labels
            LblAltUnit.Text = toIp ? "Altitude (ft)" : "Altitude (m)";
            LblFlowUnit.Text = toIp ? "Fresh Air (CFM)" : "Fresh Air (L/s)";
            LblDbUnit.Text = toIp ? "Fresh Air DB (°F)" : "Fresh Air DB (°C)";
            LblWbUnit.Text = toIp ? "Fresh Air WB (°F)" : "Fresh Air WB (°C)";
            LblEaFlowUnit.Text = toIp ? "Flow (CFM)" : "Flow (L/s)";
            LblEaDbUnit.Text = toIp ? "DB (°F)" : "DB (°C)";
            LblCoilDtUnit.Text = toIp ? "Water Delta T (°F)" : "Water Delta T (°C)";
            LblOffCoilUnit.Text = toIp ? "Off Coil Temp (°F)" : "Off Coil Temp (°C)";
            LblSupTempUnit.Text = toIp ? "Target Supply (°F)" : "Target Supply (°C)";
            LblEwtUnit.Text = toIp ? "Ent. Water (°F)" : "Ent. Water (°C)";
            LblLwtUnit.Text = toIp ? "Lvg. Water (°F)" : "Lvg. Water (°C)";
            LblSupEspUnit.Text = toIp ? "Supply ESP (in.wg)" : "Supply ESP (Pa)";
            LblExtEspUnit.Text = toIp ? "Extract ESP (in.wg)" : "Extract ESP (Pa)";
            LblDxEffUnit.Text = toIp ? "DX Eff. (EER)" : "DX Eff. (COP)";

            // Convert all textboxes
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
            var sfd = new SaveFileDialog { Filter = "DOAS Project|*.doas", FileName = "Project.doas" };
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
                try {
                    var xs = new XmlSerializer(typeof(ProjectData));
                    using var sw = new StreamWriter(sfd.FileName);
                    xs.Serialize(sw, data);
                    MessageBox.Show("Project saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                } catch (Exception ex) { MessageBox.Show("Error saving: " + ex.Message); }
            }
        }

        private void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog { Filter = "DOAS Project|*.doas" };
            if (ofd.ShowDialog() == true)
            {
                try {
                    var xs = new XmlSerializer(typeof(ProjectData));
                    using var sr = new StreamReader(ofd.FileName);
                    var data = (ProjectData?)xs.Deserialize(sr);
                    if (data == null) return;
                    
                    _isInternalUpdate = true;
                    _isIp = data.IsIpUnits;
                    MenuSI.IsChecked = !_isIp; MenuIP.IsChecked = _isIp;
                    
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
                    
                    ImgChart.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(_isIp ? "pack://application:,,,/Images/FlyCarpetPsyChart_IP.png" : "pack://application:,,,/Images/FlyCarpetPsyChart_SI.png"));
                    _isInternalUpdate = false;
                    Calculate_Click(null, null);
                } catch (Exception ex) { MessageBox.Show("Error loading: " + ex.Message); }
            }
        }

        private void PrintReport_Click(object sender, RoutedEventArgs e)
        {
            PrintDocument pd = new PrintDocument();
            pd.PrintPage += (s, ev) => {
                Graphics g = ev.Graphics!;
                System.Drawing.Font tF = new System.Drawing.Font("Arial", 18, System.Drawing.FontStyle.Bold);
                System.Drawing.Font hF = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold);
                System.Drawing.Font bF = new System.Drawing.Font("Arial", 10);
                Pen p = Pens.Black;
                int y = 50, x = 50;

                g.DrawString("DOAS Sizing Report", tF, Brushes.DarkBlue, x, y); y += 40;
                g.DrawString($"Date: {DateTime.Now:d}", bF, Brushes.Black, x, y); y += 25;
                g.DrawString(LblDensity.Text, bF, Brushes.Black, x, y); y += 40;

                g.DrawString("System Summary", hF, Brushes.DarkBlue, x, y); y += 25;
                var data = new List<(string, string)> {
                    ("Cooling Capacity", LblCooling.Text),
                    ("Heating Capacity", LblHeating.Text),
                    ("Reheat Load", LblReheat.Text),
                    ("Fresh Air Flow", TxtOaFlow.Text + (_isIp ? " CFM" : " L/s")),
                    ("Extract Air Flow", TxtEaFlow.Text + (_isIp ? " CFM" : " L/s"))
                };

                int col1W = 200, col2W = 350, rH = 25;
                foreach (var item in data) {
                    g.DrawRectangle(p, x, y, col1W, rH); g.DrawString(item.Item1, bF, Brushes.Black, x + 5, y + 5);
                    g.DrawRectangle(p, x + col1W, y, col2W, rH); g.DrawString(item.Item2, bF, Brushes.Black, x + col1W + 5, y + 5);
                    y += rH;
                }
            };
            var diag = new System.Windows.Controls.PrintDialog();
            if (diag.ShowDialog() == true) { pd.Print(); }
        }

        private void SaveRtf_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog { Filter = "Rich Text Format|*.rtf", FileName = "Report.rtf" };
            if (sfd.ShowDialog() == true) {
                string rtf = "{\\rtf1\\ansi\\deff0 {\\fonttbl{\\f0 Segoe UI;}}{\\colortbl;\\red0\\green0\\blue255;\\red255\\green0\\blue0;}\n" +
                             "{\\f0\\fs36\\b DOAS Sizing Report}\\par\n" +
                             "Date: " + DateTime.Now.ToShortDateString() + "\\par\\par\n" +
                             "{\\b System Results:}\\par\n" +
                             "Cooling: " + LblCooling.Text + "\\par\n" +
                             "Heating: " + LblHeating.Text + "\\par\n" +
                             "Reheat: " + LblReheat.Text + "\\par\n}";
                File.WriteAllText(sfd.FileName, rtf);
                MessageBox.Show("RTF Report saved!");
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