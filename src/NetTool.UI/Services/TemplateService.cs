using System.IO;
using System.Text.Json;
using NetTool.UI.Models;

namespace NetTool.UI.Services
{
    public static class TemplateService
    {
        private static readonly string TemplateDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NetTool", "templates");

        /// <summary>
        /// Get the default template directory, creating it if needed.
        /// </summary>
        public static string GetTemplateDirectory()
        {
            if (!Directory.Exists(TemplateDir))
                Directory.CreateDirectory(TemplateDir);
            return TemplateDir;
        }

        /// <summary>
        /// Save a config as a named template.
        /// </summary>
        public static void Save(TestConfig config, string filePath)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Load a config template from file.
        /// </summary>
        public static TestConfig? Load(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<TestConfig>(json);
        }
    }
}
