using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using BIMQualityAuditor.Core.Enums;
using BIMQualityAuditor.Models;

namespace BIMQualityAuditor.Services
{
    public class FullAuditResult
    {
        public AuditSummary Summary { get; set; } = new();
        public List<RuleAuditResult> RuleResults { get; set; } = new();
        public List<ElementAuditResult> AllElementResults { get; set; } = new();
    }

    public class AuditSummary
    {
        public int TotalElementsEvaluated { get; set; }
        public int TotalCompliantElements { get; set; }
        public int TotalNonCompliantElements { get; set; }
        public double GlobalCompliancePercentage { get; set; }
        public int TotalViolationsCount { get; set; }
        public int HighSeverityCount { get; set; }
        public int MediumSeverityCount { get; set; }
        public int LowSeverityCount { get; set; }
    }

    public static class AuditService
    {
        public static FullAuditResult RunAudit(Document doc, IEnumerable<RuleDefinition> rules)
        {
            var activeRules = rules.Where(r => r.IsActive).ToList();
            var fullResult = new FullAuditResult();

            if (doc == null || !activeRules.Any()) return fullResult;

            // Dictionary to map element UniqueId -> ElementAuditResult (Element -> Collection of Violations)
            var elementMap = new Dictionary<string, ElementAuditResult>();

            foreach (var rule in activeRules)
            {
                var ruleAudit = new RuleAuditResult { Rule = rule };

                List<Element> elements = ElementCollectorService.GetElementsForCategory(doc, rule.Category);

                foreach (var elem in elements)
                {
                    string uniqueId = elem.UniqueId;
                    long elemId = elem.Id.Value; // Revit 2027 long ElementId

                    if (!elementMap.TryGetValue(uniqueId, out var elemResult))
                    {
                        string familyName = GetFamilyName(doc, elem);
                        string typeName = elem.Name;
                        string categoryName = elem.Category != null ? elem.Category.Name : "Sin categoría";
                        string levelName = ParameterService.GetParameterValueAsString(elem, "Level");

                        elemResult = new ElementAuditResult
                        {
                            ElementId = elemId,
                            UniqueId = uniqueId,
                            CategoryName = categoryName,
                            FamilyName = familyName,
                            TypeName = typeName,
                            LevelName = string.IsNullOrEmpty(levelName) ? "(Sin nivel)" : levelName,
                            ParameterEvaluated = rule.ParameterName
                        };
                        elementMap[uniqueId] = elemResult;
                    }

                    var eval = RuleEngine.EvaluateRule(rule, elem);
                    elemResult.ValueFound = eval.ValueFound;

                    if (!eval.Complies)
                    {
                        var violation = new RuleViolation
                        {
                            RuleId = rule.Id,
                            RuleName = rule.Name,
                            Severity = rule.Severity,
                            HexColor = rule.HexColor,
                            Observation = eval.Observation
                        };

                        elemResult.Violations.Add(violation);
                    }

                    ruleAudit.TestedElements.Add(elemResult);
                }

                fullResult.RuleResults.Add(ruleAudit);
            }

            fullResult.AllElementResults = elementMap.Values.ToList();

            // Calculate Global Summary
            int totalElems = fullResult.AllElementResults.Count;
            int compliantElems = fullResult.AllElementResults.Count(e => e.Complies);
            int nonCompliantElems = totalElems - compliantElems;

            int totalViolations = fullResult.AllElementResults.Sum(e => e.Violations.Count);
            int highCount = fullResult.AllElementResults.Sum(e => e.Violations.Count(v => v.Severity == RuleSeverity.Alta || v.Severity == RuleSeverity.Critica));
            int medCount = fullResult.AllElementResults.Sum(e => e.Violations.Count(v => v.Severity == RuleSeverity.Media));
            int lowCount = fullResult.AllElementResults.Sum(e => e.Violations.Count(v => v.Severity == RuleSeverity.Baja));

            double pct = totalElems == 0 ? 100.0 : Math.Round((double)compliantElems / totalElems * 100.0, 1);

            fullResult.Summary = new AuditSummary
            {
                TotalElementsEvaluated = totalElems,
                TotalCompliantElements = compliantElems,
                TotalNonCompliantElements = nonCompliantElems,
                GlobalCompliancePercentage = pct,
                TotalViolationsCount = totalViolations,
                HighSeverityCount = highCount,
                MediumSeverityCount = medCount,
                LowSeverityCount = lowCount
            };

            return fullResult;
        }

        private static string GetFamilyName(Document doc, Element elem)
        {
            ElementId typeId = elem.GetTypeId();
            if (typeId != ElementId.InvalidElementId)
            {
                ElementType? typeElem = doc.GetElement(typeId) as ElementType;
                if (typeElem != null) return typeElem.FamilyName;
            }
            return elem.Category != null ? elem.Category.Name : string.Empty;
        }
    }
}
