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
            Calculate_Click(this, null);
        }

        private void Input_Changed(object? sender, object? e) => Calculate_Click(this, null);

        private void Calculate_Click(object? sender, RoutedEventArgs? e)
        {
            if (NumOaFlow == null || _isInternalUpdate) return;

            try {
                var inputs = new SystemInputs {
                    IsHeatingMode = TogHeating.IsOn,
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
                    
                    DoubleWheelEnabled = TogDw.IsOn,
                    DwSens = NumDwEff.Value,
                    HpEnabled = TogHp.IsOn,
                    HpEff = NumHpEff.Value,

                    OffCoilTemp = _isIp ? Units.FtoC(NumOffCoil.Value) : NumOffCoil.Value,
                    ReheatEnabled = TogReheat.IsOn,
                    TargetSupplyTemp = _isIp ? Units.FtoC(NumSupplyTemp.Value) : NumSupplyTemp.Value,
                    SupOaEsp = _isIp ? NumSupEsp.Value / 0.00401463 : NumSupEsp.Value,
                    ExtEaEsp = _isIp ? NumExtEsp.Value / 0.00401463 : NumExtEsp.Value,
                    FanEff = NumFanEff.Value,

                    PdDamper = _isIp ? NumPdDamper.Value / 0.00401463 : NumPdDamper.Value,
                    PdFilterPre = _isIp ? NumPdFilterPre.Value / 0.00401463 : NumPdFilterPre.Value,
                    PdFilterMain = _isIp ? NumPdFilterMain.Value / 0.00401463 : NumPdFilterMain.Value,
                    PdCoil = _isIp ? NumPdCoil.Value / 0.00401463 : NumPdCoil.Value
                };

                var results = DOASEngine.Process(inputs);
                
                if (_isIp) {
                    ResCooling.Text = $"{Units.KwToMbh(results.TotalCooling):F1} MBH";
                    ResCoolingBreakdown.Text = $"S: {Units.KwToMbh(results.SensibleCooling):F1} | L: {Units.KwToMbh(results.LatentCooling):F1} MBH";
                    ResReheat.Text = $"{Units.KwToMbh(results.ReheatLoad):F1} MBH";
                    ResFanPower.Text = $"{results.TotalFanPowerKW:F2} kW";
                    ResFanBreakdown.Text = $"S: {results.SupFanPowerKW:F2} | E: {results.ExtFanPowerKW:F2} kW";
                } else {
                    ResCooling.Text = $"{results.TotalCooling:F1} kW";
                    ResCoolingBreakdown.Text = $"S: {results.SensibleCooling:F1} | L: {results.LatentCooling:F1} kW";
                    ResReheat.Text = $"{results.ReheatLoad:F1} kW";
                    ResFanPower.Text = $"{results.TotalFanPowerKW:F2} kW";
                    ResFanBreakdown.Text = $"S: {results.SupFanPowerKW:F2} | E: {results.ExtFanPowerKW:F2} kW";
                }
                
                var schedule = new List<object>();
                foreach(var s in results.Steps) schedule.Add(new { Component = s.Component, Entering = _isIp ? s.Entering.ToIpString() : s.Entering.ToString(), Leaving = _isIp ? s.Leaving.ToIpString() : s.Leaving.ToString() });
                ListSchedule.ItemsSource = schedule;
            } catch { }
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
            NumPdDamper.Value = toIp ? Units.PaToInWg(NumPdDamper.Value) : Units.InWgToPa(NumPdDamper.Value);
            NumPdFilterPre.Value = toIp ? Units.PaToInWg(NumPdFilterPre.Value) : Units.InWgToPa(NumPdFilterPre.Value);
            NumPdFilterMain.Value = toIp ? Units.PaToInWg(NumPdFilterMain.Value) : Units.InWgToPa(NumPdFilterMain.Value);
            NumPdCoil.Value = toIp ? Units.PaToInWg(NumPdCoil.Value) : Units.InWgToPa(NumPdCoil.Value);

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
            NumPdDamper.Header = toIp ? "Damper (in.wg)" : "Damper (Pa)";
            NumPdFilterPre.Header = toIp ? "Pre-Filter (in.wg)" : "Pre-Filter (Pa)";
            NumPdFilterMain.Header = toIp ? "Main Filter (in.wg)" : "Main Filter (Pa)";
            NumPdCoil.Header = toIp ? "Cooling Coil (in.wg)" : "Cooling Coil (Pa)";

            _isInternalUpdate = false;
            Calculate_Click(this, null);
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Exit();
    }
}