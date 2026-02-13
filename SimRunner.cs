using System;
using DOASCalculatorWinUI;

namespace Simulation
{
    class Program
    {
        static void RunSimulation()
        {
            // Inputs based on Trosten TAH-270 TFM PDF Data
            var inputs = new SystemInputs
            {
                Altitude = 0,
                // Using Standard Air Volume (45054 m3/h) to compare against 392kW manufacturer load
                OaFlow = 12515, 
                OaDb = 34,
                OaWb = 32,
                // Extract Standard Volume (38257 m3/h)
                EaFlow = 10627, 
                EaDb = 25, // Return Air DB
                EaRh = 55.1, // Return Air RH
                WheelEnabled = true,
                WheelSens = 65.5,
                WheelLat = 68.8,
                DoubleWheelEnabled = true,
                DwSens = 68.3,
                HpEnabled = false,
                OffCoilTemp = 14.01,
                MainCoilType = CoilType.Water,
                MainCoilDeltaT = 9.0, // 5.5 to 14.5
                ReheatEnabled = false,
                FanEff = 81.9,
                MotorEff = 93.0,
                DriveEff = 89.0,
                SupOaEsp = 1000,
                ExtEaEsp = 1000
            };

            var results = DOASEngine.Process(inputs);

            Console.WriteLine($"TOTAL COOLING LOAD: {results.TotalCooling:F2} kW");
            Console.WriteLine($"SENSIBLE: {results.SensibleCooling:F2} kW | LATENT: {results.LatentCooling:F2} kW");
            Console.WriteLine($"CHW FLOW: {results.MainCoilWaterFlow:F2} L/s");
            Console.WriteLine($"SUPPLY FAN POWER (Abs): {results.SupFanPowerKW:F2} kW");
            Console.WriteLine($"SUPPLY ELEC POWER (Input): {results.SupElectricalPowerKW:F2} kW");
            Console.WriteLine($"EXTRACT FAN POWER (Abs): {results.ExtFanPowerKW:F2} kW");
            Console.WriteLine($"EXTRACT ELEC POWER (Input): {results.ExtElectricalPowerKW:F2} kW");
            Console.WriteLine($"TOTAL ELEC POWER: {results.TotalElectricalPowerKW:F2} kW");
            Console.WriteLine("--- TRANSITION SCHEDULE ---");
            foreach (var step in results.Steps)
            {
                Console.WriteLine($"{step.Component}: {step.Entering} -> {step.Leaving}");
            }
        }
    }
}
