#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace DOASCalculatorWinUI
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; } = new MainViewModel();

        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = "DOAS Sizing Calculator (WinUI 3)";
            ViewModel.OnCalculationError += ShowErrorDialog;

#if DEBUG
            EngineTests.RunAllTests();
#endif
        }

        private async void ShowErrorDialog(string message)
        {
            var root = this.Content as FrameworkElement;
            if (root?.XamlRoot != null)
            {
                var dialog = new ContentDialog
                {
                    Title = "Calculation Error",
                    Content = $"System logic could not be processed. Please verify all inputs (Efficiency and Flow must be non-zero).\n\nDetails: {message}",
                    CloseButtonText = "OK",
                    XamlRoot = root.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }

        public Visibility BoolToVis(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

        private void Calculate_Click(object? sender, RoutedEventArgs? e) => ViewModel.Calculate();

        private void Units_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsIp = !ViewModel.IsIp;
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
            if (file != null) { try { string json = await Windows.Storage.FileIO.ReadTextAsync(file); var data = System.Text.Json.JsonSerializer.Deserialize<ProjectData>(json); if (data != null) ViewModel.LoadProject(data); } catch { } }
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
            if (file != null) { var data = ViewModel.GetProjectData(); string json = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }); await Windows.Storage.FileIO.WriteTextAsync(file, json); }
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Exit();
    }

    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class ScheduleItem
    {
        public string Component { get; set; } = "";
        public string Entering { get; set; } = "";
        public string Leaving { get; set; } = "";
    }

    public class MainViewModel : ViewModelBase
    {
        public event Action<string>? OnCalculationError;
        private bool _isIp = false;
        public bool IsIp { get => _isIp; set { if (SetProperty(ref _isIp, value)) { OnPropertyChanged(nameof(IsSi)); UpdateUnitLabels(); ConvertUnits(value); ValidateAndCalculate(); } } }
        public bool IsSi => !IsIp;

        private Dictionary<string, string> _errors = new Dictionary<string, string>();
        public bool HasError(string propertyName) => _errors.ContainsKey(propertyName);
        public string GetError(string propertyName) => _errors.ContainsKey(propertyName) ? _errors[propertyName] : "";

        // Inputs
        private double _altitude = 0; public double Altitude { get => _altitude; set { if (SetProperty(ref _altitude, value)) ValidateAndCalculate(); } }
        private double _oaFlow = 1000; public double OaFlow { get => _oaFlow; set { if (SetProperty(ref _oaFlow, value)) ValidateAndCalculate(); } }
        private double _oaDb = 35.0; public double OaDb { get => _oaDb; set { if (SetProperty(ref _oaDb, value)) ValidateAndCalculate(); } }
        private double _oaWb = 28.0; public double OaWb { get => _oaWb; set { if (SetProperty(ref _oaWb, value)) ValidateAndCalculate(); } }
        private double _eaFlow = 800; public double EaFlow { get => _eaFlow; set { if (SetProperty(ref _eaFlow, value)) ValidateAndCalculate(); } }
        private double _eaDb = 24.0; public double EaDb { get => _eaDb; set { if (SetProperty(ref _eaDb, value)) ValidateAndCalculate(); } }
        private double _eaRh = 50.0; public double EaRh { get => _eaRh; set { if (SetProperty(ref _eaRh, value)) ValidateAndCalculate(); } }
        private bool _wheelEnabled = true; public bool WheelEnabled { get => _wheelEnabled; set { if (SetProperty(ref _wheelEnabled, value)) ValidateAndCalculate(); } }
        private double _wheelSens = 75; public double WheelSens { get => _wheelSens; set { if (SetProperty(ref _wheelSens, value)) ValidateAndCalculate(); } }
        private double _wheelLat = 70; public double WheelLat { get => _wheelLat; set { if (SetProperty(ref _wheelLat, value)) ValidateAndCalculate(); } }
        private bool _dwEnabled = false; public bool DwEnabled { get => _dwEnabled; set { if (SetProperty(ref _dwEnabled, value)) ValidateAndCalculate(); } }
        private double _dwEff = 65; public double DwEff { get => _dwEff; set { if (SetProperty(ref _dwEff, value)) ValidateAndCalculate(); } }
        private bool _hpEnabled = false; public bool HpEnabled { get => _hpEnabled; set { if (SetProperty(ref _hpEnabled, value)) ValidateAndCalculate(); } }
        private double _hpEff = 45; public double HpEff { get => _hpEff; set { if (SetProperty(ref _hpEff, value)) ValidateAndCalculate(); } }
        private double _offCoil = 12.0; public double OffCoil { get => _offCoil; set { if (SetProperty(ref _offCoil, value)) ValidateAndCalculate(); } }
        private CoilType _mainCoilType = CoilType.Dx; public CoilType MainCoilType { get => _mainCoilType; set { if (SetProperty(ref _mainCoilType, value)) { UpdateVisibilities(); ValidateAndCalculate(); } } }
        public int MainCoilTypeIndex { get => (int)MainCoilType; set => MainCoilType = (CoilType)value; }
        private double _mainCoilDeltaT = 5.5; public double MainCoilDeltaT { get => _mainCoilDeltaT; set { if (SetProperty(ref _mainCoilDeltaT, value)) ValidateAndCalculate(); } }
        private double _fanEff = 60; public double FanEff { get => _fanEff; set { if (SetProperty(ref _fanEff, value)) ValidateAndCalculate(); } }
        private double _motorEff = 93; public double MotorEff { get => _motorEff; set { if (SetProperty(ref _motorEff, value)) ValidateAndCalculate(); } }
        private double _driveEff = 95; public double DriveEff { get => _driveEff; set { if (SetProperty(ref _driveEff, value)) ValidateAndCalculate(); } }
        private bool _reheatEnabled = false; public bool ReheatEnabled { get => _reheatEnabled; set { if (SetProperty(ref _reheatEnabled, value)) { UpdateVisibilities(); ValidateAndCalculate(); } } }
        private ReheatSource _reheatType = ReheatSource.Electric; public ReheatSource ReheatType { get => _reheatType; set { if (SetProperty(ref _reheatType, value)) { UpdateVisibilities(); ValidateAndCalculate(); } } }
        public int ReheatTypeIndex { get => (int)ReheatType; set => ReheatType = (ReheatSource)value; }
        private double _supplyTemp = 20.0; public double SupplyTemp { get => _supplyTemp; set { if (SetProperty(ref _supplyTemp, value)) ValidateAndCalculate(); } }
        private double _hwEwt = 80; public double HwEwt { get => _hwEwt; set { if (SetProperty(ref _hwEwt, value)) ValidateAndCalculate(); } }
        private double _hwLwt = 60; public double HwLwt { get => _hwLwt; set { if (SetProperty(ref _hwLwt, value)) ValidateAndCalculate(); } }
        private double _gasEfficiency = 80; public double GasEfficiency { get => _gasEfficiency; set { if (SetProperty(ref _gasEfficiency, value)) ValidateAndCalculate(); } }
        private double _dxEfficiency = 3.5; public double DxEfficiency { get => _dxEfficiency; set { if (SetProperty(ref _dxEfficiency, value)) ValidateAndCalculate(); } }
        private bool _autoAshrae = true; public bool AutoAshrae { get => _autoAshrae; set { if (SetProperty(ref _autoAshrae, value)) ValidateAndCalculate(); } }
        private double _supEsp = 500; public double SupEsp { get => _supEsp; set { if (SetProperty(ref _supEsp, value)) ValidateAndCalculate(); } }
        private double _extEsp = 500; public double ExtEsp { get => _extEsp; set { if (SetProperty(ref _extEsp, value)) ValidateAndCalculate(); } }
        private double _pdDamper = 50; public double PdDamper { get => _pdDamper; set { if (SetProperty(ref _pdDamper, value)) ValidateAndCalculate(); } }
        private double _pdFilterPre = 100; public double PdFilterPre { get => _pdFilterPre; set { if (SetProperty(ref _pdFilterPre, value)) ValidateAndCalculate(); } }
        private double _pdFilterMain = 250; public double PdFilterMain { get => _pdFilterMain; set { if (SetProperty(ref _pdFilterMain, value)) ValidateAndCalculate(); } }
        private double _pdCoil = 250; public double PdCoil { get => _pdCoil; set { if (SetProperty(ref _pdCoil, value)) ValidateAndCalculate(); } }

        // Results
        private string _resCooling = "0.0 kW"; public string ResCooling { get => _resCooling; set => SetProperty(ref _resCooling, value); }
        private string _resCoolingBreakdown = "Sensible: 0.0 | Latent: 0.0"; public string ResCoolingBreakdown { get => _resCoolingBreakdown; set => SetProperty(ref _resCoolingBreakdown, value); }
        private string _resReheat = "0.0 kW"; public string ResReheat { get => _resReheat; set => SetProperty(ref _resReheat, value); }
        private string _resFanPower = "0.0 kW"; public string ResFanPower { get => _resFanPower; set => SetProperty(ref _resFanPower, value); }
        private string _resFanBreakdown = "Sup Fan: 0.0 | Ext Fan: 0.0"; public string ResFanBreakdown { get => _resFanBreakdown; set => SetProperty(ref _resFanBreakdown, value); }
        private string _resDxUnitPower = ""; public string ResDxUnitPower { get => _resDxUnitPower; set => SetProperty(ref _resDxUnitPower, value); }

        public ObservableCollection<ScheduleItem> Schedule { get; } = new ObservableCollection<ScheduleItem>();

        // Visibility Flags
        public bool IsErrorAlt => HasError(nameof(Altitude));
        public bool IsErrorOaFlow => HasError(nameof(OaFlow));
        public bool IsErrorOaDb => HasError(nameof(OaDb));
        public bool IsErrorOaWb => HasError(nameof(OaWb));
        public bool IsErrorWheelS => HasError(nameof(WheelSens));
        public bool IsErrorWheelL => HasError(nameof(WheelLat));
        public bool IsErrorFan => HasError(nameof(FanEff));
        public bool IsErrorDx => HasError(nameof(DxEfficiency));
        public bool IsMainWater => MainCoilType == CoilType.Water;
        public bool IsMainDx => MainCoilType == CoilType.Dx;
        public bool IsReheatTarget => ReheatEnabled;
        public bool IsReheatWater => ReheatEnabled && ReheatType == ReheatSource.HotWater;
        public bool IsReheatGas => ReheatEnabled && ReheatType == ReheatSource.Gas;
        public bool IsWheelFields => WheelEnabled;
        public bool IsDwFields => DwEnabled;
        public bool IsHpFields => HpEnabled;

        public string ErrorAltitude => GetError(nameof(Altitude));
        public string ErrorOaFlow => GetError(nameof(OaFlow));
        public string ErrorOaDb => GetError(nameof(OaDb));
        public string ErrorOaWb => GetError(nameof(OaWb));
        public string ErrorWheelSens => GetError(nameof(WheelSens));
        public string ErrorWheelLat => GetError(nameof(WheelLat));
        public string ErrorFanEff => GetError(nameof(FanEff));
        public string ErrorDxEfficiency => GetError(nameof(DxEfficiency));

        public string LabelAltitude => IsIp ? "Altitude (ft)" : "Altitude (m)";
        public string LabelFlow => IsIp ? "Flow (CFM)" : "Flow (L/s)";
        public string LabelTemp => IsIp ? "Temp (°F)" : "Temp (°C)";
        public string LabelPress => IsIp ? "Pressure (in.wg)" : "Pressure (Pa)";

        public MainViewModel() { ValidateAndCalculate(); }

        private void UpdateVisibilities()
        {
            OnPropertyChanged(nameof(IsErrorAlt)); OnPropertyChanged(nameof(IsErrorOaFlow)); OnPropertyChanged(nameof(IsErrorOaDb)); OnPropertyChanged(nameof(IsErrorOaWb)); OnPropertyChanged(nameof(IsErrorWheelS)); OnPropertyChanged(nameof(IsErrorWheelL)); OnPropertyChanged(nameof(IsErrorFan)); OnPropertyChanged(nameof(IsErrorDx));
            OnPropertyChanged(nameof(IsMainWater)); OnPropertyChanged(nameof(IsMainDx)); OnPropertyChanged(nameof(IsReheatTarget)); OnPropertyChanged(nameof(IsReheatWater)); OnPropertyChanged(nameof(IsReheatGas)); OnPropertyChanged(nameof(IsWheelFields)); OnPropertyChanged(nameof(IsDwFields)); OnPropertyChanged(nameof(IsHpFields));
            OnPropertyChanged(nameof(MainCoilTypeIndex)); OnPropertyChanged(nameof(ReheatTypeIndex));
        }

        private void ValidateAndCalculate()
        {
            _errors.Clear();
            double altSi = IsIp ? Units.FtToM(Altitude) : Altitude;
            if (altSi < -500 || altSi > 5000) _errors[nameof(Altitude)] = IsIp ? "Range: -1640 to 16400 ft" : "Range: -500 to 5000 m";
            if ((IsIp ? Units.CfmToLps(OaFlow) : OaFlow) <= 0) _errors[nameof(OaFlow)] = "Must be > 0";
            double dbSi = IsIp ? Units.FtoC(OaDb) : OaDb;
            if (dbSi < -50 || dbSi > 60) _errors[nameof(OaDb)] = "Range: -50 to 60 °C";
            if ((IsIp ? Units.FtoC(OaWb) : OaWb) > dbSi) _errors[nameof(OaWb)] = "WB > DB";
            if (WheelSens < 0 || WheelSens > 100) _errors[nameof(WheelSens)] = "0-100%";
            if (WheelLat < 0 || WheelLat > 100) _errors[nameof(WheelLat)] = "0-100%";
            if (FanEff < 10 || FanEff > 95) _errors[nameof(FanEff)] = "10-95%";
            if (MotorEff < 50 || MotorEff > 99) _errors[nameof(MotorEff)] = "50-99%";
            if (DriveEff < 50 || DriveEff > 100) _errors[nameof(DriveEff)] = "50-100%";
            if (DxEfficiency <= 0) _errors[nameof(DxEfficiency)] = "Must be > 0";
            UpdateVisibilities();
            if (_errors.Count == 0) Calculate();
        }

        public void Calculate()
        {
            try {
                var inputs = new SystemInputs { Altitude = IsIp ? Units.FtToM(Altitude) : Altitude, OaFlow = IsIp ? Units.CfmToLps(OaFlow) : OaFlow, OaDb = IsIp ? Units.FtoC(OaDb) : OaDb, OaWb = IsIp ? Units.FtoC(OaWb) : OaWb, EaFlow = IsIp ? Units.CfmToLps(EaFlow) : EaFlow, EaDb = IsIp ? Units.FtoC(EaDb) : EaDb, EaRh = EaRh, WheelEnabled = WheelEnabled, WheelSens = WheelSens, WheelLat = WheelLat, DoubleWheelEnabled = DwEnabled, DwSens = DwEff, HpEnabled = HpEnabled, HpEff = HpEff, OffCoilTemp = IsIp ? Units.FtoC(OffCoil) : OffCoil, MainCoilType = MainCoilType, MainCoilDeltaT = IsIp ? MainCoilDeltaT / 1.8 : MainCoilDeltaT, ReheatEnabled = ReheatEnabled, ReheatType = ReheatType, TargetSupplyTemp = IsIp ? Units.FtoC(SupplyTemp) : SupplyTemp, HwEwt = IsIp ? Units.FtoC(HwEwt) : HwEwt, HwLwt = IsIp ? Units.FtoC(HwLwt) : HwLwt, GasEfficiency = GasEfficiency, SupOaEsp = IsIp ? Units.InWgToPa(SupEsp) : SupEsp, ExtEaEsp = IsIp ? Units.InWgToPa(ExtEsp) : ExtEsp, FanEff = FanEff, MotorEff = MotorEff, DriveEff = DriveEff, PdDamper = IsIp ? Units.InWgToPa(PdDamper) : PdDamper, PdFilterPre = IsIp ? Units.InWgToPa(PdFilterPre) : PdFilterPre, PdFilterMain = IsIp ? Units.InWgToPa(PdFilterMain) : PdFilterMain, PdCoil = IsIp ? Units.InWgToPa(PdCoil) : PdCoil };
                var results = DOASEngine.Process(inputs);
                if (AutoAshrae && MainCoilType == CoilType.Dx) { double eer = GetAshraeMinEER(Units.KwToBtuH(results.TotalCooling), ReheatEnabled && ReheatType == ReheatSource.Gas); _dxEfficiency = IsIp ? eer : Units.EerToCop(eer); OnPropertyChanged(nameof(DxEfficiency)); }
                if (MainCoilType == CoilType.Dx) { var dx = DxCalculator.GetAshraePerformance(results.TotalCooling); ResDxUnitPower = $"DX Unit: {dx.ElectricalPowerKw:F1} kW (ASHRAE EER: {dx.MinEer})"; } else ResDxUnitPower = "";
                
                if (IsIp) { 
                    ResCooling = $"{Units.KwToMbh(results.TotalCooling):F1} MBH"; 
                    string breakdown = $"Sensible: {Units.KwToMbh(results.SensibleCooling):F1} MBH | Latent: {Units.KwToMbh(results.LatentCooling):F1} MBH"; 
                    if (IsMainWater) breakdown += $" | Flow: {results.MainCoilWaterFlow * 15.85:F1} GPM"; 
                    ResCoolingBreakdown = breakdown; 
                    ResReheat = $"{Units.KwToMbh(results.ReheatLoad):F1} MBH"; 
                    if (IsReheatWater) ResReheat += $" (Water Flow: {results.ReheatWaterFlow * 15.85:F1} GPM)"; 
                    else if (IsReheatGas) ResReheat += $" (Gas Consumption: {results.GasConsumption * 35.31:F1} SCFH)"; 
                }
                else { 
                    ResCooling = $"{results.TotalCooling:F1} kW"; 
                    string breakdown = $"Sensible: {results.SensibleCooling:F1} kW | Latent: {results.LatentCooling:F1} kW"; 
                    if (IsMainWater) breakdown += $" | Flow: {results.MainCoilWaterFlow:F2} L/s"; 
                    else breakdown += $" | Electrical: {(results.TotalCooling / (IsIp ? Units.EerToCop(DxEfficiency) : DxEfficiency)):F1} kW"; 
                    ResCoolingBreakdown = breakdown; 
                    ResReheat = $"{results.ReheatLoad:F1} kW"; 
                    if (IsReheatWater) ResReheat += $" (Water Flow: {results.ReheatWaterFlow:F2} L/s)"; 
                    else if (IsReheatGas) ResReheat += $" (Gas Consumption: {results.GasConsumption:F2} m3/h)"; 
                }
                ResFanPower = $"{results.TotalFanPowerKW:F2} kW (Elec: {results.TotalElectricalPowerKW:F2} kW)"; 
                ResFanBreakdown = $"Sup Fan: {results.SupFanPowerKW:F2} kW (Elec: {results.SupElectricalPowerKW:F2} kW) | Ext Fan: {results.ExtFanPowerKW:F2} kW (Elec: {results.ExtElectricalPowerKW:F2} kW)";
                
                Schedule.Clear(); 
                foreach (var s in results.Steps) 
                {
                    Schedule.Add(new ScheduleItem { 
                        Component = s.Component, 
                        Entering = IsIp ? s.Entering.ToIpString() : s.Entering.ToString(), 
                        Leaving = IsIp ? s.Leaving.ToIpString() : s.Leaving.ToString() 
                    });
                }
            } 
            catch (Exception ex) {
                OnCalculationError?.Invoke(ex.Message);
            }
        }

        private double GetAshraeMinEER(double btu, bool gas) { if (btu < 65000) return 12.0; if (btu < 135000) return gas ? 11.0 : 11.2; if (btu < 240000) return gas ? 10.8 : 11.0; return gas ? 9.8 : 10.0; }

        public void LoadProject(ProjectData data) { _isIp = data.IsIpUnits; Altitude = double.TryParse(data.Altitude, out var a) ? a : 0; OaFlow = double.TryParse(data.OaFlow, out var of) ? of : 1000; OaDb = double.TryParse(data.OaDb, out var odb) ? odb : 35; OaWb = double.TryParse(data.OaWb, out var owb) ? owb : 28; WheelEnabled = data.WheelEnabled; WheelSens = double.TryParse(data.WheelSens, out var ws) ? ws : 75; WheelLat = double.TryParse(data.WheelLat, out var wl) ? wl : 70; EaFlow = double.TryParse(data.EaFlow, out var ef) ? ef : 800; EaDb = double.TryParse(data.EaDb, out var edb) ? edb : 24; EaRh = double.TryParse(data.EaRh, out var erh) ? erh : 50; DwEnabled = data.DoubleWheelEnabled; DwEff = double.TryParse(data.DwSens, out var ds) ? ds : 65; HpEnabled = data.HpEnabled; HpEff = double.TryParse(data.HpEff, out var hs) ? hs : 45; OffCoil = double.TryParse(data.OffCoil, out var oc) ? oc : 12; MainCoilType = (CoilType)data.CoilTypeIndex; MainCoilDeltaT = double.TryParse(data.CoilDeltaT, out var cdt) ? cdt : 5.5; ReheatEnabled = data.ReheatEnabled; SupplyTemp = double.TryParse(data.SupplyTemp, out var st) ? st : 20; ReheatType = (ReheatSource)data.ReheatTypeIndex; HwEwt = double.TryParse(data.HwEwt, out var ewt) ? ewt : 80; HwLwt = double.TryParse(data.HwLwt, out var lwt) ? lwt : 60; GasEfficiency = double.TryParse(data.GasEff, out var ge) ? ge : 80; SupEsp = double.TryParse(data.SupOaEsp, out var se) ? se : 500; ExtEsp = double.TryParse(data.ExtEaEsp, out var ee) ? ee : 500; FanEff = double.TryParse(data.FanEff, out var fe) ? fe : 60; MotorEff = double.TryParse(data.MotorEff, out var me) ? me : 93; DriveEff = double.TryParse(data.DriveEff, out var de) ? de : 95; UpdateUnitLabels(); UpdateVisibilities(); ValidateAndCalculate(); }
        public ProjectData GetProjectData() => new ProjectData { IsIpUnits = IsIp, Altitude = Altitude.ToString(), OaFlow = OaFlow.ToString(), OaDb = OaDb.ToString(), OaWb = OaWb.ToString(), WheelEnabled = WheelEnabled, WheelSens = WheelSens.ToString(), WheelLat = WheelLat.ToString(), EaFlow = EaFlow.ToString(), EaDb = EaDb.ToString(), EaRh = EaRh.ToString(), DoubleWheelEnabled = DwEnabled, DwSens = DwEff.ToString(), HpEnabled = HpEnabled, HpEff = HpEff.ToString(), OffCoil = OffCoil.ToString(), CoilTypeIndex = (int)MainCoilType, CoilDeltaT = MainCoilDeltaT.ToString(), ReheatEnabled = ReheatEnabled, SupplyTemp = SupplyTemp.ToString(), ReheatTypeIndex = (int)ReheatType, HwEwt = HwEwt.ToString(), HwLwt = HwLwt.ToString(), GasEff = GasEfficiency.ToString(), SupOaEsp = SupEsp.ToString(), ExtEaEsp = ExtEsp.ToString(), FanEff = FanEff.ToString(), MotorEff = MotorEff.ToString(), DriveEff = DriveEff.ToString() };
        private void UpdateUnitLabels() { OnPropertyChanged(nameof(LabelAltitude)); OnPropertyChanged(nameof(LabelFlow)); OnPropertyChanged(nameof(LabelTemp)); OnPropertyChanged(nameof(LabelPress)); }
        private void ConvertUnits(bool toIp) { Altitude = toIp ? Units.MToFt(Altitude) : Units.FtToM(Altitude); OaFlow = toIp ? Units.LpsToCfm(OaFlow) : Units.CfmToLps(OaFlow); OaDb = toIp ? Units.CtoF(OaDb) : Units.FtoC(OaDb); OaWb = toIp ? Units.CtoF(OaWb) : Units.FtoC(OaWb); EaFlow = toIp ? Units.LpsToCfm(EaFlow) : Units.CfmToLps(EaFlow); EaDb = toIp ? Units.CtoF(EaDb) : Units.FtoC(EaDb); OffCoil = toIp ? Units.CtoF(OffCoil) : Units.FtoC(OffCoil); SupplyTemp = toIp ? Units.CtoF(SupplyTemp) : Units.FtoC(SupplyTemp); SupEsp = toIp ? Units.PaToInWg(SupEsp) : Units.InWgToPa(SupEsp); ExtEsp = toIp ? Units.PaToInWg(ExtEsp) : Units.InWgToPa(ExtEsp); PdDamper = toIp ? Units.PaToInWg(PdDamper) : Units.InWgToPa(PdDamper); PdFilterPre = toIp ? Units.PaToInWg(PdFilterPre) : Units.InWgToPa(PdFilterPre); PdFilterMain = toIp ? Units.PaToInWg(PdFilterMain) : Units.InWgToPa(PdFilterMain); PdCoil = toIp ? Units.PaToInWg(PdCoil) : Units.InWgToPa(PdCoil); DxEfficiency = toIp ? Units.CopToEer(DxEfficiency) : Units.EerToCop(DxEfficiency); }
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language) => (value is bool b && b) ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type targetType, object parameter, string language) => value is Visibility v && v == Visibility.Visible;
    }
}