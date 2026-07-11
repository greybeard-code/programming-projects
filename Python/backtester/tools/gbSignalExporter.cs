// gbSignalExporter — dump every closed bar's OHLCV plus the six GreyBeard
// Signal_Trade codes (KO/PA/TH/SJ/SU/NC) to one CSV, for validating the
// Python ports in backtester/gbsignals/ (tools/compare_signals.py).
//
// Install: copy to Documents\NinjaTrader 8\bin\Custom\Indicators\, compile
// (F5), add "gbSignalExporter" to the chart to validate (e.g. MNQ ninZaRenko
// 60/3 — the OneSet_3ofAll_BestTime series). All six engines run with the
// GodZillaKilla v1.10 DEFAULT parameters (matching the Python ports'
// defaults); historical bars are written on load.
//
// Output: Documents\NinjaTrader 8\export\signals_<instrument>_<period>.csv
// Times are the CHART timezone (this machine: US/Eastern).

#region Using declarations
using System;
using System.Globalization;
using System.IO;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators.GreyBeard;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class gbSignalExporter : Indicator
    {
        private StreamWriter sw;
        private gbKingOrderBlock ko;
        private gbPANAKanal pa;
        private gbThunderZilla th;
        private gbSuperJumpBoost sj;
        private gbSumoPullback su;
        private gbNobleCloud nc;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Writes Time,OHLCV + the six gb Signal_Trade codes per closed bar (backtester signal-parity validation).";
                Name = "gbSignalExporter";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = false;
                IsSuspendedWhileInactive = false;
            }
            else if (State == State.DataLoaded)
            {
                // GodZillaKilla v1.10 defaults (GZK.cs SetDefaults 762-843)
                ko = gbKingOrderBlock(Close, 5, 3, 50, 500, 10, 10, 3, 6);
                pa = gbPANAKanal(Close, 20, 4.0, 14, 20, 10);
                th = gbThunderZilla(Close, gbThunderZilla_MAType.SMA, 200,
                    false, gbThunderZilla_MAType.EMA, 10, 60.0, 2, 999);
                sj = gbSuperJumpBoost(Close, true, 1.0, 2.0, 3.0, 4.0, 4.0,
                    2, 100, 30, 70, 2, 20);
                su = gbSumoPullback(Close,
                    gbSumoPullback_MAType.SMA, 60, false, gbSumoPullback_MAType.EMA, 10,
                    gbSumoPullback_MAType.EMA, 14, false, gbSumoPullback_MAType.SMA, 5,
                    gbSumoPullback_MAType.EMA, 30, false, gbSumoPullback_MAType.SMA, 10,
                    gbSumoPullback_MAType.EMA, 45, false, gbSumoPullback_MAType.SMA, 15,
                    15, 30);
                nc = gbNobleCloud(Close, 60.0, 1,
                    gbNobleCloud_MAType.SMA, 60, true, gbNobleCloud_MAType.EMA, 60,
                    gbNobleCloud_MAType.SMA, 20, true, gbNobleCloud_MAType.EMA, 5,
                    5, true, 10, 300);
                try
                {
                    string dir = Path.Combine(Environment.GetFolderPath(
                        Environment.SpecialFolder.MyDocuments), "NinjaTrader 8", "export");
                    Directory.CreateDirectory(dir);
                    string period = BarsPeriod.ToString();
                    foreach (char c in Path.GetInvalidFileNameChars())
                        period = period.Replace(c, '-');
                    period = period.Replace(' ', '_');
                    string file = Path.Combine(dir, "signals_"
                        + Instrument.MasterInstrument.Name + "_" + period + ".csv");
                    sw = new StreamWriter(file, false) { AutoFlush = true };
                    sw.WriteLine("time,open,high,low,close,volume,ko,pa,th,sj,su,nc");
                    Print("gbSignalExporter -> " + file);
                }
                catch (Exception ex)
                {
                    Print("gbSignalExporter: could not open file: " + ex.Message);
                }
            }
            else if (State == State.Terminated)
            {
                try { if (sw != null) { sw.Dispose(); sw = null; } } catch { }
            }
        }

        protected override void OnBarUpdate()
        {
            if (sw == null) return;
            try
            {
                sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "{0:yyyy-MM-dd HH:mm:ss.fff},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}",
                    Time[0], Open[0], High[0], Low[0], Close[0], (long)Volume[0],
                    (int)Math.Round(ko.Signal_Trade[0]),
                    (int)Math.Round(pa.Signal_Trade[0]),
                    (int)Math.Round(th.Signal_Trade[0]),
                    (int)Math.Round(sj.Signal_Trade[0]),
                    (int)Math.Round(su.Signal_Trade[0]),
                    (int)Math.Round(nc.Signal_Trade[0])));
            }
            catch { }
        }
    }
}
