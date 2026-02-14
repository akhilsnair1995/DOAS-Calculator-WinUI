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

            double cp = 1.006; // kJ/kg.K

            // 1. Initial Air States
            double oaPv = Psychrometrics.GetPvFromTwb(input.OaDb, input.OaWb);
            double oaW = Psychrometrics.GetHumidityRatio(oaPv);
            res.AirDensity = Psychrometrics.GetDensity(input.OaDb);
            double mDotOa = (input.OaFlow / 1000.0) * res.AirDensity;

            double raW = Psychrometrics.GetHumidityRatio(Psychrometrics.GetPvFromRh(input.EaDb, input.EaRh));
            double raRho = Psychrometrics.GetDensity(input.EaDb);
            double mDotRa = (input.EaFlow / 1000.0) * raRho;

            AirState currentOa = new AirState(input.OaDb, oaW, "OA");
            AirState currentRa = new AirState(input.EaDb, raW, "RA");
            
            res.ChartPoints["OA"] = currentOa;
            res.ChartPoints["RA"] = currentRa;

            // 2. Sensible Wheel Recovery (Series Source: Return Air)
            double qShrw = 0;
            if (input.DoubleWheelEnabled && mDotRa > 0)
            {
                // Sensible Wheel transfers heat from RA to Coil-Leaving air.
                // This pre-cools the RA before it enters the Enthalpy Wheel.
                double cMin = Math.Min(mDotOa, mDotRa) * cp;
                qShrw = (input.DwSens / 100.0) * cMin * (input.EaDb - input.OffCoilTemp);
                
                if (qShrw > 0)
                {
                    double dtRa = qShrw / (mDotRa * cp);
                    AirState raMid = new AirState(currentRa.T - dtRa, currentRa.W, "RA_Exh_Out");
                    res.Steps.Add(new ProcessStep { Component = "Sensible Wheel (Exh)", Entering = currentRa, Leaving = raMid, Stream = AirStream.Exhaust });
                    currentRa = raMid;
                }
            }

            // 3. Enthalpy Wheel (Primary Recovery)
            if (input.WheelEnabled && mDotRa > 0)
            {
                double cMin = Math.Min(mDotOa, mDotRa);
                AirState entryOa = currentOa;
                AirState entryRa = currentRa;

                // 3a. OA Side Calculation
                double qEhrwSens = (input.WheelSens / 100.0) * (cMin * cp) * (entryOa.T - entryRa.T);
                double tOutOa = entryOa.T - qEhrwSens / (mDotOa * cp);
                
                double dW_Oa = (input.WheelLat / 100.0) * (cMin / mDotOa) * (entryOa.W - entryRa.W);
                double wOutOa = entryOa.W - dW_Oa;

                // Physics Clamp for OA (cannot exceed saturation)
                double wsatOa = Psychrometrics.GetHumidityRatio(Psychrometrics.GetSatVapPres(tOutOa));
                if (wOutOa > wsatOa) wOutOa = wsatOa;

                currentOa = new AirState(tOutOa, wOutOa, "EW_Out_OA");
                res.Steps.Add(new ProcessStep { Component = "Enthalpy Wheel (OA)", Entering = entryOa, Leaving = currentOa, Stream = AirStream.Supply });

                // 3b. EA Side Calculation (Energy/Mass Balance)
                double tOutEa = entryRa.T + qEhrwSens / (mDotRa * cp);
                double wOutEa = entryRa.W + (mDotOa / mDotRa) * (entryOa.W - currentOa.W);
                
                currentRa = new AirState(tOutEa, wOutEa, "EW_Out_EA");
                res.Steps.Add(new ProcessStep { Component = "Enthalpy Wheel (EA)", Entering = entryRa, Leaving = currentRa, Stream = AirStream.Exhaust });
            }

            // 4. Heat Pipe Pre-Cool (Standard Wrap-around)
            double qHp = 0;
            if (input.HpEnabled)
            {
                double driving = currentOa.T - input.OffCoilTemp;
                if (driving > 0)
                {
                    qHp = (input.HpEff / 100.0) * mDotOa * cp * driving;
                    double dt = qHp / (mDotOa * cp);
                    AirState nextOa = new AirState(currentOa.T - dt, currentOa.W, "HP_Pre");
                    res.Steps.Add(new ProcessStep { Component = "HP Pre-Cool", Entering = currentOa, Leaving = nextOa, Stream = AirStream.Supply });
                    currentOa = nextOa;
                }
            }

            // 5. Main Cooling Coil
            double offSatW = Psychrometrics.GetHumidityRatio(Psychrometrics.GetSatVapPres(input.OffCoilTemp));
            double offW = (currentOa.W > offSatW) ? offSatW : currentOa.W;
            AirState coilOut = new AirState(input.OffCoilTemp, offW, "Coil_Out");
            
            res.TotalCooling = mDotOa * (currentOa.Enthalpy - coilOut.Enthalpy);
            res.SensibleCooling = mDotOa * cp * (currentOa.T - coilOut.T);
            res.LatentCooling = Math.Max(0, res.TotalCooling - res.SensibleCooling);

            res.Steps.Add(new ProcessStep { Component = "Cooling Coil", Entering = currentOa, Leaving = coilOut, Stream = AirStream.Supply });
            
            if (input.MainCoilType == CoilType.Water && input.MainCoilDeltaT > 0)
                res.MainCoilWaterFlow = res.TotalCooling / (4.186 * input.MainCoilDeltaT);

            currentOa = coilOut;

            // 6. Recovery Reheat
            if (qHp > 0)
            {
                double dt = qHp / (mDotOa * cp);
                AirState nextOa = new AirState(currentOa.T + dt, currentOa.W, "HP_Reheat");
                res.Steps.Add(new ProcessStep { Component = "HP Reheat", Entering = currentOa, Leaving = nextOa, Stream = AirStream.Supply });
                currentOa = nextOa;
            }

            if (qShrw > 0)
            {
                double dtSa = qShrw / (mDotOa * cp);
                AirState saFinal = new AirState(currentOa.T + dtSa, currentOa.W, "SW_Reheat");
                res.Steps.Add(new ProcessStep { Component = "Sensible Wheel (Re)", Entering = currentOa, Leaving = saFinal, Stream = AirStream.Supply });
                currentOa = saFinal;
            }

            // 7. Supplementary Reheat
            if (input.ReheatEnabled && input.TargetSupplyTemp > currentOa.T)
            {
                res.ReheatLoad = mDotOa * cp * (input.TargetSupplyTemp - currentOa.T);
                AirState next = new AirState(input.TargetSupplyTemp, currentOa.W, "SA_Reheat");
                res.Steps.Add(new ProcessStep { Component = "Supplementary Reheat", Entering = currentOa, Leaving = next, Stream = AirStream.Supply });
                
                if (input.ReheatType == ReheatSource.HotWater)
                {
                    double dT = Math.Abs(input.HwEwt - input.HwLwt);
                    if (dT > 0) res.ReheatWaterFlow = res.ReheatLoad / (4.186 * dT);
                }
                else if (input.ReheatType == ReheatSource.Gas && input.GasEfficiency > 0)
                {
                    res.GasConsumption = (res.ReheatLoad / (input.GasEfficiency / 100.0)) / 10.0; 
                }
                currentOa = next;
            }

            // 8. Internal Pressure Drop Estimation
            double wheelPd = input.WheelEnabled ? 250 : 0;
            double recoverySensPd = (input.DoubleWheelEnabled || input.HpEnabled) ? 150 : 0;
            double reheatPd = input.ReheatEnabled ? 50 : 0;

            res.SupInternalPd = input.PdDamper + input.PdFilterPre + input.PdFilterMain + wheelPd + input.PdCoil + recoverySensPd + reheatPd;
            res.ExtInternalPd = input.PdDamper + input.PdFilterPre + wheelPd; 

            // 9. Fan Power Calculation
            double supTsp = input.SupOaEsp + res.SupInternalPd;
            double qSupLocal = mDotOa / Psychrometrics.GetDensity(currentOa.T); 
            res.SupFanPowerKW = (qSupLocal * supTsp) / (input.FanEff / 100.0) / 1000.0;
            res.SupElectricalPowerKW = res.SupFanPowerKW / ((input.MotorEff / 100.0) * (input.DriveEff / 100.0));

            // 10. Final Supply State (Excluding Fan Heat to match manufacturer data)
            res.ChartPoints["SA"] = currentOa;

            double extTsp = input.ExtEaEsp + res.ExtInternalPd;
            double qExtLocal = (input.EaFlow / 1000.0) * (Psychrometrics.GetDensity(input.EaDb) / Psychrometrics.GetDensity(input.EaDb)); 
            res.ExtFanPowerKW = (qExtLocal * extTsp) / (input.FanEff / 100.0) / 1000.0;
            res.ExtElectricalPowerKW = res.ExtFanPowerKW / ((input.MotorEff / 100.0) * (input.DriveEff / 100.0));

            // 11. Motor Selection
            res.SupMotorKW = SelectMotor(res.SupElectricalPowerKW * 1.15); 
            res.ExtMotorKW = SelectMotor(res.ExtElectricalPowerKW * 1.15);

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
