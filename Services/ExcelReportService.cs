using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;
using BIMQualityAuditor.Models;

namespace BIMQualityAuditor.Services
{
    /// <summary>Genera un libro .xlsx autocontenido, sin requerir Microsoft Excel instalado.</summary>
    public static class ExcelReportService
    {
        public static void Export(string filePath, string projectName, AuditSummary summary,
            IEnumerable<ReportItem> reportItems, IEnumerable<CategoryQuantity> quantities)
        {
            using var archive = ZipFile.Open(filePath, ZipArchiveMode.Create);
            Write(archive, "[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet2.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet3.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>");
            Write(archive, "_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
            Write(archive, "xl/workbook.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Resumen\" sheetId=\"1\" r:id=\"rId1\"/><sheet name=\"Listado de reporte\" sheetId=\"2\" r:id=\"rId2\"/><sheet name=\"Cantidades\" sheetId=\"3\" r:id=\"rId3\"/></sheets></workbook>");
            Write(archive, "xl/_rels/workbook.xml.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet2.xml\"/><Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet3.xml\"/><Relationship Id=\"rId4\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>");
            Write(archive, "xl/styles.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Aptos\"/></font><font><b/><color rgb=\"FFFFFFFF\"/><sz val=\"11\"/><name val=\"Aptos\"/></font></fonts><fills count=\"3\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FF1D4ED8\"/><bgColor indexed=\"64\"/></patternFill></fill></fills><borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders><cellXfs count=\"2\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/><xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"0\" applyFont=\"1\" applyFill=\"1\"/></cellXfs></styleSheet>");

            Write(archive, "xl/worksheets/sheet1.xml", Sheet(new[]
            {
                new[] { "AudiBIM - Resumen de auditoría" }, new[] { "Proyecto", projectName },
                new[] { "Fecha de exportación", DateTime.Now.ToString("yyyy-MM-dd HH:mm") },
                new[] { "Elementos revisados", summary.TotalElementsEvaluated.ToString(CultureInfo.InvariantCulture) }, new[] { "Cumplen", summary.TotalCompliantElements.ToString(CultureInfo.InvariantCulture) },
                new[] { "Incumplimientos", summary.TotalNonCompliantElements.ToString(CultureInfo.InvariantCulture) }, new[] { "Cumplimiento global (%)", summary.GlobalCompliancePercentage.ToString(CultureInfo.InvariantCulture) }
            }, false));
            Write(archive, "xl/worksheets/sheet2.xml", Sheet(new[] { new[] { "Estado", "ElementId", "Categoría", "Familia", "Tipo", "Nivel", "Observaciones" } }
                .Concat(reportItems.Select(x => new[] { x.Status, x.ElementId.ToString(CultureInfo.InvariantCulture), x.CategoryName, x.FamilyName, x.TypeName, x.LevelName, x.Observations })), true));
            Write(archive, "xl/worksheets/sheet3.xml", Sheet(new[] { new[] { "Categoría", "Elementos", "Longitud (m)", "Área (m²)", "Volumen (m³)" } }
                .Concat(quantities.Select(x => new[] { x.CategoryName, x.ElementCount.ToString(CultureInfo.InvariantCulture), x.LengthMeters.ToString("0.00", CultureInfo.InvariantCulture), x.AreaSquareMeters.ToString("0.00", CultureInfo.InvariantCulture), x.VolumeCubicMeters.ToString("0.00", CultureInfo.InvariantCulture) })), true));
        }

        private static void Write(ZipArchive archive, string path, string content)
        {
            var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
            using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
            writer.Write(content);
        }

        private static string Sheet(IEnumerable<string[]> rows, bool headers)
        {
            var sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetViews><sheetView workbookViewId=\"0\"/></sheetViews><sheetFormatPr defaultRowHeight=\"15\"/><cols><col min=\"1\" max=\"1\" width=\"18\" customWidth=\"1\"/><col min=\"2\" max=\"7\" width=\"24\" customWidth=\"1\"/></cols><sheetData>");
            int rowNumber = 1;
            foreach (var row in rows)
            {
                sb.Append($"<row r=\"{rowNumber}\">");
                for (int i = 0; i < row.Length; i++)
                    sb.Append($"<c r=\"{Column(i + 1)}{rowNumber}\" t=\"inlineStr\"{(headers && rowNumber == 1 ? " s=\"1\"" : string.Empty)}><is><t>{Escape(row[i])}</t></is></c>");
                sb.Append("</row>"); rowNumber++;
            }
            return sb.Append("</sheetData></worksheet>").ToString();
        }

        private static string Column(int number) { var text = string.Empty; while (number > 0) { number--; text = (char)('A' + number % 26) + text; number /= 26; } return text; }
        private static string Escape(string value) => SecurityElement.Escape(value) ?? string.Empty;
    }
}
