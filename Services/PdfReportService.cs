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
            System.Collections.Generic.IEnumerable<Models.RuleAuditResult> ruleResults,
            System.Collections.Generic.IEnumerable<Models.CategoryQuantity>? quantities = null)
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
            sb.AppendLine("    .dashboard-controls { display:flex; gap:12px; margin:0 0 16px; flex-wrap:wrap; }");
            sb.AppendLine("    .dashboard-controls input, .dashboard-controls select { padding:9px 10px; border:1px solid #cbd5e1; border-radius:6px; font:inherit; }");
            sb.AppendLine("    .dashboard-controls input { min-width:260px; }");
            sb.AppendLine("    .element-dashboard { margin:0 0 32px; border:1px solid #e2e8f0; border-radius:8px; overflow:hidden; }");
            sb.AppendLine("    .status-ok { color:#16a34a; font-weight:bold; }");
            sb.AppendLine("    .footer { text-align: center; margin-top: 32px; font-size: 12px; color: #94a3b8; border-top: 1px solid #e2e8f0; padding-top: 16px; }");
            sb.AppendLine("    .charts { display:grid; grid-template-columns:1fr 1.5fr; gap:18px; margin:0 0 28px; } .chart { border:1px solid #e2e8f0; border-radius:8px; padding:16px; } .chart h2 { font-size:15px; margin:0 0 10px; } .legend { font-size:12px; color:#475569; line-height:1.7; } .bar-label { font-size:11px; fill:#334155; } .bar-value { font-size:11px; fill:#0f172a; font-weight:bold; }");
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

            AppendQuantityDashboard(sb, quantities?.ToList() ?? new System.Collections.Generic.List<Models.CategoryQuantity>());

            // Interactive element-level dashboard. The report keeps one row per model element,
            // even when it breaches more than one rule, so counts remain understandable.
            var allElements = ruleResults
                .SelectMany(r => r.TestedElements)
                .GroupBy(e => e.UniqueId)
                .Select(g => g.First())
                .OrderBy(e => e.CategoryName)
                .ThenBy(e => e.ElementId)
                .ToList();

            sb.AppendLine("    <div class='element-dashboard'>");
            sb.AppendLine("      <div class='rule-header'><div><span class='rule-title'>Dashboard iterativo por elemento</span><div style='font-size:12px;color:#64748b;margin-top:4px;'>Filtre y revise el estado de cada elemento evaluado.</div></div><div style='font-weight:bold;color:#2563eb;' id='visibleCount'></div></div>");
            sb.AppendLine("      <div style='padding:16px;'>");
            sb.AppendLine("        <div class='dashboard-controls'><input id='elementSearch' type='search' placeholder='Buscar por ID, categoría, familia, tipo o nivel'><select id='statusFilter'><option value='all'>Todos los estados</option><option value='error'>Solo incumplimientos</option><option value='ok'>Solo cumplen</option></select></div>");
            sb.AppendLine("        <table id='elementTable'><thead><tr><th>Estado</th><th>ElementId</th><th>Categoría</th><th>Familia</th><th>Tipo</th><th>Nivel</th><th>Reglas / observaciones</th></tr></thead><tbody>");
            foreach (var elem in allElements)
            {
                bool complies = elem.Complies;
                string observations = complies
                    ? "Sin incidencias"
                    : string.Join(" | ", elem.Violations.Select(v => $"{v.RuleId}: {v.Observation}"));
                string status = complies ? "✓ Cumple" : $"✕ {elem.Violations.Count} incidencia(s)";
                string css = complies ? "status-ok" : "status-error";
                string dataStatus = complies ? "ok" : "error";
                sb.AppendLine($"<tr data-status='{dataStatus}'><td class='{css}'>{status}</td><td><strong>{Html(elem.ElementId.ToString())}</strong></td><td>{Html(elem.CategoryName)}</td><td>{Html(elem.FamilyName)}</td><td>{Html(elem.TypeName)}</td><td>{Html(elem.LevelName)}</td><td>{Html(observations)}</td></tr>");
            }
            sb.AppendLine("        </tbody></table>");
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
            sb.AppendLine("<script>const search=document.getElementById('elementSearch'),filter=document.getElementById('statusFilter'),rows=[...document.querySelectorAll('#elementTable tbody tr')],count=document.getElementById('visibleCount');function update(){let n=0,q=search.value.toLowerCase();rows.forEach(r=>{let on=(filter.value==='all'||r.dataset.status===filter.value)&&r.innerText.toLowerCase().includes(q);r.style.display=on?'':'none';if(on)n++;});count.textContent=n+' / '+rows.length+' elementos visibles';}search.addEventListener('input',update);filter.addEventListener('change',update);update();</script>");
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
            System.Collections.Generic.IEnumerable<Models.RuleAuditResult> ruleResults,
            System.Collections.Generic.IEnumerable<Models.CategoryQuantity>? quantities = null)
        {
            string htmlContent = GenerateAuditReportHtml(projectName, totalRules, totalElements, totalNonCompliant, globalCompliance, ruleResults, quantities);
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

        private static string Html(string? value) => System.Net.WebUtility.HtmlEncode(value ?? string.Empty);

        private static void AppendQuantityDashboard(StringBuilder sb, System.Collections.Generic.List<Models.CategoryQuantity> quantities)
        {
            sb.AppendLine("    <section><h2 style='font-size:19px;margin:0 0 5px;'>Dashboard de cantidades</h2><p class='subtitle' style='margin:0 0 14px;'>Conteo, longitud, área y volumen leídos directamente del modelo Revit activo.</p>");
            if (quantities.Count == 0)
            {
                sb.AppendLine("<div class='chart'>No se encontraron categorías de modelo con cantidades para mostrar.</div></section>");
                return;
            }

            var top = quantities.OrderByDescending(q => q.ElementCount).Take(8).ToList();
            int total = quantities.Sum(q => q.ElementCount);
            string[] colors = { "#2563eb", "#16a34a", "#f59e0b", "#dc2626", "#8b5cf6", "#0891b2", "#db2777", "#64748b" };
            var pie = top.Take(7).ToList();
            int remaining = total - pie.Sum(q => q.ElementCount);
            if (remaining > 0) pie.Add(new Models.CategoryQuantity { CategoryName = "Otras categorías", ElementCount = remaining });
            sb.AppendLine("<div class='charts'><div class='chart'><h2>Distribución por categoría</h2><svg viewBox='0 0 220 220' width='100%' role='img' aria-label='Gráfico de torta de elementos por categoría'>");
            double start = -90;
            for (int i = 0; i < pie.Count; i++)
            {
                double sweep = total == 0 ? 0 : (double)pie[i].ElementCount / total * 360;
                sb.AppendLine($"<path d='{DonutSlice(110, 110, 82, 48, start, start + sweep)}' fill='{colors[i]}'><title>{Html(pie[i].CategoryName)}: {pie[i].ElementCount}</title></path>");
                start += sweep;
            }
            sb.AppendLine("<circle cx='110' cy='110' r='43' fill='#fff'/><text x='110' y='106' text-anchor='middle' style='font-size:22px;font-weight:bold;fill:#0f172a;'>" + total + "</text><text x='110' y='125' text-anchor='middle' style='font-size:11px;fill:#64748b;'>elementos</text></svg><div class='legend'>");
            for (int i = 0; i < pie.Count; i++) sb.AppendLine($"<span style='color:{colors[i]};font-weight:bold;'>■</span> {Html(pie[i].CategoryName)} ({pie[i].ElementCount})<br>");
            sb.AppendLine("</div></div><div class='chart'><h2>Elementos por categoría</h2><svg viewBox='0 0 500 250' width='100%' role='img' aria-label='Gráfico de barras de elementos por categoría'>");
            int max = Math.Max(1, top.Max(q => q.ElementCount));
            for (int i = 0; i < top.Count; i++)
            {
                int y = 18 + i * 28; double w = 300.0 * top[i].ElementCount / max;
                sb.AppendLine($"<text x='0' y='{y + 12}' class='bar-label'>{Html(Short(top[i].CategoryName, 19))}</text><rect x='150' y='{y}' width='{w.ToString(System.Globalization.CultureInfo.InvariantCulture)}' height='16' rx='3' fill='{colors[i]}'/><text x='{155 + w.ToString(System.Globalization.CultureInfo.InvariantCulture)}' y='{y + 12}' class='bar-value'>{top[i].ElementCount}</text>");
            }
            sb.AppendLine("</svg></div></div><div class='element-dashboard'><table><thead><tr><th>Categoría</th><th>Elementos</th><th>Longitud</th><th>Área</th><th>Volumen</th></tr></thead><tbody>");
            foreach (var q in quantities) sb.AppendLine($"<tr><td>{Html(q.CategoryName)}</td><td>{q.ElementCount}</td><td>{q.LengthDisplay}</td><td>{q.AreaDisplay}</td><td>{q.VolumeDisplay}</td></tr>");
            sb.AppendLine("</tbody></table></div></section>");
        }

        private static string Short(string value, int max) => value.Length <= max ? value : value.Substring(0, max - 1) + "…";

        private static string DonutSlice(double cx, double cy, double outer, double inner, double startDegrees, double endDegrees)
        {
            if (endDegrees - startDegrees >= 359.999)
            {
                return $"M {cx - outer:F2} {cy:F2} A {outer} {outer} 0 1 1 {cx + outer:F2} {cy:F2} A {outer} {outer} 0 1 1 {cx - outer:F2} {cy:F2} L {cx - inner:F2} {cy:F2} A {inner} {inner} 0 1 0 {cx + inner:F2} {cy:F2} A {inner} {inner} 0 1 0 {cx - inner:F2} {cy:F2} Z";
            }
            double ToX(double radius, double degree) => cx + radius * Math.Cos(degree * Math.PI / 180);
            double ToY(double radius, double degree) => cy + radius * Math.Sin(degree * Math.PI / 180);
            bool large = endDegrees - startDegrees > 180;
            return $"M {ToX(outer, startDegrees):F2} {ToY(outer, startDegrees):F2} A {outer} {outer} 0 {(large ? 1 : 0)} 1 {ToX(outer, endDegrees):F2} {ToY(outer, endDegrees):F2} L {ToX(inner, endDegrees):F2} {ToY(inner, endDegrees):F2} A {inner} {inner} 0 {(large ? 1 : 0)} 0 {ToX(inner, startDegrees):F2} {ToY(inner, startDegrees):F2} Z";
        }
    }
}
