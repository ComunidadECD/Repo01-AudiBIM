using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace BIMQualityAuditor.Services
{
    public static class PdfReportService
    {
        public static string GenerateAuditReportHtml(
            string projectName,
            int totalRules,
            int totalElements,
            int totalNonCompliant,
            double globalCompliance,
            System.Collections.Generic.IEnumerable<Models.RuleAuditResult> ruleResults)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang='es'>");
            sb.AppendLine("<head>");
            sb.AppendLine("  <meta charset='UTF-8'>");
            sb.AppendLine("  <title>Informe de Auditoría BIM - AudiBIM</title>");
            sb.AppendLine("  <style>");
            sb.AppendLine("    body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f8fafc; color: #0f172a; margin: 0; padding: 24px; }");
            sb.AppendLine("    .container { max-width: 1000px; margin: 0 auto; background: #ffffff; padding: 32px; border-radius: 12px; box-shadow: 0 4px 12px rgba(0,0,0,0.08); }");
            sb.AppendLine("    .header { display: flex; justify-content: space-between; align-items: center; border-bottom: 2px solid #e2e8f0; padding-bottom: 16px; margin-bottom: 24px; }");
            sb.AppendLine("    .title { font-size: 26px; font-weight: bold; color: #2563eb; margin: 0; }");
            sb.AppendLine("    .subtitle { font-size: 14px; color: #64748b; margin-top: 4px; }");
            sb.AppendLine("    .metrics-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 16px; margin-bottom: 32px; }");
            sb.AppendLine("    .metric-card { background: #f1f5f9; padding: 16px; border-radius: 8px; border: 1px solid #cbd5e1; text-align: center; }");
            sb.AppendLine("    .metric-val { font-size: 24px; font-weight: bold; color: #0f172a; }");
            sb.AppendLine("    .metric-val.error { color: #dc2626; }");
            sb.AppendLine("    .metric-val.success { color: #16a34a; }");
            sb.AppendLine("    .metric-val.accent { color: #2563eb; }");
            sb.AppendLine("    .metric-lbl { font-size: 12px; color: #475569; font-weight: 600; text-transform: uppercase; margin-top: 4px; }");
            sb.AppendLine("    .rule-section { margin-bottom: 32px; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden; }");
            sb.AppendLine("    .rule-header { background: #f8fafc; padding: 16px; border-bottom: 1px solid #e2e8f0; display: flex; justify-content: space-between; align-items: center; }");
            sb.AppendLine("    .rule-title { font-size: 16px; font-weight: bold; }");
            sb.AppendLine("    .rule-badge { padding: 4px 10px; border-radius: 4px; color: #ffffff; font-weight: bold; font-size: 12px; display: inline-block; }");
            sb.AppendLine("    table { width: 100%; border-collapse: collapse; font-size: 13px; }");
            sb.AppendLine("    th { background: #f1f5f9; text-align: left; padding: 10px 12px; border-bottom: 2px solid #cbd5e1; font-weight: bold; color: #334155; }");
            sb.AppendLine("    td { padding: 10px 12px; border-bottom: 1px solid #e2e8f0; color: #1e293b; }");
            sb.AppendLine("    tr:nth-child(even) { background-color: #f8fafc; }");
            sb.AppendLine("    .status-error { color: #dc2626; font-weight: bold; }");
            sb.AppendLine("    .footer { text-align: center; margin-top: 32px; font-size: 12px; color: #94a3b8; border-top: 1px solid #e2e8f0; padding-top: 16px; }");
            sb.AppendLine("  </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("  <div class='container'>");
            sb.AppendLine("    <div class='header'>");
            sb.AppendLine("      <div>");
            sb.AppendLine("        <h1 class='title'>AudiBIM - Informe de Auditoría de Calidad</h1>");
            sb.AppendLine($"        <div class='subtitle'>Proyecto: <strong>{projectName}</strong> | Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}</div>");
            sb.AppendLine("      </div>");
            sb.AppendLine("    </div>");

            // Summary Metrics
            sb.AppendLine("    <div class='metrics-grid'>");
            sb.AppendLine($"      <div class='metric-card'><div class='metric-val accent'>{totalRules}</div><div class='metric-lbl'>Reglas Evaluadas</div></div>");
            sb.AppendLine($"      <div class='metric-card'><div class='metric-val'>{totalElements}</div><div class='metric-lbl'>Elementos Revisados</div></div>");
            sb.AppendLine($"      <div class='metric-card'><div class='metric-val error'>{totalNonCompliant}</div><div class='metric-lbl'>Incumplimientos</div></div>");
            sb.AppendLine($"      <div class='metric-card'><div class='metric-val success'>{globalCompliance}%</div><div class='metric-lbl'>Cumplimiento Global</div></div>");
            sb.AppendLine("    </div>");

            // Rule Details
            foreach (var ruleRes in ruleResults)
            {
                var rule = ruleRes.Rule;
                var nonCompliant = ruleRes.TestedElements.Where(e => e.Violations.Any(v => v.RuleId == rule.Id)).ToList();

                sb.AppendLine("    <div class='rule-section'>");
                sb.AppendLine("      <div class='rule-header'>");
                sb.AppendLine("        <div>");
                sb.AppendLine($"          <span class='rule-badge' style='background-color: {rule.HexColor};'>{rule.Id}</span>");
                sb.AppendLine($"          <span class='rule-title' style='margin-left: 8px;'>{rule.Name}</span>");
                sb.AppendLine($"          <div style='font-size: 12px; color: #64748b; margin-top: 4px;'>{rule.Description}</div>");
                sb.AppendLine("        </div>");
                sb.AppendLine($"        <div style='text-align: right;'><span style='font-weight: bold; color: {(nonCompliant.Count > 0 ? "#dc2626" : "#16a34a")};'>{nonCompliant.Count} Incumplimiento(s)</span> ({ruleRes.CompliancePercentage}% Cumplimiento)</div>");
                sb.AppendLine("      </div>");

                if (nonCompliant.Count == 0)
                {
                    sb.AppendLine("      <div style='padding: 16px; color: #16a34a; font-weight: bold;'>✓ Todos los elementos evaluados cumplen esta regla.</div>");
                }
                else
                {
                    sb.AppendLine("      <table>");
                    sb.AppendLine("        <thead>");
                    sb.AppendLine("          <tr>");
                    sb.AppendLine("            <th>ElementId</th><th>Categoría</th><th>Familia</th><th>Tipo</th><th>Nivel</th><th>Valor Encontrado</th><th>Observación</th>");
                    sb.AppendLine("          </tr>");
                    sb.AppendLine("        </thead>");
                    sb.AppendLine("        <tbody>");

                    foreach (var elem in nonCompliant)
                    {
                        string obs = elem.Violations.FirstOrDefault(v => v.RuleId == rule.Id)?.Observation ?? "Incumple regla";
                        sb.AppendLine("          <tr>");
                        sb.AppendLine($"            <td><strong>{elem.ElementId}</strong></td>");
                        sb.AppendLine($"            <td>{elem.CategoryName}</td>");
                        sb.AppendLine($"            <td>{elem.FamilyName}</td>");
                        sb.AppendLine($"            <td>{elem.TypeName}</td>");
                        sb.AppendLine($"            <td>{elem.LevelName}</td>");
                        sb.AppendLine($"            <td><em>{elem.ValueFound}</em></td>");
                        sb.AppendLine($"            <td class='status-error'>{obs}</td>");
                        sb.AppendLine("          </tr>");
                    }

                    sb.AppendLine("        </tbody>");
                    sb.AppendLine("      </table>");
                }

                sb.AppendLine("    </div>");
            }

            sb.AppendLine("    <div class='footer'>Generado automáticamente por AudiBIM - Auditoría de Calidad BIM</div>");
            sb.AppendLine("  </div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        public static string SaveAndExportPdfReport(
            string pdfFilePath,
            string projectName,
            int totalRules,
            int totalElements,
            int totalNonCompliant,
            double globalCompliance,
            System.Collections.Generic.IEnumerable<Models.RuleAuditResult> ruleResults)
        {
            string htmlContent = GenerateAuditReportHtml(projectName, totalRules, totalElements, totalNonCompliant, globalCompliance, ruleResults);
            string htmlFilePath = Path.ChangeExtension(pdfFilePath, ".html");

            File.WriteAllText(htmlFilePath, htmlContent, Encoding.UTF8);

            // Attempt to print/convert HTML to PDF using msedge or chrome headless if available
            try
            {
                string edgePath = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";
                if (!File.Exists(edgePath))
                {
                    edgePath = @"C:\Program Files\Microsoft\Edge\Application\msedge.exe";
                }

                if (File.Exists(edgePath))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = edgePath,
                        Arguments = $"--headless --disable-gpu --print-to-pdf=\"{pdfFilePath}\" \"{htmlFilePath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using (var proc = Process.Start(psi))
                    {
                        proc?.WaitForExit(10000);
                    }

                    if (File.Exists(pdfFilePath))
                    {
                        return pdfFilePath;
                    }
                }
            }
            catch
            {
                // Fallback to HTML file if headless print is blocked
            }

            return htmlFilePath;
        }
    }
}
