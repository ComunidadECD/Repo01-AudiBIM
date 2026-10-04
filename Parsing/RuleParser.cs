using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using BIMQualityAuditor.Core.Enums;
using BIMQualityAuditor.Models;

namespace BIMQualityAuditor.Parsing
{
    public class RuleParseResult
    {
        public List<RuleDefinition> Rules { get; set; } = new();
        public List<RuleParsingError> Errors { get; set; } = new();
    }

    public static class RuleParser
    {
        public static RuleParseResult ParseMarkdownFiles(IEnumerable<string> filePaths)
        {
            var result = new RuleParseResult();

            foreach (var filePath in filePaths)
            {
                if (!File.Exists(filePath)) continue;

                string fileName = Path.GetFileName(filePath);
                string content = File.ReadAllText(filePath);

                ParseSingleMarkdownContent(content, fileName, result);
            }

            return result;
        }

        public static void ParseSingleMarkdownContent(string content, string fileName, RuleParseResult result)
        {
            // Split content into rule blocks starting with '## REGLA' or '#'
            string[] blocks = Regex.Split(content, @"(?m)^[ \t]*##?[ \t]+REGLA", RegexOptions.IgnoreCase);

            foreach (var block in blocks)
            {
                if (string.IsNullOrWhiteSpace(block)) continue;

                var rawDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                string[] lines = block.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    int colonIndex = line.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        string key = line.Substring(0, colonIndex).Trim().ToUpperInvariant();
                        string value = line.Substring(colonIndex + 1).Trim();
                        rawDict[key] = value;
                    }
                }

                if (rawDict.Count == 0) continue;

                string id = GetValueOrDefault(rawDict, "ID");
                string category = GetValueOrDefault(rawDict, "CATEGORIA", "CATEGORY");
                string parameter = GetValueOrDefault(rawDict, "PARAMETRO", "PARAMETER");
                string operatorStr = GetValueOrDefault(rawDict, "OPERADOR", "OPERATOR");

                // Validation
                if (string.IsNullOrWhiteSpace(id))
                {
                    result.Errors.Add(new RuleParsingError
                    {
                        FileName = fileName,
                        RuleId = "Desconocido",
                        MissingOrInvalidField = "ID",
                        ErrorMessage = $"Regla en {fileName} no pudo cargarse. El campo ID no fue encontrado."
                    });
                    continue;
                }

                if (string.IsNullOrWhiteSpace(category))
                {
                    result.Errors.Add(new RuleParsingError
                    {
                        FileName = fileName,
                        RuleId = id,
                        MissingOrInvalidField = "CATEGORIA",
                        ErrorMessage = $"Regla '{id}' no pudo cargarse. El campo CATEGORIA no fue encontrado."
                    });
                    continue;
                }

                if (string.IsNullOrWhiteSpace(parameter))
                {
                    result.Errors.Add(new RuleParsingError
                    {
                        FileName = fileName,
                        RuleId = id,
                        MissingOrInvalidField = "PARAMETRO",
                        ErrorMessage = $"Regla '{id}' no pudo cargarse. El campo PARAMETRO no fue encontrado."
                    });
                    continue;
                }

                if (string.IsNullOrWhiteSpace(operatorStr) || !Enum.TryParse<RuleOperator>(operatorStr, true, out var opEnum))
                {
                    result.Errors.Add(new RuleParsingError
                    {
                        FileName = fileName,
                        RuleId = id,
                        MissingOrInvalidField = "OPERADOR",
                        ErrorMessage = $"Regla '{id}' no pudo cargarse. El campo OPERADOR '{operatorStr}' es inválido o no fue encontrado."
                    });
                    continue;
                }

                // Severity parsing
                string severityStr = GetValueOrDefault(rawDict, "SEVERIDAD", "SEVERITY");
                RuleSeverity severity = RuleSeverity.Media;
                if (!string.IsNullOrWhiteSpace(severityStr))
                {
                    Enum.TryParse(severityStr, true, out severity);
                }

                // Color parsing
                string colorHex = GetValueOrDefault(rawDict, "COLOR", "HEXCOLOR");
                if (string.IsNullOrWhiteSpace(colorHex) || !colorHex.StartsWith("#"))
                {
                    colorHex = "#E53935"; // Default red
                }

                // Active flag parsing
                string activeStr = GetValueOrDefault(rawDict, "ACTIVA", "ACTIVE");
                bool isActive = true;
                if (!string.IsNullOrWhiteSpace(activeStr) && bool.TryParse(activeStr, out var parsedActive))
                {
                    isActive = parsedActive;
                }

                var rule = new RuleDefinition
                {
                    Id = id,
                    Name = GetValueOrDefault(rawDict, "NOMBRE", "NAME", id),
                    Description = GetValueOrDefault(rawDict, "DESCRIPCION", "DESCRIPTION"),
                    Category = category,
                    ParameterName = parameter,
                    Operator = opEnum,
                    Value = GetValueOrDefault(rawDict, "VALOR", "VALUE"),
                    Severity = severity,
                    HexColor = colorHex,
                    IsActive = isActive,
                    SourceFile = fileName
                };

                result.Rules.Add(rule);
            }
        }

        private static string GetValueOrDefault(Dictionary<string, string> dict, string key, string fallbackKey = "", string defaultValue = "")
        {
            if (dict.TryGetValue(key, out var val)) return val;
            if (!string.IsNullOrEmpty(fallbackKey) && dict.TryGetValue(fallbackKey, out var fallbackVal)) return fallbackVal;
            return defaultValue;
        }
    }
}
