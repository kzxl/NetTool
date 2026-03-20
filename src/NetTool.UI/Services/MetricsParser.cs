using System.Text.Json;
using NetTool.UI.Models;

namespace NetTool.UI.Services
{
    public static class MetricsParser
    {
        /// <summary>
        /// Parse a JSON line from engine stdout into MetricsData.
        /// Returns null if the line is not valid JSON metrics.
        /// </summary>
        public static MetricsData? Parse(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            try
            {
                return JsonSerializer.Deserialize<MetricsData>(line);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
