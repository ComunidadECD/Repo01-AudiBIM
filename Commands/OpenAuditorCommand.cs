using System;
using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BIMQualityAuditor.Revit;
using BIMQualityAuditor.UI.ViewModels;
using BIMQualityAuditor.UI.Views;

namespace BIMQualityAuditor.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class OpenAuditorCommand : IExternalCommand
    {
        private static MainWindow? _mainWindow;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiapp = commandData.Application;
                UIDocument? uidoc = uiapp.ActiveUIDocument;
                Document? doc = uidoc?.Document;

                if (_mainWindow != null && _mainWindow.IsLoaded)
                {
                    _mainWindow.Activate();
                    if (_mainWindow.WindowState == System.Windows.WindowState.Minimized)
                    {
                        _mainWindow.WindowState = System.Windows.WindowState.Normal;
                    }
                    return Result.Succeeded;
                }

                var vm = new MainViewModel
                {
                    EventController = new ExternalEventController()
                };

                if (doc != null)
                {
                    vm.ActiveProjectName = doc.Title;
                    vm.UpdateSchemaFromRevit(doc);
                    if (uidoc != null)
                    {
                        vm.UpdateLiveInspector(uidoc);
                    }
                }

                _mainWindow = new MainWindow(vm);

                // Set Revit Main Window as owner of WPF window
                IntPtr revitHandle = uiapp.MainWindowHandle;
                var helper = new WindowInteropHelper(_mainWindow)
                {
                    Owner = revitHandle
                };

                _mainWindow.Closing += (s, e) =>
                {
                    try
                    {
                        vm.SaveSessionRules();
                    }
                    catch { }
                };

                _mainWindow.Closed += (s, e) =>
                {
                    _mainWindow = null;
                };

                _mainWindow.Show();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        public static void CloseActiveWindow()
        {
            try
            {
                var window = _mainWindow;
                if (window == null) return;

                if (window.Dispatcher.CheckAccess()) window.Close();
                else window.Dispatcher.Invoke(window.Close);
            }
            catch
            {
                _mainWindow = null;
            }
        }
    }
}
