using System;
using System.IO;
using System.Reflection;
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
                string tabName = "Automatización BIM";
                string panelName = "Auditoria";

                // Create Ribbon Tab if not already present
                try
                {
                    application.CreateRibbonTab(tabName);
                }
                catch
                {
                    // Tab might already exist
                }

                // Create Ribbon Panel
                RibbonPanel panel = application.CreateRibbonPanel(tabName, panelName);

                // Create PushButton
                string assemblyPath = Assembly.GetExecutingAssembly().Location;
                string assemblyDir = Path.GetDirectoryName(assemblyPath) ?? string.Empty;

                PushButtonData buttonData = new PushButtonData(
                    "btnAudiBIM",
                    "AudiBIM",
                    assemblyPath,
                    typeof(OpenAuditorCommand).FullName)
                {
                    ToolTip = "AudiBIM - Auditoría de Calidad BIM en Revit mediante reglas en Markdown."
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
            return Result.Succeeded;
        }
    }
}
