using System.IO;
using System.Text;
using System.Text.Json;
using NetTool.UI.Models;

namespace NetTool.UI.Services
{
    public static class ExportService
    {
        /// <summary>
        /// Export metrics history to CSV file.
        /// </summary>
        public static void ExportCsv(List<MetricsData> history, string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Time(s),RPS,Avg(ms),Min(ms),Max(ms),StdDev,P50(ms),P95(ms),P99(ms),ErrorRate,Total,Success,Fail,2xx,3xx,4xx,5xx,Bytes,ActiveConns");

            foreach (var m in history)
            {
                var codes = m.StatusCodes;
                sb.AppendLine(string.Join(",",
                    m.Time,
                    m.Rps,
                    m.Avg.ToString("F2"),
                    m.Min.ToString("F2"),
                    m.Max.ToString("F2"),
                    m.StdDev.ToString("F2"),
                    m.P50.ToString("F2"),
                    m.P95.ToString("F2"),
                    m.P99.ToString("F2"),
                    m.ErrorRate.ToString("F4"),
                    m.Total,
                    m.Success,
                    m.Fail,
                    codes?.S2xx ?? 0,
                    codes?.S3xx ?? 0,
                    codes?.S4xx ?? 0,
                    codes?.S5xx ?? 0,
                    m.BytesReceived,
                    m.ActiveConnections
                ));
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// Export metrics history to JSON file.
        /// </summary>
        public static void ExportJson(List<MetricsData> history, string filePath)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(history, options);
            File.WriteAllText(filePath, json, Encoding.UTF8);
        }
    }
}
