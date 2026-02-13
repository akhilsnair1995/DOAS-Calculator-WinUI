using System;
using System.Collections.Generic;

namespace DOASCalculatorWinUI
{
    public class AirState
    {
        public double T { get; set; } // Celsius
        public double W { get; set; } // kg/kg
        public string Name { get; set; }

        public AirState(double t, double w, string name = "")
        {
            T = t; W = w; Name = name;
        }

        public double Enthalpy => Psychrometrics.GetEnthalpy(T, W);
        public double Rh => Psychrometrics.GetRhFromW(T, W);
        public double Twb => Psychrometrics.GetWetBulb(T, W);

        public override string ToString() => $"{T:F1}°C / {Twb:F1}°C / {Rh:F0}%";
        
        public string ToIpString() => 
            $"{Units.CtoF(T):F1}°F / {Units.CtoF(Twb):F1}°F / {Rh:F0}%";
    }

    public enum CoilType { Dx, Water }
    public enum ReheatSource { Electric, HotWater, Gas }

    public class SystemInputs
    {
        public double OaFlow { get; set; }
        public double OaDb { get; set; }
        public double OaWb { get; set; }
        public double Altitude { get; set; }

        public bool WheelEnabled { get; set; }
        public double WheelSens { get; set; }
        public double WheelLat { get; set; }
        public double EaFlow { get; set; }
        public double EaDb { get; set; }
        public double EaRh { get; set; }

        public bool DoubleWheelEnabled { get; set; }
        public double DwSens { get; set; }

        public bool HpEnabled { get; set; }
        public double HpEff { get; set; }

        public double OffCoilTemp { get; set; }
        public CoilType MainCoilType { get; set; }
        public double MainCoilDeltaT { get; set; }
        
        public bool ReheatEnabled { get; set; }
        public double TargetSupplyTemp { get; set; }
        public ReheatSource ReheatType { get; set; }
        public double HwEwt { get; set; }
        public double HwLwt { get; set; }
        public double GasEfficiency { get; set; }

        // Compliance
        public double SupOaEsp { get; set; }
        public double ExtEaEsp { get; set; }
        public double FanEff { get; set; }

        // Customizable Pressure Drops (Pa)
        public double PdDamper { get; set; } = 50;
        public double PdFilterPre { get; set; } = 100;
        public double PdFilterMain { get; set; } = 250;
        public double PdCoil { get; set; } = 250;
    }

    public class ProcessStep
    {
        public string Component { get; set; } = "";
        public AirState Entering { get; set; } = new AirState(0,0);
        public AirState Leaving { get; set; } = new AirState(0,0);
        public string HighlightColorHex { get; set; } = "#000000"; 
    }

    public class SystemResults
    {
        public List<ProcessStep> Steps { get; set; } = new List<ProcessStep>();
        public Dictionary<string, AirState> ChartPoints { get; set; } = new Dictionary<string, AirState>();
        public double TotalCooling { get; set; } // kW
        public double SensibleCooling { get; set; }
        public double LatentCooling { get; set; }
        public double MainCoilWaterFlow { get; set; } // L/s
        public double ReheatLoad { get; set; } // kW
        public double ReheatWaterFlow { get; set; } // L/s
        public double GasConsumption { get; set; } // m3/h (approx)
        public double AirDensity { get; set; }
        public double SupInternalPd { get; set; }
        public double ExtInternalPd { get; set; }
        public double SupFanPowerKW { get; set; }
        public double ExtFanPowerKW { get; set; }
        public double SupMotorKW { get; set; }
        public double ExtMotorKW { get; set; }
        public double TotalFanPowerKW => SupFanPowerKW + ExtFanPowerKW;
    }

    public class ProjectData
    {
        public string OaFlow { get; set; } = "";
        public string OaDb { get; set; } = "";
        public string OaWb { get; set; } = "";
        public string Altitude { get; set; } = "";
        public bool WheelEnabled { get; set; }
        public string WheelSens { get; set; } = "";
        public string WheelLat { get; set; } = "";
        public string EaFlow { get; set; } = "";
        public string EaDb { get; set; } = "";
        public string EaRh { get; set; } = "";
        public bool DoubleWheelEnabled { get; set; } 
        public string DwSens { get; set; } = "";
        public bool HpEnabled { get; set; }
        public string HpEff { get; set; } = "";
        public string OffCoil { get; set; } = "";
        public int CoilTypeIndex { get; set; }
        public string CoilDeltaT { get; set; } = "";
        public bool ReheatEnabled { get; set; }
        public string SupplyTemp { get; set; } = "";
        public int ReheatTypeIndex { get; set; }
        public string HwEwt { get; set; } = "";
        public string HwLwt { get; set; } = "";
        public string GasEff { get; set; } = "";
        public string SupOaEsp { get; set; } = "";
        public string ExtEaEsp { get; set; } = "";
        public string FanEff { get; set; } = "";
        public string DxEff { get; set; } = "";
        public bool IsIpUnits { get; set; }
    }
}