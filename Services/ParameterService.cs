using System;
using Autodesk.Revit.DB;

namespace BIMQualityAuditor.Services
{
    public static class ParameterService
    {
        public static string GetLevelName(Element element)
        {
            if (element == null) return string.Empty;
            return GetParameterValueAsString(element, "Level");
        }

        public static string GetParameterValueAsString(Element element, string parameterName)
        {
            if (element == null || string.IsNullOrWhiteSpace(parameterName))
                return string.Empty;

            Document doc = element.Document;

            // 1. Check Type Name / Level special parameter shortcuts
            if (parameterName.Equals("Type Name", StringComparison.OrdinalIgnoreCase) ||
                parameterName.Equals("Nombre de tipo", StringComparison.OrdinalIgnoreCase) ||
                parameterName.Equals("Tipo", StringComparison.OrdinalIgnoreCase))
            {
                ElementId typeId = element.GetTypeId();
                if (typeId != ElementId.InvalidElementId)
                {
                    ElementType? elementType = doc.GetElement(typeId) as ElementType;
                    if (elementType != null) return elementType.Name;
                }
            }

            if (parameterName.Equals("Level", StringComparison.OrdinalIgnoreCase) ||
                parameterName.Equals("Nivel", StringComparison.OrdinalIgnoreCase))
            {
                if (element.LevelId != ElementId.InvalidElementId)
                {
                    Level? lvl = doc.GetElement(element.LevelId) as Level;
                    if (lvl != null) return lvl.Name;
                }

                // Fallback to Instance parameter Lookup
                Parameter lvlParam = element.get_Parameter(BuiltInParameter.INSTANCE_SCHEDULE_ONLY_LEVEL_PARAM)
                                  ?? element.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM);
                if (lvlParam != null && lvlParam.HasValue)
                {
                    return lvlParam.AsValueString() ?? lvlParam.AsString() ?? string.Empty;
                }
            }

            // 2. Lookup Instance Parameter by name
            Parameter param = element.LookupParameter(parameterName);

            // 3. Fallback to Type Parameter by name
            if (param == null || !param.HasValue)
            {
                ElementId typeId = element.GetTypeId();
                if (typeId != ElementId.InvalidElementId)
                {
                    ElementType? typeElement = doc.GetElement(typeId) as ElementType;
                    if (typeElement != null)
                    {
                        param = typeElement.LookupParameter(parameterName);
                    }
                }
            }

            if (param == null || !param.HasValue)
                return string.Empty;

            return GetRawParameterStringValue(doc, param);
        }

        private static string GetRawParameterStringValue(Document doc, Parameter param)
        {
            switch (param.StorageType)
            {
                case StorageType.String:
                    return param.AsString() ?? string.Empty;

                case StorageType.Integer:
                    string valString = param.AsValueString();
                    if (!string.IsNullOrEmpty(valString)) return valString;
                    return param.AsInteger().ToString();

                case StorageType.Double:
                    string dblValString = param.AsValueString();
                    if (!string.IsNullOrEmpty(dblValString)) return dblValString;
                    return param.AsDouble().ToString("0.##");

                case StorageType.ElementId:
                    ElementId id = param.AsElementId();
                    if (id != ElementId.InvalidElementId)
                    {
                        Element? refElem = doc.GetElement(id);
                        if (refElem != null) return refElem.Name;
                    }
                    return string.Empty;

                default:
                    return param.AsValueString() ?? string.Empty;
            }
        }
    }
}
