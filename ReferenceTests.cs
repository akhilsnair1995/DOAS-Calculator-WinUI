using System;
using System.Diagnostics;
using System.Linq;

namespace DOASCalculatorWinUI
{
    public static class ReferenceTests
    {
        public static void Run()
        {
            Console.WriteLine("\n=== REFERENCE DATA COMPARISON (PDF VALUES) ===");
            CompareHeatPipeUnit();
            CompareDoubleWheelUnit();
            Console.WriteLine("==============================================");
        }

        private static void CompareHeatPipeUnit()
        {
            // P1-FAHU-01 from PDF Schedule
            Console.WriteLine("\nTesting P1-FAHU-01 (Heat Pipe + EW)");
            var inputs = new SystemInputs
            {
                Altitude = 0,
                OaFlow = 3300,
                OaDb = 46.0,
                OaWb = 29.0,
                EaFlow = 3300,
                EaDb = 24.0,
                EaRh = 58.0, // RA 24/18 is ~58% RH
                
                WheelEnabled = true,
                WheelSens = 54.1, 
                WheelLat = 43.0,  
                
                HpEnabled = true,
                HpEff = 51.9, 
                
                OffCoilTemp = 13.5,
                MainCoilType = CoilType.Water,
                MainCoilDeltaT = 5.5, // 14.5 - 9.0
                
                FanEff = 65,
                SupOaEsp = 500
            };

            var res = DOASEngine.Process(inputs);

            var ewStep = res.Steps.FirstOrDefault(s => s.Component == "Enthalpy Wheel (OA)");
            var hpStep = res.Steps.FirstOrDefault(s => s.Component == "HP Pre-Cool");
            var coilStep = res.Steps.FirstOrDefault(s => s.Component == "Cooling Coil");
            var sa = res.ChartPoints["SA"];

            Console.WriteLine("Results for P1-FAHU-01:");
            Console.WriteLine("  EW Leaving: " + (ewStep?.Leaving?.ToString() ?? "N/A"));
            Console.WriteLine("  HP Pre-cool Leaving: " + (hpStep?.Leaving?.ToString() ?? "N/A"));
            Console.WriteLine("  Coil Leaving: " + (coilStep?.Leaving?.ToString() ?? "N/A"));
            Console.WriteLine("  Total Cooling: " + res.TotalCooling.ToString("F1") + " kW (Schedule: 67.5 kW)");
            Console.WriteLine("  Sensible Cooling: " + res.SensibleCooling.ToString("F1") + " kW (Schedule: 39.5 kW)");
            Console.WriteLine("  Final Supply Temp: " + sa.T.ToString("F1") + "°C (Schedule: 24.3°C)");
        }

        private static void CompareDoubleWheelUnit()
        {
            // P4-FAHU-01 from PDF Schedule
            Console.WriteLine("\nTesting P4-FAHU-01 (Double Wheel)");
            var inputs = new SystemInputs
            {
                Altitude = 0,
                OaFlow = 2345,
                OaDb = 46.0,
                OaWb = 29.0,
                EaFlow = 2345,
                EaDb = 24.0,
                EaRh = 58.0,
                
                WheelEnabled = true,
                WheelSens = 54.1,
                WheelLat = 43.0,
                
                DoubleWheelEnabled = true,
                DwSens = 68.3, 
                
                OffCoilTemp = 12.0,
                MainCoilType = CoilType.Water,
                MainCoilDeltaT = 5.5,
                
                FanEff = 65,
                SupOaEsp = 500
            };

            var res = DOASEngine.Process(inputs);

            var ewStep = res.Steps.FirstOrDefault(s => s.Component == "Enthalpy Wheel (OA)");
            var sa = res.ChartPoints["SA"];

            Console.WriteLine("Results for P4-FAHU-01:");
            Console.WriteLine("  EW Leaving: " + (ewStep?.Leaving?.ToString() ?? "N/A"));
            Console.WriteLine("  Total Cooling: " + res.TotalCooling.ToString("F1") + " kW (Schedule: 102.4 kW)");
            Console.WriteLine("  Sensible Cooling: " + res.SensibleCooling.ToString("F1") + " kW (Schedule: 61.4 kW)");
            Console.WriteLine("  Final Supply Temp: " + sa.T.ToString("F1") + "°C (Schedule: 20.2°C)");
        }
    }
}
