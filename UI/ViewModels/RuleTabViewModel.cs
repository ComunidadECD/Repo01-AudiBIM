using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BIMQualityAuditor.Models;
using BIMQualityAuditor.Revit;
using BIMQualityAuditor.Services;

namespace BIMQualityAuditor.UI.ViewModels
{
    public enum ElementFilterMode
    {
        Todos,
        Cumplen,
        NoCumplen
    }

    public class RuleTabViewModel : BaseViewModel
    {
        private ElementFilterMode _currentFilter = ElementFilterMode.Todos;
        private ElementAuditResult? _selectedElement;

        public RuleDefinition Rule { get; }
        public ExternalEventController? EventController { get; set; }

        public ObservableCollection<ElementAuditResult> AllElements { get; } = new();
        public ObservableCollection<ElementAuditResult> FilteredElements { get; } = new();

        public ElementAuditResult? SelectedElement
        {
            get => _selectedElement;
            set => SetProperty(ref _selectedElement, value);
        }

        public int TotalEvaluated => AllElements.Count;
        public int CompliantCount => AllElements.Count(e => e.Complies || !e.Violations.Any(v => v.RuleId == Rule.Id));
        public int NonCompliantCount => AllElements.Count(e => e.Violations.Any(v => v.RuleId == Rule.Id));
        public double CompliancePercentage => TotalEvaluated == 0 ? 100.0 : System.Math.Round((double)CompliantCount / TotalEvaluated * 100.0, 1);

        public ElementFilterMode CurrentFilter
        {
            get => _currentFilter;
            set
            {
                if (SetProperty(ref _currentFilter, value))
                {
                    ApplyFilter();
                }
            }
        }

        public ObservableCollection<ElementAuditResult> SelectedElements { get; } = new();

        public ICommand SetFilterTodosCommand { get; }
        public ICommand SetFilterCumplenCommand { get; }
        public ICommand SetFilterNoCumplenCommand { get; }

        public ICommand SelectInRevitCommand { get; }
        public ICommand IsolateInRevitCommand { get; }
        public ICommand HighlightInRevitCommand { get; }
        public ICommand IsolateNonCompliantCommand { get; }
        public ICommand HighlightNonCompliantCommand { get; }
        public ICommand RestoreViewCommand { get; }

        public RuleTabViewModel(RuleDefinition rule)
        {
            Rule = rule;

            SetFilterTodosCommand = new RelayCommand(_ => CurrentFilter = ElementFilterMode.Todos);
            SetFilterCumplenCommand = new RelayCommand(_ => CurrentFilter = ElementFilterMode.Cumplen);
            SetFilterNoCumplenCommand = new RelayCommand(_ => CurrentFilter = ElementFilterMode.NoCumplen);

            SelectInRevitCommand = new RelayCommand(_ => ExecuteSelectInRevit());
            IsolateInRevitCommand = new RelayCommand(_ => ExecuteIsolateInRevit());
            HighlightInRevitCommand = new RelayCommand(_ => ExecuteHighlightInRevit());
            IsolateNonCompliantCommand = new RelayCommand(_ => ExecuteIsolateNonCompliant());
            HighlightNonCompliantCommand = new RelayCommand(_ => ExecuteHighlightNonCompliant());
            RestoreViewCommand = new RelayCommand(_ => ExecuteRestoreView());
        }

        public void RefreshMetrics()
        {
            OnPropertyChanged(nameof(TotalEvaluated));
            OnPropertyChanged(nameof(CompliantCount));
            OnPropertyChanged(nameof(NonCompliantCount));
            OnPropertyChanged(nameof(CompliancePercentage));
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            FilteredElements.Clear();
            var query = _currentFilter switch
            {
                ElementFilterMode.Cumplen => AllElements.Where(e => e.Complies || !e.Violations.Any(v => v.RuleId == Rule.Id)),
                ElementFilterMode.NoCumplen => AllElements.Where(e => e.Violations.Any(v => v.RuleId == Rule.Id)),
                _ => AllElements.AsEnumerable()
            };

            foreach (var item in query)
            {
                FilteredElements.Add(item);
            }
        }

        private IEnumerable<long> GetTargetElementIds()
        {
            if (SelectedElements.Count > 0)
            {
                return SelectedElements.Select(e => e.ElementId).ToList();
            }
            if (SelectedElement != null)
            {
                return new[] { SelectedElement.ElementId };
            }
            return FilteredElements.Select(e => e.ElementId).ToList();
        }

        private IEnumerable<long> GetNonCompliantElementIds()
        {
            return AllElements.Where(e => e.Violations.Any(v => v.RuleId == Rule.Id)).Select(e => e.ElementId).ToList();
        }

        private void ExecuteSelectInRevit()
        {
            if (EventController == null) return;
            var ids = GetTargetElementIds().ToList();
            if (!ids.Any()) return;

            EventController.EnqueueTask(app =>
            {
                UIDocument uidoc = app.ActiveUIDocument;
                if (uidoc != null)
                {
                    SelectionService.SelectElements(uidoc, ids);
                }
            });
        }

        private void ExecuteIsolateInRevit()
        {
            if (EventController == null) return;
            var ids = GetTargetElementIds().ToList();
            if (!ids.Any()) return;

            EventController.EnqueueTask(app =>
            {
                UIDocument uidoc = app.ActiveUIDocument;
                if (uidoc?.Document != null)
                {
                    // Automatically restore view first before applying new isolation
                    IsolationService.RestoreView(uidoc.Document);
                    GraphicOverrideService.ClearGraphicOverridesInView(uidoc.Document, AllElements.Select(e => e.ElementId));

                    IsolationService.IsolateElementsInView(uidoc.Document, ids);
                }
            });
        }

        private void ExecuteHighlightInRevit()
        {
            if (EventController == null) return;
            var ids = GetTargetElementIds().ToList();
            if (!ids.Any()) return;

            EventController.EnqueueTask(app =>
            {
                UIDocument uidoc = app.ActiveUIDocument;
                if (uidoc?.Document != null)
                {
                    // Automatically restore view first before applying new highlighting
                    IsolationService.RestoreView(uidoc.Document);
                    GraphicOverrideService.ClearGraphicOverridesInView(uidoc.Document, AllElements.Select(e => e.ElementId));

                    GraphicOverrideService.HighlightElementsInView(uidoc.Document, ids, Rule.HexColor);
                }
            });
        }

        private void ExecuteIsolateNonCompliant()
        {
            if (EventController == null) return;
            var ids = GetNonCompliantElementIds().ToList();
            if (!ids.Any()) return;

            EventController.EnqueueTask(app =>
            {
                UIDocument uidoc = app.ActiveUIDocument;
                if (uidoc?.Document != null)
                {
                    // Restore view first then isolate non-compliant
                    IsolationService.RestoreView(uidoc.Document);
                    GraphicOverrideService.ClearGraphicOverridesInView(uidoc.Document, AllElements.Select(e => e.ElementId));

                    IsolationService.IsolateElementsInView(uidoc.Document, ids);
                }
            });
        }

        private void ExecuteHighlightNonCompliant()
        {
            if (EventController == null) return;
            var ids = GetNonCompliantElementIds().ToList();
            if (!ids.Any()) return;

            EventController.EnqueueTask(app =>
            {
                UIDocument uidoc = app.ActiveUIDocument;
                if (uidoc?.Document != null)
                {
                    // Restore view first then highlight non-compliant
                    IsolationService.RestoreView(uidoc.Document);
                    GraphicOverrideService.ClearGraphicOverridesInView(uidoc.Document, AllElements.Select(e => e.ElementId));

                    GraphicOverrideService.HighlightElementsInView(uidoc.Document, ids, Rule.HexColor);
                }
            });
        }

        private void ExecuteRestoreView()
        {
            if (EventController == null) return;

            EventController.EnqueueTask(app =>
            {
                UIDocument uidoc = app.ActiveUIDocument;
                if (uidoc?.Document != null)
                {
                    IsolationService.RestoreView(uidoc.Document);
                    GraphicOverrideService.ClearGraphicOverridesInView(uidoc.Document, AllElements.Select(e => e.ElementId));
                }
            });
        }
    }
}
