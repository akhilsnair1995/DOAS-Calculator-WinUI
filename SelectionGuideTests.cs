using System;
using System.Diagnostics;
using System.Linq;

namespace DOASCalculatorWinUI
{
    public static class SelectionGuideTests
    {
        public static void Run()
        {
            Console.WriteLine("=== SELECTION GUIDE DATA COMPARISON (DAIKIN FY17) ===");
            TestFahu1510x2800();
            Console.WriteLine("=====================================================");
        }

        private static void TestFahu1510x2800()
        {
            Console.WriteLine("Testing FAHU Model 1510x2800 (Heat Wheel + Heat Pipe)");
            
            var inputs = new SystemInputs
            {
                Altitude = 0,
                OaFlow = 7000,
                OaDb = 46.0,
                OaWb = 29.1,
                EaFlow = 7000, 
                EaDb = 24.0,
                EaRh = 50.0,
                
                WheelEnabled = true,
                WheelSens = 61.8, 
                WheelLat = 65.0,  
                
                HpEnabled = true,
                HpEff = 61.0, 
                
                OffCoilTemp = 13.7,
                MainCoilType = CoilType.Water,
                MainCoilDeltaT = 5.0,
                
                FanEff = 70,
                SupOaEsp = 500
            };

            var res = DOASEngine.Process(inputs);

            var ewStep = res.Steps.FirstOrDefault(s => s.Component == "Enthalpy Wheel");
            var hpPreStep = res.Steps.FirstOrDefault(s => s.Component == "HP Pre-Cool");
            var coilStep = res.Steps.FirstOrDefault(s => s.Component == "Cooling Coil");
            var sa = res.ChartPoints["SA"];

            Console.WriteLine("Results for 1510x2800:");
            Console.WriteLine("  EW Leaving: " + (ewStep?.Leaving?.ToString() ?? "N/A"));
            Console.WriteLine("  Coil Leaving: " + (coilStep?.Leaving?.ToString() ?? "N/A"));
            Console.WriteLine("  Final Supply (after HP Reheat): " + sa.T.ToString("F1") + "°C (Guide: 21.0°C)");
            Console.WriteLine("  Coil Total Cooling: " + res.TotalCooling.ToString("F1") + " kW");
            Console.WriteLine("  Coil Sensible Cooling: " + res.SensibleCooling.ToString("F1") + " kW");
        }
    }
}
