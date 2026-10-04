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

                RevitHoverTooltipWindow? tooltipWindow = new RevitHoverTooltipWindow();
                bool isClosing = false;

                // Live Inspector event handlers for Revit View hover/selection
                EventHandler<Autodesk.Revit.UI.Events.SelectionChangedEventArgs> selectionHandler = (s, e) =>
                {
                    try
                    {
                        if (isClosing || _mainWindow == null || !_mainWindow.IsLoaded || !_mainWindow.IsVisible) return;
                        if (uiapp.ActiveUIDocument == null) return;

                        _mainWindow.Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                if (isClosing || _mainWindow == null || tooltipWindow == null) return;
                                vm.UpdateLiveInspector(uiapp.ActiveUIDocument);

                                var live = vm.LiveInspector;
                                if (live != null && live.HasSelection && live.Details.Count > 0)
                                {
                                    tooltipWindow.UpdateTooltip(live.ElementName, live.ElementId, live.CategoryName, live.Details);
                                    if (!tooltipWindow.IsVisible) tooltipWindow.Show();
                                    tooltipWindow.PositionNearCursor();
                                }
                                else
                                {
                                    if (tooltipWindow.IsVisible) tooltipWindow.Hide();
                                }
                            }
                            catch { }
                        });
                    }
                    catch { }
                };

                EventHandler<Autodesk.Revit.UI.Events.IdlingEventArgs> idlingHandler = (s, e) =>
                {
                    try
                    {
                        if (isClosing || _mainWindow == null || !_mainWindow.IsLoaded || tooltipWindow == null || !tooltipWindow.IsVisible) return;

                        _mainWindow.Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                if (!isClosing && tooltipWindow.IsVisible)
                                {
                                    tooltipWindow.PositionNearCursor();
                                }
                            }
                            catch { }
                        });
                    }
                    catch { }
                };

                uiapp.SelectionChanged += selectionHandler;
                uiapp.Idling += idlingHandler;

                _mainWindow.Closing += (s, e) =>
                {
                    try
                    {
                        isClosing = true;
                        vm.SaveSessionRules();
                        uiapp.SelectionChanged -= selectionHandler;
                        uiapp.Idling -= idlingHandler;

                        if (tooltipWindow != null)
                        {
                            tooltipWindow.Hide();
                            tooltipWindow.Close();
                            tooltipWindow = null;
                        }
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
    }
}
