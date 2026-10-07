using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using BIMQualityAuditor.Models;

namespace BIMQualityAuditor.Services
{
    /// <summary>Obtiene cantidades sin modificar el documento de Revit.</summary>
    public static class QuantityService
    {
        public static List<CategoryQuantity> Collect(Document doc)
        {
            var rows = new Dictionary<string, CategoryQuantity>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in new FilteredElementCollector(doc).WhereElementIsNotElementType())
            {
                Category? category = element.Category;
                if (category == null || category.CategoryType != CategoryType.Model) continue;

                string name = category.Name;
                if (!rows.TryGetValue(name, out var row))
                {
                    row = new CategoryQuantity { CategoryName = name };
                    rows.Add(name, row);
                }

                row.ElementCount++;
                row.LengthMeters += Read(element, BuiltInParameter.INSTANCE_LENGTH_PARAM, UnitTypeId.Meters);
                row.AreaSquareMeters += Read(element, BuiltInParameter.HOST_AREA_COMPUTED, UnitTypeId.SquareMeters);
                row.VolumeCubicMeters += Read(element, BuiltInParameter.HOST_VOLUME_COMPUTED, UnitTypeId.CubicMeters);
            }

            return rows.Values.OrderByDescending(x => x.ElementCount).ToList();
        }

        private static double Read(Element element, BuiltInParameter parameter, ForgeTypeId unit)
        {
            try
            {
                Parameter? value = element.get_Parameter(parameter);
                return value?.StorageType == StorageType.Double
                    ? UnitUtils.ConvertFromInternalUnits(value.AsDouble(), unit)
                    : 0;
            }
            catch { return 0; }
        }
    }
}
