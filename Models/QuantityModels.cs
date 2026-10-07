namespace BIMQualityAuditor.Models
{
    public class ReportItem
    {
        public long ElementId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string FamilyName { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public string LevelName { get; set; } = string.Empty;
        public bool Complies { get; set; }
        public string Status => Complies ? "Cumple" : "Incumple";
        public string Observations { get; set; } = string.Empty;
    }

    /// <summary>Resumen de cantidades físicas de una categoría del modelo.</summary>
    public class CategoryQuantity
    {
        public string CategoryName { get; set; } = "Sin categoría";
        public int ElementCount { get; set; }
        public double LengthMeters { get; set; }
        public double AreaSquareMeters { get; set; }
        public double VolumeCubicMeters { get; set; }

        public string LengthDisplay => LengthMeters > 0 ? $"{LengthMeters:N2} m" : "—";
        public string AreaDisplay => AreaSquareMeters > 0 ? $"{AreaSquareMeters:N2} m²" : "—";
        public string VolumeDisplay => VolumeCubicMeters > 0 ? $"{VolumeCubicMeters:N2} m³" : "—";
    }
}
