using System;
using System.Diagnostics;
using System.Linq;

namespace DOASCalculatorWinUI
{
    public static class EngineTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== DOAS ENGINE TEST SUITE ===");
            
            TestPsychrometrics();
            TestFullProcess();
            TestFanHeatGain();
            
            Console.WriteLine("=== TESTS COMPLETED ===");
        }

        private static void TestPsychrometrics()
        {
            Console.Write("Testing Psychrometrics (Buck 1981)... ");
            Psychrometrics.SetAltitude(0);
            
            // Standard point: 35C / 28C WB
            double pv = Psychrometrics.GetPvFromTwb(35, 28);
            double w = Psychrometrics.GetHumidityRatio(pv);
            double rh = Psychrometrics.GetRhFromW(35, w);
            
            // Expected RH at 35/28 is approx 59%
            Debug.Assert(rh > 58 && rh < 60, "RH calculation drift detected");
            Console.WriteLine("PASSED");
        }

        private static void TestFullProcess()
        {
            Console.Write("Testing Full DOAS Process Flow... ");
            var inputs = new SystemInputs
            {
                Altitude = 0,
                OaFlow = 1000, // L/s
                OaDb = 35,
                OaWb = 28,
                EaFlow = 1000,
                EaDb = 24,
                EaRh = 50,
                WheelEnabled = true,
                WheelSens = 75,
                WheelLat = 70,
                OffCoilTemp = 12,
                MainCoilType = CoilType.Dx,
                FanEff = 60,
                SupOaEsp = 500
            };

            var results = DOASEngine.Process(inputs);

            Debug.Assert(results.TotalCooling > 0, "Cooling load should be positive");
            Debug.Assert(results.Steps.Any(s => s.Component == "Enthalpy Wheel"), "Wheel step missing");
            Debug.Assert(results.Steps.Any(s => s.Component == "Cooling Coil"), "Coil step missing");
            
            Console.WriteLine("PASSED");
        }

        private static void TestFanHeatGain()
        {
            Console.Write("Testing Supply Fan Heat Gain Logic... ");
            var inputs = new SystemInputs
            {
                Altitude = 0,
                OaFlow = 1000,
                OaDb = 30,
                OaWb = 20,
                OffCoilTemp = 12,
                FanEff = 60,
                SupOaEsp = 1000 // High pressure to see significant heat
            };

            var results = DOASEngine.Process(inputs);
            
            var fanStep = results.Steps.FirstOrDefault(s => s.Component == "Supply Fan Heat");
            Debug.Assert(fanStep != null, "Fan heat step not generated");
            
            double dT = fanStep.Leaving.T - fanStep.Entering.T;
            Debug.Assert(dT > 0, "Fan must increase air temperature");
            
            Console.WriteLine($"PASSED (dT = {dT:F2} K)");
        }
    }
}