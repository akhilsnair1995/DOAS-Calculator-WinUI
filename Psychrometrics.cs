using System;

namespace DOASCalculatorWinUI
{
    public static class Psychrometrics
    {
        public static double PATM = 101.325; 

        public static void SetAltitude(double meters)
        {
            PATM = 101.325 * Math.Pow(1 - 2.25577e-5 * meters, 5.25588);
        }

        /// <summary>
        /// Saturation vapor pressure (kPa) using Buck formula (1981)
        /// </summary>
        public static double GetSatVapPres(double T)
        {
            if (T >= 0)
                return 0.61121 * Math.Exp((17.502 * T) / (T + 240.97));
            else
                return 0.61115 * Math.Exp((22.452 * T) / (T + 272.55));
        }

        public static double GetPvFromTwb(double Tdb, double Twb)
        {
            double Psat_wb = GetSatVapPres(Twb);
            // Psychrometric constant for water/air
            double A = 0.00066 * (1 + 0.00115 * Twb);
            return Psat_wb - PATM * A * (Tdb - Twb);
        }

        public static double GetPvFromRh(double Tdb, double Rh) => GetSatVapPres(Tdb) * (Rh / 100.0);

        public static double GetHumidityRatio(double Pv)
        {
            if (PATM - Pv <= 0) return 0.000001;
            double W = 0.621945 * Pv / (PATM - Pv);
            return Math.Max(0.0, W);
        }

        public static double GetEnthalpy(double T, double W) => 1.006 * T + W * (2501 + 1.86 * T);

        public static double GetDensity(double T) => PATM / (0.287 * (T + 273.15));

        public static double GetRhFromW(double T, double W)
        {
            if (W <= 0) return 0.0;
            double Pv = (W * PATM) / (0.621945 + W);
            double Psat = GetSatVapPres(T);
            if (Psat == 0) return 0.0;
            return Math.Clamp((Pv / Psat) * 100.0, 0, 100);
        }

        public static double GetWetBulb(double Tdb, double W)
        {
            double targetPv = (W * PATM) / (0.621945 + W);
            double twb = Tdb; // Initial guess
            
            // Newton-Raphson or Secant Method for better convergence
            // Using a simple but robust bisection as fallback, but Newton is faster
            for (int i = 0; i < 20; i++)
            {
                double pvCalc = GetPvFromTwb(Tdb, twb);
                double error = pvCalc - targetPv;
                if (Math.Abs(error) < 0.0001) break;

                // Numerical derivative
                double delta = 0.01;
                double pvNext = GetPvFromTwb(Tdb, twb + delta);
                double dPv = (pvNext - pvCalc) / delta;
                
                if (Math.Abs(dPv) < 1e-9) break;
                twb -= error / dPv;
            }
            return Math.Min(twb, Tdb);
        }

        public static double GetDewPoint(double Tdb, double Rh)
        {
            double pv = GetPvFromRh(Tdb, Rh);
            double low = -50, high = Tdb;
            for (int i = 0; i < 30; i++)
            {
                double mid = (low + high) / 2.0;
                if (GetSatVapPres(mid) < pv) low = mid; else high = mid;
            }
            return high;
        }
    }

    public static class Units
    {
        public static double CtoF(double c) => c * 1.8 + 32;
        public static double FtoC(double f) => (f - 32) / 1.8;
        public static double LpsToCfm(double lps) => lps / 0.471947;
        public static double CfmToLps(double cfm) => cfm * 0.471947;
        public static double KwToMbh(double kw) => kw * 3.412;
        public static double MToFt(double m) => m * 3.28084;
        public static double FtToM(double ft) => ft / 3.28084;
        public static double PaToInWg(double pa) => pa * 0.00401463;
        public static double InWgToPa(double inWg) => inWg / 0.00401463;
        public static double CopToEer(double cop) => cop * 3.412;
        public static double EerToCop(double eer) => eer / 3.412;
        public static double KwToBtuH(double kw) => kw * 3412.142;
    }
}