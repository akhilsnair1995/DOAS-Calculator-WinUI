using System;
using System.Collections.Generic;

namespace DOASCalculatorWinUI
{
    public static class DOASEngine
    {
        public static SystemResults Process(SystemInputs input)
        {
            var res = new SystemResults();
            Psychrometrics.SetAltitude(input.Altitude);

            double oaPv = Psychrometrics.GetPvFromTwb(input.OaDb, input.OaWb);
            double oaW = Psychrometrics.GetHumidityRatio(oaPv);
            res.AirDensity = Psychrometrics.GetDensity(input.OaDb);
            double mDot = (input.OaFlow / 1000.0) * res.AirDensity; // kg/s

            AirState current = new AirState(input.OaDb, oaW, "OA");
            res.ChartPoints["OA"] = current;

            // EA Props for recovery
            double eaW = Psychrometrics.GetHumidityRatio(Psychrometrics.GetPvFromRh(input.EaDb, input.EaRh));
            AirState eaState = new AirState(input.EaDb, eaW, "EA");
            res.ChartPoints["EA"] = eaState;

            int ptIdx = 1;

            // 1. Enthalpy Wheel
            if (input.WheelEnabled)
            {
                double tOut = current.T - (input.WheelSens / 100.0) * (current.T - input.EaDb);
                double wOut = current.W - (input.WheelLat / 100.0) * (current.W - eaW);
                
                // Physics Clamp (Saturation)
                double wsat = Psychrometrics.GetHumidityRatio(Psychrometrics.GetSatVapPres(tOut));
                if (wOut > wsat) wOut = wsat;

                AirState next = new AirState(tOut, wOut, ptIdx.ToString());
                res.Steps.Add(new ProcessStep { Component = "Enthalpy Wheel", Entering = current, Leaving = next });
                res.ChartPoints[next.Name] = next;
                current = next; ptIdx++;
            }

            // 2. Pre-Cool / Pre-Heat Recovery (Sensible only or Wrap-around)
            double qRecoveredSensible = 0;

            if (input.DoubleWheelEnabled && !input.IsHeatingMode)
            {
                double driving = current.T - input.OffCoilTemp;
                if (driving > 0)
                {
                    double hIn = current.Enthalpy;
                    double tOut = current.T - (input.DwSens / 100.0) * driving;
                    double wsat = Psychrometrics.GetHumidityRatio(Psychrometrics.GetSatVapPres(tOut));
                    double wOut = current.W > wsat ? wsat : current.W;

                    AirState next = new AirState(tOut, wOut, ptIdx.ToString());
                    res.Steps.Add(new ProcessStep { Component = "Sensible Wheel (Pre)", Entering = current, Leaving = next });
                    res.ChartPoints[next.Name] = next;
                    
                    qRecoveredSensible = mDot * (hIn - next.Enthalpy);
                    current = next; ptIdx++;
                }
            }

            if (input.HpEnabled && !input.IsHeatingMode)
            {
                double driving = current.T - input.OffCoilTemp;
                if (driving > 0)
                {
                    double hIn = current.Enthalpy;
                    double tOut = current.T - (input.HpEff / 100.0) * driving;
                    double wsat = Psychrometrics.GetHumidityRatio(Psychrometrics.GetSatVapPres(tOut));
                    double wOut = current.W > wsat ? wsat : current.W;

                    AirState next = new AirState(tOut, wOut, ptIdx.ToString());
                    res.Steps.Add(new ProcessStep { Component = "HP Pre-Cool", Entering = current, Leaving = next });
                    res.ChartPoints[next.Name] = next;
                    
                    qRecoveredSensible += mDot * (hIn - next.Enthalpy);
                    current = next; ptIdx++;
                }
            }

            // 3. Main Coil
            if (input.IsHeatingMode)
            {
                // Heating Mode
                if (input.OffCoilTemp > current.T)
                {
                    AirState next = new AirState(input.OffCoilTemp, current.W, ptIdx.ToString());
                    res.TotalHeating = mDot * 1.006 * (next.T - current.T);
                    res.Steps.Add(new ProcessStep { Component = "Heating Coil", Entering = current, Leaving = next });
                    res.ChartPoints[next.Name] = next;
                    current = next; ptIdx++;
                }
            }
            else
            {
                // Cooling Mode
                double offSatW = Psychrometrics.GetHumidityRatio(Psychrometrics.GetSatVapPres(input.OffCoilTemp));
                double offW = (current.W > offSatW) ? offSatW : current.W;
                AirState coilOut = new AirState(input.OffCoilTemp, offW, ptIdx.ToString());
                
                res.TotalCooling = mDot * (current.Enthalpy - coilOut.Enthalpy);
                res.SensibleCooling = mDot * 1.006 * (current.T - coilOut.T);
                res.LatentCooling = Math.Max(0, res.TotalCooling - res.SensibleCooling);

                res.Steps.Add(new ProcessStep { Component = "Cooling Coil", Entering = current, Leaving = coilOut });
                res.ChartPoints[coilOut.Name] = coilOut;
                current = coilOut; ptIdx++;
            }

            // 4. Recovery Reheat (Scientist's First Law Correction)
            if (qRecoveredSensible > 0)
            {
                // Re-inject the energy removed during pre-cool
                double dT = qRecoveredSensible / (mDot * 1.006);
                AirState next = new AirState(current.T + dT, current.W, ptIdx.ToString());
                string comp = input.DoubleWheelEnabled ? "Sensible Wheel (Re)" : "HP Reheat";
                res.Steps.Add(new ProcessStep { Component = comp, Entering = current, Leaving = next });
                res.ChartPoints[next.Name] = next;
                current = next; ptIdx++;
            }

            // 5. Supplementary Reheat
            if (input.ReheatEnabled && input.TargetSupplyTemp > current.T)
            {
                res.ReheatLoad = mDot * 1.006 * (input.TargetSupplyTemp - current.T);
                AirState next = new AirState(input.TargetSupplyTemp, current.W, ptIdx.ToString());
                res.Steps.Add(new ProcessStep { Component = "Supplementary Reheat", Entering = current, Leaving = next });
                res.ChartPoints[next.Name] = next;
                current = next;
            }

            // 6. Internal Pressure Drop Estimation (Matched to Daikin FAHU Cuttshhet)
            double damperPd = 50;
            double preFilterG3 = 100;
            double bagFilterF7 = 250; 
            double wheelPd = input.WheelEnabled ? 250 : 0;
            double coilPd = 250; 
            double recoverySensPd = (input.DoubleWheelEnabled || input.HpEnabled) ? 150 : 0;
            double reheatPd = input.ReheatEnabled ? 50 : 0;

            res.SupInternalPd = damperPd + preFilterG3 + bagFilterF7 + wheelPd + coilPd + recoverySensPd + reheatPd;
            res.ExtInternalPd = damperPd + preFilterG3 + wheelPd; 

            // 7. Fan Power Calculation
            double supTsp = input.SupOaEsp + res.SupInternalPd;
            double supDensity = Psychrometrics.GetDensity(current.T);
            double qSup = (input.OaFlow / 1000.0) * (res.AirDensity / supDensity); 
            res.SupFanPowerKW = (qSup * supTsp) / (input.FanEff / 100.0) / 1000.0;

            double extTsp = input.ExtEaEsp + res.ExtInternalPd;
            double extDensity = Psychrometrics.GetDensity(input.EaDb);
            double qExt = (input.EaFlow / 1000.0) * (Psychrometrics.GetDensity(input.EaDb) / extDensity); 
            res.ExtFanPowerKW = (qExt * extTsp) / (input.FanEff / 100.0) / 1000.0;

            // 8. Motor Selection (Standard Sizes: 0.75, 1.1, 1.5, 2.2, 3.0, 4.0, 5.5, 7.5, 11, 15...)
            res.SupMotorKW = SelectMotor(res.SupFanPowerKW / 0.85); // 0.85 factor for motor efficiency/margin
            res.ExtMotorKW = SelectMotor(res.ExtFanPowerKW / 0.85);

            return res;
        }

        private static double SelectMotor(double absorbedKw)
        {
            double[] standardKw = { 0.37, 0.55, 0.75, 1.1, 1.5, 2.2, 3.0, 4.0, 5.5, 7.5, 11.0, 15.0, 18.5, 22.0, 30.0, 37.0, 45.0, 55.0 };
            foreach (var size in standardKw) if (size >= absorbedKw) return size;
            return Math.Ceiling(absorbedKw);
        }
    }
}