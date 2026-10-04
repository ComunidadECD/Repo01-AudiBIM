using System;
using System.Collections.Generic;
using System.Linq;
using BIMQualityAuditor.Core.Enums;

namespace BIMQualityAuditor.Models
{
    public class RuleDefinition
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string ParameterName { get; set; } = string.Empty;
        public RuleOperator Operator { get; set; } = RuleOperator.NOT_EMPTY;
        public string Value { get; set; } = string.Empty;
        public RuleSeverity Severity { get; set; } = RuleSeverity.Media;
        public string HexColor { get; set; } = "#E53935";
        public bool IsActive { get; set; } = true;
        public string SourceFile { get; set; } = string.Empty;
    }

    public class ColorOption
    {
        public string Name { get; set; } = string.Empty;
        public string HexColor { get; set; } = "#E53935";

        public static List<ColorOption> AvailableColors { get; } = new List<ColorOption>
        {
            new ColorOption { Name = "Rojo (#E53935)", HexColor = "#E53935" },
            new ColorOption { Name = "Naranja (#FB8C00)", HexColor = "#FB8C00" },
            new ColorOption { Name = "Amarillo (#FDD835)", HexColor = "#FDD835" },
            new ColorOption { Name = "Verde (#4CAF50)", HexColor = "#4CAF50" },
            new ColorOption { Name = "Azul (#2563EB)", HexColor = "#2563EB" },
            new ColorOption { Name = "Púrpura (#8E24AA)", HexColor = "#8E24AA" },
            new ColorOption { Name = "Rosa (#E91E63)", HexColor = "#E91E63" },
            new ColorOption { Name = "Cyan (#00BCD4)", HexColor = "#00BCD4" },
            new ColorOption { Name = "Gris (#78909C)", HexColor = "#78909C" },
            new ColorOption { Name = "Oscuro (#1E293B)", HexColor = "#1E293B" }
        };

        public override string ToString() => Name;
    }

    public class RuleParsingError
    {
        public string FileName { get; set; } = string.Empty;
        public string RuleId { get; set; } = string.Empty;
        public string MissingOrInvalidField { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class RuleViolation
    {
        public string RuleId { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public RuleSeverity Severity { get; set; }
        public string HexColor { get; set; } = "#E53935";
        public string Observation { get; set; } = string.Empty;
    }

    public class ElementAuditResult
    {
        public long ElementId { get; set; }
        public string UniqueId { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string FamilyName { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public string LevelName { get; set; } = string.Empty;
        public string ParameterEvaluated { get; set; } = string.Empty;
        public string ValueFound { get; set; } = string.Empty;

        public bool Complies => Violations.Count == 0;
        public List<RuleViolation> Violations { get; set; } = new();
    }

    public class RuleAuditResult
    {
        public RuleDefinition Rule { get; set; } = new();
        public List<ElementAuditResult> TestedElements { get; set; } = new();

        public int TotalEvaluated => TestedElements.Count;
        public int CompliantCount => TestedElements.Count(e => e.Complies || !e.Violations.Any(v => v.RuleId == Rule.Id));
        public int NonCompliantCount => TestedElements.Count(e => e.Violations.Any(v => v.RuleId == Rule.Id));
        public double CompliancePercentage => TotalEvaluated == 0 ? 100.0 : Math.Round((double)CompliantCount / TotalEvaluated * 100.0, 1);
    }

    public class LiveInspectedElement
    {
        public long ElementId { get; set; }
        public string ElementName { get; set; } = "Sin selección";
        public string CategoryName { get; set; } = string.Empty;
        public string LevelName { get; set; } = string.Empty;
        public bool HasSelection { get; set; } = false;
        public bool OverallComplies { get; set; } = true;
        public string StatusSummary { get; set; } = "Seleccione un elemento en Revit para inspeccionar sus reglas en vivo.";
        public List<RuleEvaluationDetail> Details { get; set; } = new();
    }

    public class RuleEvaluationDetail
    {
        public string RuleId { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public bool Complies { get; set; }
        public string StatusIcon => Complies ? "✅ Cumple" : "❌ Incumple";
        public string HexColor { get; set; } = "#E53935";
        public string Observation { get; set; } = string.Empty;
    }
}
