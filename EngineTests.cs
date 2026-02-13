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
    }
}