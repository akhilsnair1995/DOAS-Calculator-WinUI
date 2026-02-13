using System;

namespace DOASCalculatorWinUI
{
    /// <summary>
    /// Self-contained ASHRAE 90.1 DX Efficiency and Power Calculator.
    /// Values based on Table 6.8.1-1 (Air-Cooled, Cooling Mode).
    /// </summary>
    public static class DxCalculator
    {
        public struct DxPerformance
        {
            public double MinEer;
            public double MinCop;
            public double ElectricalPowerKw;
        }

        /// <summary>
        /// Calculates the minimum required performance based on cooling capacity.
        /// </summary>
        /// <param name="coolingLoadKw">The required cooling capacity in kW.</param>
        /// <returns>A DxPerformance struct with EER, COP, and estimated kW input.</returns>
        public static DxPerformance GetAshraePerformance(double coolingLoadKw)
        {
            if (coolingLoadKw <= 0) return new DxPerformance();

            // ASHRAE 90.1 typically uses Btu/h for capacity brackets
            double btuH = coolingLoadKw * 3412.142;
            double eer;

            // Brackets for Air-Cooled DX (Cooling Only / Electric Heat)
            // Values from ASHRAE 90.1-2019/2022
            if (btuH < 65000) 
                eer = 12.0;
            else if (btuH < 135000) 
                eer = 11.2;
            else if (btuH < 240000) 
                eer = 11.0;
            else if (btuH < 760000) 
                eer = 10.0;
            else 
                eer = 9.7;

            double cop = eer / 3.412;

            return new DxPerformance
            {
                MinEer = eer,
                MinCop = cop,
                ElectricalPowerKw = coolingLoadKw / cop
            };
        }
    }
}