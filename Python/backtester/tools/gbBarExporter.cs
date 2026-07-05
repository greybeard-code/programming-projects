// gbBarExporter — dump every closed bar's Time/OHLC/Volume to CSV.
//
// Purpose: validate the Python backtester's bar construction (ninZaRenko
// 100/4 etc.) against the exact bars NT8 shows on the chart.
//
// Install: copy this file to Documents\NinjaTrader 8\bin\Custom\Indicators\,
// open NinjaScript Editor, compile (F5). Add "gbBarExporter" to the chart
// whose bars you want to export (e.g. MNQ ninZaRenko 100/4). Bars load ->
// file appears immediately; historical bars are written on load.
//
// Output: Documents\NinjaTrader 8\export\bars_<instrument>_<period>.csv
// Times are the CHART timezone (this machine: US/Eastern) — the Python
// comparison tool converts with --tz America/New_York.

#region Using declarations
using System;
using System.Globalization;
using System.IO;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class gbBarExporter : Indicator
    {
        private StreamWriter sw;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Writes every closed bar (Time,O,H,L,C,V) to a CSV for backtester bar-parity validation.";
                Name = "gbBarExporter";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = false;
                IsSuspendedWhileInactive = false;
            }
            else if (State == State.DataLoaded)
            {
                try
                {
                    string dir = Path.Combine(Environment.GetFolderPath(
                        Environment.SpecialFolder.MyDocuments), "NinjaTrader 8", "export");
                    Directory.CreateDirectory(dir);
                    string period = BarsPeriod.ToString();
                    foreach (char c in Path.GetInvalidFileNameChars())
                        period = period.Replace(c, '-');
                    period = period.Replace(' ', '_');
                    string file = Path.Combine(dir, "bars_"
                        + Instrument.MasterInstrument.Name + "_" + period + ".csv");
                    sw = new StreamWriter(file, false) { AutoFlush = true };
                    sw.WriteLine("time,open,high,low,close,volume");
                    Print("gbBarExporter -> " + file);
                }
                catch (Exception ex)
                {
                    Print("gbBarExporter: could not open file: " + ex.Message);
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
                    "{0:yyyy-MM-dd HH:mm:ss.fff},{1},{2},{3},{4},{5}",
                    Time[0], Open[0], High[0], Low[0], Close[0], (long)Volume[0]));
            }
            catch { }
        }
    }
}
