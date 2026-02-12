using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace DOASCalculatorWinUI
{
    public sealed partial class MainWindow : Window
    {
        private BitmapImage _imgSi;
        private BitmapImage _imgIp;
        private bool _isIp = false;

        // Chart Calibration Constants (SI)
        private const float CHAR_WIDTH = 902.0f;
        private const float CHART_HEIGHT = 652.0f;
        private const float Y_BOTTOM = 590.0f;
        private const float Y_TOP = 50.0f;

        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = "DOAS Sizing Calculator (WinUI)";

            // Load Resources
            _imgSi = new BitmapImage(new Uri("ms-appx:///Images/FlyCarpetPsyChart_SI.png"));
            _imgIp = new BitmapImage(new Uri("ms-appx:///Images/FlyCarpetPsyChart_IP.png"));
            
            // Set Default Chart
            ImgPsychChart.Source = _imgSi;
            
            // Initial Calculation
            Calculate_Click(null, null);
        }

        private void Calculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Gather Inputs
                var inputs = new SystemInputs
                {
                    IsHeatingMode = RadioMode.SelectedIndex == 1,
                    Altitude = NumAltitude.Value,
                    
                    OaFlow = NumOaFlow.Value,
                    OaDb = NumOaDb.Value,
                    OaWb = NumOaWb.Value,
                    
                    EaFlow = NumEaFlow.Value,
                    EaDb = NumEaDb.Value,
                    EaRh = NumEaRh.Value,
                    
                    WheelEnabled = TogWheel.IsOn,
                    EconomizerEnabled = ChkEconomizer.IsChecked ?? false,
                    WheelSens = NumWheelSens.Value,
                    WheelLat = NumWheelLat.Value,
                    
                    DoubleWheelEnabled = TogDoubleWheel.IsOn,
                    DwSens = NumDwEff.Value,
                    
                    HpEnabled = TogHp.IsOn,
                    HpEff = NumHpEff.Value,
                    
                    OffCoilTemp = NumOffCoil.Value,
                    
                    ReheatEnabled = TogReheat.IsOn,
                    TargetSupplyTemp = NumSupplyTemp.Value,
                    
                    SupOaEsp = NumSupEsp.Value,
                    ExtEaEsp = NumExtEsp.Value,
                    FanEff = NumFanEff.Value
                };

                // 2. Process
                var results = DOASEngine.Process(inputs);

                // 3. Display Results
                ResTotalCooling.Text = $"{results.TotalCooling:F1} kW";
                ResTotalHeating.Text = $"{results.TotalHeating:F1} kW";
                ResReheat.Text = $"{results.ReheatLoad:F1} kW";
                ResFanPower.Text = $"{results.TotalFanPowerKW:F2} kW";

                // 4. Draw Chart
                DrawChart(results, inputs.IsHeatingMode);
            }
            catch (Exception ex)
            {
                // Simple error handling
                if (ResTotalCooling != null) ResTotalCooling.Text = "Error";
            }
        }

        private void DrawChart(SystemResults results, bool isHeating)
        {
            ChartCanvas.Children.Clear();

            // Calibration Logic (Ported from PsychChartRenderer.cs)
            Point Map(double t, double w)
            {
                float xB, xT, xS, yS;
                double valW = w * 1000.0; // convert to g/kg for chart y-axis scaling

                if (_isIp)
                {
                    // IP Logic Placeholder (Need to port if IP mode is added fully)
                    // Falling back to SI logic for now as main focus
                    xB = 196.5f + (float)t * 12.53f;
                    xT = 162.45f + (float)t * 12.00f;
                }
                else
                {
                    // SI intercepts derived from SVG: 0C line and 50C line
                    xB = 196.5f + (float)t * 12.53f;
                    xT = 162.45f + (float)t * 12.00f;
                }

                yS = Y_BOTTOM - (float)valW * 18.0f;
                // Interpolate X based on Y height (skewed chart)
                xS = xB + (xT - xB) * (yS - Y_BOTTOM) / (Y_TOP - Y_BOTTOM);
                
                // Scale to Canvas Size (902x652)
                float pxX = (xS / 902.0f) * (float)ChartCanvas.Width;
                float pxY = (yS / 652.0f) * (float)ChartCanvas.Height;
                
                return new Point(pxX, pxY);
            }

            // Colors
            var colors = new SolidColorBrush[] 
            { 
                new SolidColorBrush(Microsoft.UI.Colors.Green), 
                new SolidColorBrush(Microsoft.UI.Colors.Purple), 
                new SolidColorBrush(Microsoft.UI.Colors.DarkCyan), 
                new SolidColorBrush(Microsoft.UI.Colors.Blue), 
                new SolidColorBrush(Microsoft.UI.Colors.Magenta), 
                new SolidColorBrush(Microsoft.UI.Colors.OrangeRed) 
            };

            // Draw Process Path
            for (int i = 0; i < results.Steps.Count; i++)
            {
                var step = results.Steps[i];
                var p1 = Map(step.Entering.T, step.Entering.W);
                var p2 = Map(step.Leaving.T, step.Leaving.W);
                var color = colors[i % colors.Length];

                // Draw Line
                var line = new Line
                {
                    X1 = p1.X,
                    Y1 = p1.Y,
                    X2 = p2.X,
                    Y2 = p2.Y,
                    Stroke = color,
                    StrokeThickness = 3,
                    StrokeEndLineCap = PenLineCap.Triangle // Arrow-ish
                };
                ChartCanvas.Children.Add(line);

                // Draw Component Label (Midpoint)
                /*
                var label = new TextBlock
                {
                    Text = (i + 1).ToString(),
                    Foreground = color,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold
                };
                Canvas.SetLeft(label, (p1.X + p2.X) / 2);
                Canvas.SetTop(label, (p1.Y + p2.Y) / 2);
                ChartCanvas.Children.Add(label);
                */
            }

            // Draw Point Anchors
            foreach (var pt in results.ChartPoints.Values)
            {
                var p = Map(pt.T, pt.W);
                
                var ellipse = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = new SolidColorBrush(Microsoft.UI.Colors.Black)
                };
                Canvas.SetLeft(ellipse, p.X - 4);
                Canvas.SetTop(ellipse, p.Y - 4);
                ChartCanvas.Children.Add(ellipse);

                var text = new TextBlock
                {
                    Text = pt.Name,
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black),
                    FontSize = 12,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                };
                Canvas.SetLeft(text, p.X + 6);
                Canvas.SetTop(text, p.Y - 12); // Slightly above
                ChartCanvas.Children.Add(text);
            }
        }
    }
}