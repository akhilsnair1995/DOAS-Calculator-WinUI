using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DOASCalculatorWinUI;

namespace DOASCalculatorWinUI.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private bool _isIp = false;
        public bool IsIp
        {
            get => _isIp;
            set
            {
                if (SetProperty(ref _isIp, value))
                {
                    OnPropertyChanged(nameof(IsSi));
                    UpdateUnitLabels();
                    ConvertUnits(value);
                }
            }
        }

        public bool IsSi => !IsIp;

        // Inputs
        private double _altitude = 0;
        public double Altitude { get => _altitude; set { if (SetProperty(ref _altitude, Clamp(value, -500, 5000))) Calculate(); } }

        private double _oaFlow = 1000;
        public double OaFlow { get => _oaFlow; set { if (SetProperty(ref _oaFlow, Clamp(value, 0, 100000))) Calculate(); } }

        private double _oaDb = 35.0;
        public double OaDb { get => _oaDb; set { if (SetProperty(ref _oaDb, Clamp(value, -50, 60))) Calculate(); } }

        private double _oaWb = 28.0;
        public double OaWb { get => _oaWb; set { if (SetProperty(ref _oaWb, Clamp(value, -50, _oaDb))) Calculate(); } }

        private double _eaFlow = 800;
        public double EaFlow { get => _eaFlow; set { if (SetProperty(ref _eaFlow, Clamp(value, 0, 100000))) Calculate(); } }

        private double _eaDb = 24.0;
        public double EaDb { get => _eaDb; set { if (SetProperty(ref _eaDb, Clamp(value, 10, 40))) Calculate(); } }

        private double _eaRh = 50.0;
        public double EaRh { get => _eaRh; set { if (SetProperty(ref _eaRh, Clamp(value, 0, 100))) Calculate(); } }

        private bool _wheelEnabled = true;
        public bool WheelEnabled { get => _wheelEnabled; set { if (SetProperty(ref _wheelEnabled, value)) Calculate(); } }

        private double _wheelSens = 75;
        public double WheelSens { get => _wheelSens; set { if (SetProperty(ref _wheelSens, Clamp(value, 0, 100))) Calculate(); } }

        private double _wheelLat = 70;
        public double WheelLat { get => _wheelLat; set { if (SetProperty(ref _wheelLat, Clamp(value, 0, 100))) Calculate(); } }

        private bool _dwEnabled = false;
        public bool DwEnabled { get => _dwEnabled; set { if (SetProperty(ref _dwEnabled, value)) Calculate(); } }

        private double _dwEff = 65;
        public double DwEff { get => _dwEff; set { if (SetProperty(ref _dwEff, Clamp(value, 0, 100))) Calculate(); } }

        private bool _hpEnabled = false;
        public bool HpEnabled { get => _hpEnabled; set { if (SetProperty(ref _hpEnabled, value)) Calculate(); } }

        private double _hpEff = 45;
        public double HpEff { get => _hpEff; set { if (SetProperty(ref _hpEff, Clamp(value, 0, 100))) Calculate(); } }

        private double _offCoil = 12.0;
        public double OffCoil { get => _offCoil; set { if (SetProperty(ref _offCoil, Clamp(value, 4, 30))) Calculate(); } }

        private double _fanEff = 60;
        public double FanEff { get => _fanEff; set { if (SetProperty(ref _fanEff, Clamp(value, 10, 95))) Calculate(); } }

        private bool _reheatEnabled = false;
        public bool ReheatEnabled { get => _reheatEnabled; set { if (SetProperty(ref _reheatEnabled, value)) Calculate(); } }

        private double _supplyTemp = 20.0;
        public double SupplyTemp { get => _supplyTemp; set { if (SetProperty(ref _supplyTemp, Clamp(value, 10, 50))) Calculate(); } }

        private double _supEsp = 500;
        public double SupEsp { get => _supEsp; set { if (SetProperty(ref _supEsp, Clamp(value, 0, 5000))) Calculate(); } }

        private double _extEsp = 500;
        public double ExtEsp { get => _extEsp; set { if (SetProperty(ref _extEsp, Clamp(value, 0, 5000))) Calculate(); } }

        private double _pdDamper = 50;
        public double PdDamper { get => _pdDamper; set { if (SetProperty(ref _pdDamper, Clamp(value, 0, 1000))) Calculate(); } }

        private double _pdFilterPre = 100;
        public double PdFilterPre { get => _pdFilterPre; set { if (SetProperty(ref _pdFilterPre, Clamp(value, 0, 1000))) Calculate(); } }

        private double _pdFilterMain = 250;
        public double PdFilterMain { get => _pdFilterMain; set { if (SetProperty(ref _pdFilterMain, Clamp(value, 0, 2000))) Calculate(); } }

        private double _pdCoil = 250;
        public double PdCoil { get => _pdCoil; set { if (SetProperty(ref _pdCoil, Clamp(value, 0, 2000))) Calculate(); } }

        // Results
        private string _resCooling = "0.0 kW";
        public string ResCooling { get => _resCooling; set => SetProperty(ref _resCooling, value); }

        private string _resCoolingBreakdown = "S: 0.0 | L: 0.0";
        public string ResCoolingBreakdown { get => _resCoolingBreakdown; set => SetProperty(ref _resCoolingBreakdown, value); }

        private string _resReheat = "0.0 kW";
        public string ResReheat { get => _resReheat; set => SetProperty(ref _resReheat, value); }

        private string _resFanPower = "0.0 kW";
        public string ResFanPower { get => _resFanPower; set => SetProperty(ref _resFanPower, value); }

        private string _resFanBreakdown = "S: 0.0 | E: 0.0";
        public string ResFanBreakdown { get => _resFanBreakdown; set => SetProperty(ref _resFanBreakdown, value); }

        public ObservableCollection<object> Schedule { get; } = new ObservableCollection<object>();

        // Unit Labels
        public string LabelAltitude => IsIp ? "Altitude (ft)" : "Altitude (m)";
        public string LabelFlow => IsIp ? "Flow (CFM)" : "Flow (L/s)";
        public string LabelTemp => IsIp ? "Temp (°F)" : "Temp (°C)";
        public string LabelPress => IsIp ? "Pressure (in.wg)" : "Pressure (Pa)";

        public MainViewModel()
        {
            Calculate();
        }

        private double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public void Calculate()
        {
            try
            {
                var inputs = new SystemInputs
                {
                    Altitude = IsIp ? Units.FtToM(Altitude) : Altitude,
                    OaFlow = IsIp ? Units.CfmToLps(OaFlow) : OaFlow,
                    OaDb = IsIp ? Units.FtoC(OaDb) : OaDb,
                    OaWb = IsIp ? Units.FtoC(OaWb) : OaWb,
                    EaFlow = IsIp ? Units.CfmToLps(EaFlow) : EaFlow,
                    EaDb = IsIp ? Units.FtoC(EaDb) : EaDb,
                    EaRh = EaRh,
                    WheelEnabled = WheelEnabled,
                    WheelSens = WheelSens,
                    WheelLat = WheelLat,
                    DoubleWheelEnabled = DwEnabled,
                    DwSens = DwEff,
                    HpEnabled = HpEnabled,
                    HpEff = HpEff,
                    OffCoilTemp = IsIp ? Units.FtoC(OffCoil) : OffCoil,
                    ReheatEnabled = ReheatEnabled,
                    TargetSupplyTemp = IsIp ? Units.FtoC(SupplyTemp) : SupplyTemp,
                    SupOaEsp = IsIp ? Units.InWgToPa(SupEsp) : SupEsp,
                    ExtEaEsp = IsIp ? Units.InWgToPa(ExtEsp) : ExtEsp,
                    FanEff = FanEff,
                    PdDamper = IsIp ? Units.InWgToPa(PdDamper) : PdDamper,
                    PdFilterPre = IsIp ? Units.InWgToPa(PdFilterPre) : PdFilterPre,
                    PdFilterMain = IsIp ? Units.InWgToPa(PdFilterMain) : PdFilterMain,
                    PdCoil = IsIp ? Units.InWgToPa(PdCoil) : PdCoil
                };

                var results = DOASEngine.Process(inputs);

                if (IsIp)
                {
                    ResCooling = $"{Units.KwToMbh(results.TotalCooling):F1} MBH";
                    ResCoolingBreakdown = $"S: {Units.KwToMbh(results.SensibleCooling):F1} | L: {Units.KwToMbh(results.LatentCooling):F1} MBH";
                    ResReheat = $"{Units.KwToMbh(results.ReheatLoad):F1} MBH";
                    ResFanPower = $"{results.TotalFanPowerKW:F2} kW";
                    ResFanBreakdown = $"S: {results.SupFanPowerKW:F2} | E: {results.ExtFanPowerKW:F2} kW";
                }
                else
                {
                    ResCooling = $"{results.TotalCooling:F1} kW";
                    ResCoolingBreakdown = $"S: {results.SensibleCooling:F1} | L: {results.LatentCooling:F1} kW";
                    ResReheat = $"{results.ReheatLoad:F1} kW";
                    ResFanPower = $"{results.TotalFanPowerKW:F2} kW";
                    ResFanBreakdown = $"S: {results.SupFanPowerKW:F2} | E: {results.ExtFanPowerKW:F2} kW";
                }

                Schedule.Clear();
                foreach (var s in results.Steps)
                {
                    Schedule.Add(new
                    {
                        Component = s.Component,
                        Entering = IsIp ? s.Entering.ToIpString() : s.Entering.ToString(),
                        Leaving = IsIp ? s.Leaving.ToIpString() : s.Leaving.ToString()
                    });
                }
            }
            catch { }
        }

        private void UpdateUnitLabels()
        {
            OnPropertyChanged(nameof(LabelAltitude));
            OnPropertyChanged(nameof(LabelFlow));
            OnPropertyChanged(nameof(LabelTemp));
            OnPropertyChanged(nameof(LabelPress));
        }

        private void ConvertUnits(bool toIp)
        {
            // Unit conversion logic for all numeric properties
            Altitude = toIp ? Units.MToFt(Altitude) : Units.FtToM(Altitude);
            OaFlow = toIp ? Units.LpsToCfm(OaFlow) : Units.CfmToLps(OaFlow);
            OaDb = toIp ? Units.CtoF(OaDb) : Units.FtoC(OaDb);
            OaWb = toIp ? Units.CtoF(OaWb) : Units.FtoC(OaWb);
            EaFlow = toIp ? Units.LpsToCfm(EaFlow) : Units.CfmToLps(EaFlow);
            EaDb = toIp ? Units.CtoF(EaDb) : Units.FtoC(EaDb);
            OffCoil = toIp ? Units.CtoF(OffCoil) : Units.FtoC(OffCoil);
            SupplyTemp = toIp ? Units.CtoF(SupplyTemp) : Units.FtoC(SupplyTemp);
            SupEsp = toIp ? Units.PaToInWg(SupEsp) : Units.InWgToPa(SupEsp);
            ExtEsp = toIp ? Units.PaToInWg(ExtEsp) : Units.InWgToPa(ExtEsp);
            PdDamper = toIp ? Units.PaToInWg(PdDamper) : Units.InWgToPa(PdDamper);
            PdFilterPre = toIp ? Units.PaToInWg(PdFilterPre) : Units.InWgToPa(PdFilterPre);
            PdFilterMain = toIp ? Units.PaToInWg(PdFilterMain) : Units.InWgToPa(PdFilterMain);
            PdCoil = toIp ? Units.PaToInWg(PdCoil) : Units.InWgToPa(PdCoil);
        }
    }
}