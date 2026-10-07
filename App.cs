using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using BIMQualityAuditor.Commands;

namespace BIMQualityAuditor
{
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                string tabName = "Audit Model";
                // Revit ribbon panels are owned by the add-in that creates them. Keep
                // AudiBIM in its own panel while still grouping it on Audit Model;
                // this avoids mutating a panel managed by another add-in at startup.
                string panelName = "AudiBIM – Reportes";

                // Create Ribbon Tab if not already present
                try
                {
                    application.CreateRibbonTab(tabName);
                }
                catch
                {
                    // Tab might already exist
                }

                // Reuse the panel when its owning add-in has already created it.
                // Create it only when AudiBIM is the first audit add-in to load.
                RibbonPanel? panel = application.GetRibbonPanels(tabName)
                    .FirstOrDefault(p => string.Equals(p.Name, panelName, StringComparison.OrdinalIgnoreCase));
                panel ??= application.CreateRibbonPanel(tabName, panelName);

                // Create PushButton
                string assemblyPath = Assembly.GetExecutingAssembly().Location;
                string assemblyDir = Path.GetDirectoryName(assemblyPath) ?? string.Empty;

                PushButtonData buttonData = new PushButtonData(
                    "btnAudiBIM",
                    "Reportes\ny Excel",
                    assemblyPath,
                    typeof(OpenAuditorCommand).FullName)
                {
                    ToolTip = "AudiBIM: abre el listado de auditoría, cantidades, dashboard y exportación a Excel."
                };

                // Attach 32x32 and 16x16 PNG icons to Ribbon PushButton
                try
                {
                    string png32Path = Path.Combine(assemblyDir, "AudiBIM_32.png");
                    string png16Path = Path.Combine(assemblyDir, "AudiBIM_16.png");

                    if (File.Exists(png32Path))
                    {
                        buttonData.LargeImage = new BitmapImage(new Uri(png32Path, UriKind.Absolute));
                    }
                    if (File.Exists(png16Path))
                    {
                        buttonData.Image = new BitmapImage(new Uri(png16Path, UriKind.Absolute));
                    }
                }
                catch
                {
                    // Fallback
                }

                panel.AddItem(buttonData);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error en AudiBIM", ex.Message);
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            OpenAuditorCommand.CloseActiveWindow();
            return Result.Succeeded;
        }
    }
}
