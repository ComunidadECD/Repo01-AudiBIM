using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BIMQualityAuditor.Models;

namespace BIMQualityAuditor.Services
{
    public static class RulePersistenceService
    {
        private static string GetSessionFilePath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folder = Path.Combine(appData, "AudiBIM");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            return Path.Combine(folder, "session_rules.json");
        }

        public static void SaveSessionRules(IEnumerable<RuleDefinition> rules)
        {
            try
            {
                if (rules == null) return;
                string filePath = GetSessionFilePath();
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(rules.ToList(), options);
                File.WriteAllText(filePath, json);
            }
            catch
            {
                // Ignore storage write error safely
            }
        }

        public static List<RuleDefinition>? LoadSessionRules()
        {
            try
            {
                string filePath = GetSessionFilePath();
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var rules = JsonSerializer.Deserialize<List<RuleDefinition>>(json);
                        if (rules != null && rules.Count > 0)
                        {
                            return rules;
                        }
                    }
                }
            }
            catch
            {
                // Fallback to default
            }
            return null;
        }
    }
}
