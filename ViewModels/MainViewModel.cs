using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
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

        // Errors
        private Dictionary<string, string> _errors = new Dictionary<string, string>();
        
        public string GetError(string propertyName) => _errors.ContainsKey(propertyName) ? _errors[propertyName] : "";
        public bool HasError(string propertyName) => _errors.ContainsKey(propertyName);

        // Inputs
        private double _altitude = 0;
        public double Altitude { get => _altitude; set { if (SetProperty(ref _altitude, value)) ValidateAndCalculate(); } }

        private double _oaFlow = 1000;
        public double OaFlow { get => _oaFlow; set { if (SetProperty(ref _oaFlow, value)) ValidateAndCalculate(); } }

        private double _oaDb = 35.0;
        public double OaDb { get => _oaDb; set { if (SetProperty(ref _oaDb, value)) ValidateAndCalculate(); } }

        private double _oaWb = 28.0;
        public double OaWb { get => _oaWb; set { if (SetProperty(ref _oaWb, value)) ValidateAndCalculate(); } }

        private double _eaFlow = 800;
        public double EaFlow { get => _eaFlow; set { if (SetProperty(ref _eaFlow, value)) ValidateAndCalculate(); } }

        private double _eaDb = 24.0;
        public double EaDb { get => _eaDb; set { if (SetProperty(ref _eaDb, value)) ValidateAndCalculate(); } }

        private double _eaRh = 50.0;
        public double EaRh { get => _eaRh; set { if (SetProperty(ref _eaRh, value)) ValidateAndCalculate(); } }

        private bool _wheelEnabled = true;
        public bool WheelEnabled { get => _wheelEnabled; set { if (SetProperty(ref _wheelEnabled, value)) ValidateAndCalculate(); } }

        private double _wheelSens = 75;
        public double WheelSens { get => _wheelSens; set { if (SetProperty(ref _wheelSens, value)) ValidateAndCalculate(); } }

        private double _wheelLat = 70;
        public double WheelLat { get => _wheelLat; set { if (SetProperty(ref _wheelLat, value)) ValidateAndCalculate(); } }

        private bool _dwEnabled = false;
        public bool DwEnabled { get => _dwEnabled; set { if (SetProperty(ref _dwEnabled, value)) ValidateAndCalculate(); } }

        private double _dwEff = 65;
        public double DwEff { get => _dwEff; set { if (SetProperty(ref _dwEff, value)) ValidateAndCalculate(); } }

        private bool _hpEnabled = false;
        public bool HpEnabled { get => _hpEnabled; set { if (SetProperty(ref _hpEnabled, value)) ValidateAndCalculate(); } }

        private double _hpEff = 45;
        public double HpEff { get => _hpEff; set { if (SetProperty(ref _hpEff, value)) ValidateAndCalculate(); } }

        private double _offCoil = 12.0;
        public double OffCoil { get => _offCoil; set { if (SetProperty(ref _offCoil, Clamp(value, 4, 30))) ValidateAndCalculate(); } }

        private CoilType _mainCoilType = CoilType.Dx;
        public CoilType MainCoilType { get => _mainCoilType; set { if (SetProperty(ref _mainCoilType, value)) { OnPropertyChanged(nameof(IsMainCoilWater)); OnPropertyChanged(nameof(MainCoilTypeIndex)); ValidateAndCalculate(); } } }
        public bool IsMainCoilWater => MainCoilType == CoilType.Water;

        public int MainCoilTypeIndex { get => (int)MainCoilType; set => MainCoilType = (CoilType)value; }

        private double _mainCoilDeltaT = 5.5;
        public double MainCoilDeltaT { get => _mainCoilDeltaT; set { if (SetProperty(ref _mainCoilDeltaT, Clamp(value, 1, 20))) ValidateAndCalculate(); } }

        private double _fanEff = 60;
        public double FanEff { get => _fanEff; set { if (SetProperty(ref _fanEff, Clamp(value, 10, 95))) ValidateAndCalculate(); } }

        private bool _reheatEnabled = false;
        public bool ReheatEnabled { get => _reheatEnabled; set { if (SetProperty(ref _reheatEnabled, value)) { OnPropertyChanged(nameof(VisibilityReheatTarget)); ValidateAndCalculate(); } } }

        private ReheatSource _reheatType = ReheatSource.Electric;
        public ReheatSource ReheatType { get => _reheatType; set { if (SetProperty(ref _reheatType, value)) { OnPropertyChanged(nameof(ReheatTypeIndex)); UpdateReheatVisibilities(); ValidateAndCalculate(); } } }

        public int ReheatTypeIndex { get => (int)ReheatType; set => ReheatType = (ReheatSource)value; }

        private double _supplyTemp = 20.0;
        public double SupplyTemp { get => _supplyTemp; set { if (SetProperty(ref _supplyTemp, Clamp(value, 10, 50))) ValidateAndCalculate(); } }

        private double _hwEwt = 80;
        public double HwEwt { get => _hwEwt; set { if (SetProperty(ref _hwEwt, Clamp(value, 30, 90))) ValidateAndCalculate(); } }

        private double _hwLwt = 60;
        public double HwLwt { get => _hwLwt; set { if (SetProperty(ref _hwLwt, Clamp(value, 20, 85))) ValidateAndCalculate(); } }

        private double _gasEfficiency = 80;
        public double GasEfficiency { get => _gasEfficiency; set { if (SetProperty(ref _gasEfficiency, Clamp(value, 50, 99))) ValidateAndCalculate(); } }

        public bool IsReheatWater => ReheatType == ReheatSource.HotWater;
        public bool IsReheatGas => ReheatType == ReheatSource.Gas;

        private void UpdateReheatVisibilities()
        {
            OnPropertyChanged(nameof(IsReheatWater));
            OnPropertyChanged(nameof(IsReheatGas));
            OnPropertyChanged(nameof(VisibilityReheatWater));
            OnPropertyChanged(nameof(VisibilityReheatGas));
        }

        public Visibility VisibilityMainCoilWater => IsMainCoilWater ? Visibility.Visible : Visibility.Collapsed;
        public Visibility VisibilityReheatWater => (ReheatEnabled && IsReheatWater) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility VisibilityReheatGas => (ReheatEnabled && IsReheatGas) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility VisibilityReheatTarget => ReheatEnabled ? Visibility.Visible : Visibility.Collapsed;

        private double _supEsp = 500;
        public double SupEsp { get => _supEsp; set { if (SetProperty(ref _supEsp, value)) ValidateAndCalculate(); } }

        private double _extEsp = 500;
        public double ExtEsp { get => _extEsp; set { if (SetProperty(ref _extEsp, value)) ValidateAndCalculate(); } }

        private double _pdDamper = 50;
        public double PdDamper { get => _pdDamper; set { if (SetProperty(ref _pdDamper, value)) ValidateAndCalculate(); } }

        private double _pdFilterPre = 100;
        public double PdFilterPre { get => _pdFilterPre; set { if (SetProperty(ref _pdFilterPre, value)) ValidateAndCalculate(); } }

        private double _pdFilterMain = 250;
        public double PdFilterMain { get => _pdFilterMain; set { if (SetProperty(ref _pdFilterMain, value)) ValidateAndCalculate(); } }

        private double _pdCoil = 250;
        public double PdCoil { get => _pdCoil; set { if (SetProperty(ref _pdCoil, value)) ValidateAndCalculate(); } }

        // Error message helpers for XAML
        public string ErrorAltitude => GetError(nameof(Altitude));
        public Visibility VisibilityAltitude => HasError(nameof(Altitude)) ? Visibility.Visible : Visibility.Collapsed;

        public string ErrorOaFlow => GetError(nameof(OaFlow));
        public Visibility VisibilityOaFlow => HasError(nameof(OaFlow)) ? Visibility.Visible : Visibility.Collapsed;

        public string ErrorOaDb => GetError(nameof(OaDb));
        public Visibility VisibilityOaDb => HasError(nameof(OaDb)) ? Visibility.Visible : Visibility.Collapsed;

        public string ErrorOaWb => GetError(nameof(OaWb));
        public Visibility VisibilityOaWb => HasError(nameof(OaWb)) ? Visibility.Visible : Visibility.Collapsed;

        public string ErrorWheelSens => GetError(nameof(WheelSens));
        public Visibility VisibilityWheelSens => HasError(nameof(WheelSens)) ? Visibility.Visible : Visibility.Collapsed;

        public string ErrorWheelLat => GetError(nameof(WheelLat));
        public Visibility VisibilityWheelLat => HasError(nameof(WheelLat)) ? Visibility.Visible : Visibility.Collapsed;

        public string ErrorFanEff => GetError(nameof(FanEff));
        public Visibility VisibilityFanEff => HasError(nameof(FanEff)) ? Visibility.Visible : Visibility.Collapsed;

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
            ValidateAndCalculate();
        }

        private void ValidateAndCalculate()
        {
            _errors.Clear();

            // Validate Altitude: -500m to 5000m
            double altSi = IsIp ? Units.FtToM(Altitude) : Altitude;
            if (altSi < -500 || altSi > 5000) 
                _errors[nameof(Altitude)] = IsIp ? "Altitude must be between -1640 and 16400 ft" : "Altitude must be between -500 and 5000 m";

            // Validate OA Flow: > 0
            double flowSi = IsIp ? Units.CfmToLps(OaFlow) : OaFlow;
            if (flowSi <= 0) _errors[nameof(OaFlow)] = "Flow must be greater than zero";

            // Validate OA DB: -50 to 60 C
            double dbSi = IsIp ? Units.FtoC(OaDb) : OaDb;
            if (dbSi < -50 || dbSi > 60)
                _errors[nameof(OaDb)] = IsIp ? "Temp must be between -58 and 140 °F" : "Temp must be between -50 and 60 °C";

            // Validate OA WB: <= OA DB
            double wbSi = IsIp ? Units.FtoC(OaWb) : OaWb;
            if (wbSi > dbSi) _errors[nameof(OaWb)] = "Wet bulb cannot be higher than Dry Bulb";

            // Validate Efficiencies
            if (WheelSens < 0 || WheelSens > 100) _errors[nameof(WheelSens)] = "Efficiency must be between 0 and 100%";
            if (WheelLat < 0 || WheelLat > 100) _errors[nameof(WheelLat)] = "Efficiency must be between 0 and 100%";
            if (FanEff < 10 || FanEff > 95) _errors[nameof(FanEff)] = "Fan efficiency typically 10-95%";

            // Refresh UI Error Bindings
            OnPropertyChanged(nameof(ErrorAltitude));
            OnPropertyChanged(nameof(VisibilityAltitude));
            OnPropertyChanged(nameof(ErrorOaFlow));
            OnPropertyChanged(nameof(VisibilityOaFlow));
            OnPropertyChanged(nameof(ErrorOaDb));
            OnPropertyChanged(nameof(VisibilityOaDb));
            OnPropertyChanged(nameof(ErrorOaWb));
            OnPropertyChanged(nameof(VisibilityOaWb));
            OnPropertyChanged(nameof(ErrorWheelSens));
            OnPropertyChanged(nameof(VisibilityWheelSens));
            OnPropertyChanged(nameof(ErrorWheelLat));
            OnPropertyChanged(nameof(VisibilityWheelLat));
            OnPropertyChanged(nameof(ErrorFanEff));
            OnPropertyChanged(nameof(VisibilityFanEff));

            if (_errors.Count == 0)
            {
                Calculate();
            }
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
                    MainCoilType = MainCoilType,
                    MainCoilDeltaT = IsIp ? MainCoilDeltaT / 1.8 : MainCoilDeltaT,
                    ReheatEnabled = ReheatEnabled,
                    ReheatType = ReheatType,
                    TargetSupplyTemp = IsIp ? Units.FtoC(SupplyTemp) : SupplyTemp,
                    HwEwt = IsIp ? Units.FtoC(HwEwt) : HwEwt,
                    HwLwt = IsIp ? Units.FtoC(HwLwt) : HwLwt,
                    GasEfficiency = GasEfficiency,
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
                    string breakdown = $"S: {Units.KwToMbh(results.SensibleCooling):F1} | L: {Units.KwToMbh(results.LatentCooling):F1} MBH";
                    if (IsMainCoilWater) breakdown += $" | {results.MainCoilWaterFlow * 15.85:F1} GPM";
                    ResCoolingBreakdown = breakdown;

                    ResReheat = $"{Units.KwToMbh(results.ReheatLoad):F1} MBH";
                    if (IsReheatWater) ResReheat += $" ({results.ReheatWaterFlow * 15.85:F1} GPM)";
                    else if (IsReheatGas) ResReheat += $" ({results.GasConsumption * 35.31:F1} SCFH)";

                    ResFanPower = $"{results.TotalFanPowerKW:F2} kW";
                    ResFanBreakdown = $"S: {results.SupFanPowerKW:F2} | E: {results.ExtFanPowerKW:F2} kW";
                }
                else
                {
                    ResCooling = $"{results.TotalCooling:F1} kW";
                    string breakdown = $"S: {results.SensibleCooling:F1} | L: {results.LatentCooling:F1} kW";
                    if (IsMainCoilWater) breakdown += $" | {results.MainCoilWaterFlow:F2} L/s";
                    ResCoolingBreakdown = breakdown;

                    ResReheat = $"{results.ReheatLoad:F1} kW";
                    if (IsReheatWater) ResReheat += $" ({results.ReheatWaterFlow:F2} L/s)";
                    else if (IsReheatGas) ResReheat += $" ({results.GasConsumption:F2} m3/h)";

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

        public void LoadProject(ProjectData data)
        {
            _isIp = data.IsIpUnits;
            Altitude = double.TryParse(data.Altitude, out var a) ? a : 0;
            OaFlow = double.TryParse(data.OaFlow, out var of) ?  of : 1000;
            OaDb = double.TryParse(data.OaDb, out var odb) ? odb : 35;
            OaWb = double.TryParse(data.OaWb, out var owb) ? owb : 28;
            WheelEnabled = data.WheelEnabled;
            WheelSens = double.TryParse(data.WheelSens, out var ws) ? ws : 75;
            WheelLat = double.TryParse(data.WheelLat, out var wl) ? wl : 70;
            EaFlow = double.TryParse(data.EaFlow, out var ef) ? ef : 800;
            EaDb = double.TryParse(data.EaDb, out var edb) ? edb : 24;
            EaRh = double.TryParse(data.EaRh, out var erh) ? erh : 50;
            DwEnabled = data.DoubleWheelEnabled;
            DwEff = double.TryParse(data.DwSens, out var ds) ? ds : 65;
            HpEnabled = data.HpEnabled;
            HpEff = double.TryParse(data.HpEff, out var hs) ? hs : 45;
            OffCoil = double.TryParse(data.OffCoil, out var oc) ? oc : 12;
            MainCoilType = (CoilType)data.CoilTypeIndex;
            MainCoilDeltaT = double.TryParse(data.CoilDeltaT, out var cdt) ? cdt : 5.5;
            ReheatEnabled = data.ReheatEnabled;
            SupplyTemp = double.TryParse(data.SupplyTemp, out var st) ? st : 20;
            ReheatType = (ReheatSource)data.ReheatTypeIndex;
            HwEwt = double.TryParse(data.HwEwt, out var ewt) ? ewt : 80;
            HwLwt = double.TryParse(data.HwLwt, out var lwt) ? lwt : 60;
            GasEfficiency = double.TryParse(data.GasEff, out var ge) ? ge : 80;
            SupEsp = double.TryParse(data.SupOaEsp, out var se) ? se : 500;
            ExtEsp = double.TryParse(data.ExtEaEsp, out var ee) ? ee : 500;
            FanEff = double.TryParse(data.FanEff, out var fe) ? fe : 60;

            OnPropertyChanged(nameof(IsIp));
            OnPropertyChanged(nameof(IsSi));
            UpdateUnitLabels();
            UpdateReheatVisibilities();
            ValidateAndCalculate();
        }

        public ProjectData GetProjectData()
        {
            return new ProjectData
            {
                IsIpUnits = IsIp,
                Altitude = Altitude.ToString(),
                OaFlow = OaFlow.ToString(),
                OaDb = OaDb.ToString(),
                OaWb = OaWb.ToString(),
                WheelEnabled = WheelEnabled,
                WheelSens = WheelSens.ToString(),
                WheelLat = WheelLat.ToString(),
                EaFlow = EaFlow.ToString(),
                EaDb = EaDb.ToString(),
                EaRh = EaRh.ToString(),
                DoubleWheelEnabled = DwEnabled,
                DwSens = DwEff.ToString(),
                HpEnabled = HpEnabled,
                HpEff = HpEff.ToString(),
                OffCoil = OffCoil.ToString(),
                CoilTypeIndex = (int)MainCoilType,
                CoilDeltaT = MainCoilDeltaT.ToString(),
                ReheatEnabled = ReheatEnabled,
                SupplyTemp = SupplyTemp.ToString(),
                ReheatTypeIndex = (int)ReheatType,
                HwEwt = HwEwt.ToString(),
                HwLwt = HwLwt.ToString(),
                GasEff = GasEfficiency.ToString(),
                SupOaEsp = SupEsp.ToString(),
                ExtEaEsp = ExtEsp.ToString(),
                FanEff = FanEff.ToString()
            };
        }

        private double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
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