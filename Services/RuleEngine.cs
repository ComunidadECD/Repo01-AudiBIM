using System;
using System.Globalization;
using Autodesk.Revit.DB;
using BIMQualityAuditor.Core.Enums;
using BIMQualityAuditor.Models;

namespace BIMQualityAuditor.Services
{
    public class EvaluationResult
    {
        public bool Complies { get; set; }
        public string ValueFound { get; set; } = string.Empty;
        public string Observation { get; set; } = string.Empty;
    }

    public static class RuleEngine
    {
        public static bool EvaluateRule(string valFound, RuleOperator op, string expectedVal)
        {
            var rule = new RuleDefinition { Operator = op, Value = expectedVal };
            var res = EvaluateValue(rule, valFound);
            return res.Complies;
        }

        public static EvaluationResult EvaluateRule(RuleDefinition rule, Element element)
        {
            string valFound = ParameterService.GetParameterValueAsString(element, rule.ParameterName);
            return EvaluateValue(rule, valFound);
        }

        private static EvaluationResult EvaluateValue(RuleDefinition rule, string valFound)
        {
            bool complies = false;
            string obs = string.Empty;

            string expectedVal = rule.Value ?? string.Empty;

            switch (rule.Operator)
            {
                case RuleOperator.NOT_EMPTY:
                    complies = !string.IsNullOrWhiteSpace(valFound);
                    obs = complies ? "Parámetro asignado correctamente" : $"El parámetro '{rule.ParameterName}' está vacío o no definido";
                    break;

                case RuleOperator.EMPTY:
                    complies = string.IsNullOrWhiteSpace(valFound);
                    obs = complies ? "Parámetro está vacío como se esperaba" : $"El parámetro '{rule.ParameterName}' contiene valor '{valFound}' pero debía estar vacío";
                    break;

                case RuleOperator.EQUALS:
                    complies = valFound.Equals(expectedVal, StringComparison.OrdinalIgnoreCase);
                    obs = complies ? $"Coincide exactamente con '{expectedVal}'" : $"Valor '{valFound}' no coincide con '{expectedVal}'";
                    break;

                case RuleOperator.NOT_EQUALS:
                    complies = !valFound.Equals(expectedVal, StringComparison.OrdinalIgnoreCase);
                    obs = complies ? $"Valor es diferente de '{expectedVal}'" : $"Valor '{valFound}' es igual a '{expectedVal}'";
                    break;

                case RuleOperator.CONTAINS:
                    complies = valFound.Contains(expectedVal, StringComparison.OrdinalIgnoreCase);
                    obs = complies ? $"Contiene el texto '{expectedVal}'" : $"Valor '{valFound}' no contiene '{expectedVal}'";
                    break;

                case RuleOperator.NOT_CONTAINS:
                    complies = !valFound.Contains(expectedVal, StringComparison.OrdinalIgnoreCase);
                    obs = complies ? $"No contiene el texto '{expectedVal}'" : $"Valor '{valFound}' contiene el texto no permitido '{expectedVal}'";
                    break;

                case RuleOperator.GREATER_THAN:
                    if (double.TryParse(valFound, NumberStyles.Any, CultureInfo.InvariantCulture, out double dVal) &&
                        double.TryParse(expectedVal, NumberStyles.Any, CultureInfo.InvariantCulture, out double dExp))
                    {
                        complies = dVal > dExp;
                        obs = complies ? $"{dVal} es mayor que {dExp}" : $"{dVal} no es mayor que {dExp}";
                    }
                    else
                    {
                        complies = false;
                        obs = $"No se pudo realizar la comparación numérica ({valFound} vs {expectedVal})";
                    }
                    break;

                case RuleOperator.LESS_THAN:
                    if (double.TryParse(valFound, NumberStyles.Any, CultureInfo.InvariantCulture, out double dValL) &&
                        double.TryParse(expectedVal, NumberStyles.Any, CultureInfo.InvariantCulture, out double dExpL))
                    {
                        complies = dValL < dExpL;
                        obs = complies ? $"{dValL} es menor que {dExpL}" : $"{dValL} no es menor que {dExpL}";
                    }
                    else
                    {
                        complies = false;
                        obs = $"No se pudo realizar la comparación numérica ({valFound} vs {expectedVal})";
                    }
                    break;

                default:
                    complies = false;
                    obs = $"Operador '{rule.Operator}' no implementado";
                    break;
            }

            return new EvaluationResult
            {
                Complies = complies,
                ValueFound = string.IsNullOrEmpty(valFound) ? "(Vacío)" : valFound,
                Observation = obs
            };
        }
    }
}
