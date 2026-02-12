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

        public static double GetSatVapPres(double T) => 0.61094 * Math.Exp((17.625 * T) / (T + 243.04));

        public static double GetPvFromTwb(double Tdb, double Twb)
        {
            double Psat_wb = GetSatVapPres(Twb);
            double A = 0.00066 * (1 + 0.00115 * Twb);
            return Psat_wb - PATM * A * (Tdb - Twb);
        }

        public static double GetPvFromRh(double Tdb, double Rh) => GetSatVapPres(Tdb) * (Rh / 100.0);

        public static double GetHumidityRatio(double Pv)
        {
            if (PATM - Pv <= 0) return 0.0;
            double W = 0.622 * Pv / (PATM - Pv);
            return Math.Max(0.0, W);
        }

        public static double GetEnthalpy(double T, double W) => 1.006 * T + W * (2501 + 1.86 * T);

        public static double GetDensity(double T) => PATM / (0.287 * (T + 273.15));

        public static double GetRhFromW(double T, double W)
        {
            if (W <= 0) return 0.0;
            double Pv = (W * PATM) / (0.622 + W);
            double Psat = GetSatVapPres(T);
            if (Psat == 0) return 0.0;
            return Math.Min(100.0, (Pv / Psat) * 100.0);
        }

        public static double GetWetBulb(double Tdb, double W)
        {
            double low = -50, high = Tdb; 
            double targetPv = (W * PATM) / (0.622 + W);
            for (int i = 0; i < 50; i++)
            {
                double mid = (low + high) / 2.0;
                double pvCalc = GetPvFromTwb(Tdb, mid);
                if (pvCalc > targetPv) high = mid; else low = mid;
                if (Math.Abs(high - low) < 0.001) return mid;
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