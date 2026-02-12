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

    public class SystemInputs
    {
        public bool IsHeatingMode { get; set; }
        public double OaFlow { get; set; }
        public double OaDb { get; set; }
        public double OaWb { get; set; }
        public double Altitude { get; set; }

        public bool WheelEnabled { get; set; }
        public bool EconomizerEnabled { get; set; }
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
        
        public bool ReheatEnabled { get; set; }
        public double TargetSupplyTemp { get; set; }

        // Compliance
        public double SupOaEsp { get; set; }
        public double ExtEaEsp { get; set; }
        public double FanEff { get; set; }
    }

    public class ProcessStep
    {
        public string Component { get; set; }
        public AirState Entering { get; set; }
        public AirState Leaving { get; set; }
        // WinUI uses Microsoft.UI.Colors, not System.Drawing.Color, but for model simplicity we can use string hex or keep simple
        // Removing Color dependency from Model for clean separation is best, but for now let's just comment it out or change to string
        public string HighlightColorHex { get; set; } = "#000000"; 
    }

    public class SystemResults
    {
        public List<ProcessStep> Steps { get; set; } = new List<ProcessStep>();
        public Dictionary<string, AirState> ChartPoints { get; set; } = new Dictionary<string, AirState>();
        public double TotalCooling { get; set; } // kW
        public double SensibleCooling { get; set; }
        public double LatentCooling { get; set; }
        public double TotalHeating { get; set; } // kW
        public double ReheatLoad { get; set; }
        public double AirDensity { get; set; }
        public double SupInternalPd { get; set; }
        public double ExtInternalPd { get; set; }
        public double SupFanPowerKW { get; set; }
        public double ExtFanPowerKW { get; set; }
        public double SupMotorKW { get; set; }
        public double ExtMotorKW { get; set; }
        public double TotalFanPowerKW => SupFanPowerKW + ExtFanPowerKW;
    }
}