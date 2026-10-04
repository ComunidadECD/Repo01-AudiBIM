using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BIMQualityAuditor.Core.Enums;
using BIMQualityAuditor.Models;
using BIMQualityAuditor.Parsing;
using BIMQualityAuditor.Revit;
using BIMQualityAuditor.Services;
using Microsoft.Win32;

namespace BIMQualityAuditor.UI.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private string _activeProjectName = "Ningún proyecto activo";
        private string _statusMessage = "Listo. Cargue o edite reglas para comenzar la auditoría.";
        private AuditState _state = AuditState.SIN_REVISAR;
        private AuditSummary _summary = new();
        private bool _hasAuditResults;
        private RuleDefinition? _selectedRule;
        private RuleTabViewModel? _selectedRuleTab;

        public ExternalEventController? EventController { get; set; }

        public string ActiveProjectName
        {
            get => _activeProjectName;
            set => SetProperty(ref _activeProjectName, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public AuditState State
        {
            get => _state;
            set => SetProperty(ref _state, value);
        }

        public AuditSummary Summary
        {
            get => _summary;
            set => SetProperty(ref _summary, value);
        }

        public bool HasAuditResults
        {
            get => _hasAuditResults;
            set => SetProperty(ref _hasAuditResults, value);
        }

        private LiveInspectedElement _liveInspector = new();

        public LiveInspectedElement LiveInspector
        {
            get => _liveInspector;
            set => SetProperty(ref _liveInspector, value);
        }

        public ObservableCollection<string> AvailableCategories { get; } = new();
        public ObservableCollection<string> AvailableParameters { get; } = new();
        public List<ColorOption> AvailableColors => ColorOption.AvailableColors;

        public ColorOption? SelectedColorOption
        {
            get
            {
                if (SelectedRule == null) return null;
                return AvailableColors.FirstOrDefault(c => c.HexColor.Equals(SelectedRule.HexColor, StringComparison.OrdinalIgnoreCase))
                       ?? AvailableColors.FirstOrDefault();
            }
            set
            {
                if (SelectedRule != null && value != null)
                {
                    SelectedRule.HexColor = value.HexColor;
                    OnPropertyChanged(nameof(SelectedColorOption));
                }
            }
        }

        public RuleDefinition? SelectedRule
        {
            get => _selectedRule;
            set
            {
                if (SetProperty(ref _selectedRule, value))
                {
                    OnPropertyChanged(nameof(SelectedColorOption));
                    RefreshCategoryParameters();
                }
            }
        }

        public RuleTabViewModel? SelectedRuleTab
        {
            get => _selectedRuleTab;
            set => SetProperty(ref _selectedRuleTab, value);
        }

        public ObservableCollection<RuleDefinition> Rules { get; } = new();
        public ObservableCollection<RuleParsingError> ParsingErrors { get; } = new();
        public ObservableCollection<RuleTabViewModel> RuleTabs { get; } = new();

        public int LoadedRulesCount => Rules.Count;
        public bool HasParsingErrors => ParsingErrors.Count > 0;
        public bool HasLoadedRules => Rules.Count > 0;

        public ICommand LoadRulesCommand { get; }
        public ICommand LoadSingleRuleCommand { get; }
        public ICommand SaveRulesCommand { get; }
        public ICommand ExportPdfReportCommand { get; }
        public ICommand DownloadTemplateCommand { get; }
        public ICommand AddRuleCommand { get; }
        public ICommand DeleteRuleCommand { get; }
        public ICommand ClearAllRulesCommand { get; }

        public ICommand ExecuteRulesCommand { get; }
        public ICommand RecheckCommand { get; }

        public Array AvailableOperators => Enum.GetValues(typeof(RuleOperator));
        public Array AvailableSeverities => Enum.GetValues(typeof(RuleSeverity));

        public MainViewModel()
        {
            LoadRulesCommand = new RelayCommand(_ => LoadRules());
            LoadSingleRuleCommand = new RelayCommand(_ => LoadSingleRule());
            SaveRulesCommand = new RelayCommand(_ => SaveRules(), _ => HasLoadedRules);
            ExportPdfReportCommand = new RelayCommand(_ => ExportPdfReport(), _ => HasAuditResults);
            DownloadTemplateCommand = new RelayCommand(_ => DownloadTemplate());
            AddRuleCommand = new RelayCommand(_ => AddNewRule());
            DeleteRuleCommand = new RelayCommand(_ => DeleteSelectedRule(), _ => SelectedRule != null);
            ClearAllRulesCommand = new RelayCommand(_ => ClearAllRules(), _ => HasLoadedRules);

            ExecuteRulesCommand = new RelayCommand(_ => ExecuteRules(), _ => HasLoadedRules);
            RecheckCommand = new RelayCommand(_ => RecheckRules(), _ => HasLoadedRules);

            LoadDefaultCategories();
            LoadInitialRules();
        }

        public void SaveSessionRules()
        {
            RulePersistenceService.SaveSessionRules(Rules);
        }

        private void LoadDefaultCategories()
        {
            AvailableCategories.Clear();
            var cats = ModelSchemaService.GetAvailableCategories(null!);
            foreach (var c in cats) AvailableCategories.Add(c);

            RefreshCategoryParameters();
        }

        public void UpdateSchemaFromRevit(Document doc)
        {
            if (doc == null) return;
            var cats = ModelSchemaService.GetAvailableCategories(doc);
            AvailableCategories.Clear();
            foreach (var c in cats) AvailableCategories.Add(c);

            RefreshCategoryParameters(doc);
        }

        public void RefreshCategoryParameters(Document? doc = null)
        {
            string catName = SelectedRule?.Category ?? "Walls";
            var paramsList = ModelSchemaService.GetParametersForCategory(doc!, catName);
            AvailableParameters.Clear();
            foreach (var p in paramsList) AvailableParameters.Add(p);
        }

        public void UpdateLiveInspector(UIDocument uidoc)
        {
            if (uidoc == null || uidoc.Document == null) return;
            Document doc = uidoc.Document;

            try
            {
                var selIds = uidoc.Selection.GetElementIds();
                if (selIds == null || selIds.Count == 0)
                {
                    LiveInspector = new LiveInspectedElement
                    {
                        HasSelection = false,
                        StatusSummary = "Seleccione un elemento en Revit para inspeccionar sus reglas en vivo."
                    };
                    return;
                }

                ElementId firstId = selIds.First();
                Element elem = doc.GetElement(firstId);
                if (elem == null) return;

                string elemName = elem.Name ?? "Elemento";
                if (elem is FamilyInstance fi && fi.Symbol != null)
                {
                    elemName = $"{fi.Symbol.FamilyName} - {fi.Name}";
                }

                string catName = elem.Category?.Name ?? "Desconocido";
                string levelName = ParameterService.GetLevelName(elem) ?? "N/A";

                var activeRules = Rules.Where(r => r.IsActive).ToList();
                var details = new List<RuleEvaluationDetail>();

                bool overallComplies = true;

                foreach (var rule in activeRules)
                {
                    // Check if category matches rule or rule applies to ALL_MODEL_ELEMENTS
                    bool catMatches = rule.Category.Equals("ALL_MODEL_ELEMENTS", StringComparison.OrdinalIgnoreCase) ||
                                     rule.Category.Equals("TODOS", StringComparison.OrdinalIgnoreCase) ||
                                     rule.Category.Equals(catName, StringComparison.OrdinalIgnoreCase);

                    if (!catMatches) continue;

                    string paramVal = ParameterService.GetParameterValueAsString(elem, rule.ParameterName);
                    bool complies = RuleEngine.EvaluateRule(paramVal, rule.Operator, rule.Value);

                    if (!complies) overallComplies = false;

                    details.Add(new RuleEvaluationDetail
                    {
                        RuleId = rule.Id,
                        RuleName = rule.Name,
                        Complies = complies,
                        HexColor = rule.HexColor,
                        Observation = complies 
                            ? $"Cumple ('{rule.ParameterName}' = \"{paramVal}\")" 
                            : $"Incumple ('{rule.ParameterName}' = \"{(string.IsNullOrWhiteSpace(paramVal) ? "<VACÍO>" : paramVal)}\")"
                    });
                }

                LiveInspector = new LiveInspectedElement
                {
                    ElementId = elem.Id.Value,
                    ElementName = elemName,
                    CategoryName = catName,
                    LevelName = levelName,
                    HasSelection = true,
                    OverallComplies = overallComplies,
                    StatusSummary = overallComplies 
                        ? $"✅ El elemento seleccionado cumple las {details.Count} regla(s) aplicables." 
                        : $"❌ El elemento seleccionado incumple {details.Count(d => !d.Complies)} de {details.Count} regla(s) aplicables.",
                    Details = details
                };
            }
            catch
            {
                // Live inspection safe error guard
            }
        }

        private void LoadInitialRules()
        {
            try
            {
                var savedRules = RulePersistenceService.LoadSessionRules();
                if (savedRules != null && savedRules.Count > 0)
                {
                    Rules.Clear();
                    foreach (var r in savedRules)
                    {
                        Rules.Add(r);
                    }
                    SelectedRule = Rules.FirstOrDefault();
                    OnPropertyChanged(nameof(LoadedRulesCount));
                    OnPropertyChanged(nameof(HasLoadedRules));
                    StatusMessage = $"Se restauraron {Rules.Count} reglas guardadas de la sesión anterior.";
                    return;
                }

                string templateText = MarkdownTemplateService.GetTemplateContent();
                var result = new RuleParseResult();
                RuleParser.ParseSingleMarkdownContent(templateText, "Reglas_Predeterminadas.md", result);
                
                Rules.Clear();
                foreach (var rule in result.Rules)
                {
                    Rules.Add(rule);
                }
                
                SelectedRule = Rules.FirstOrDefault();

                OnPropertyChanged(nameof(LoadedRulesCount));
                OnPropertyChanged(nameof(HasLoadedRules));
                StatusMessage = $"Se cargaron {Rules.Count} reglas iniciales de ejemplo.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al cargar reglas iniciales: {ex.Message}";
            }
        }

        private void LoadRules()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Seleccionar archivos de reglas Markdown",
                Filter = "Archivos Markdown (*.md)|*.md|Todos los archivos (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                var parseResult = RuleParser.ParseMarkdownFiles(dialog.FileNames);

                Rules.Clear();
                ParsingErrors.Clear();

                foreach (var rule in parseResult.Rules)
                {
                    Rules.Add(rule);
                }

                foreach (var err in parseResult.Errors)
                {
                    ParsingErrors.Add(err);
                }

                SelectedRule = Rules.FirstOrDefault();
                SaveSessionRules();

                OnPropertyChanged(nameof(LoadedRulesCount));
                OnPropertyChanged(nameof(HasLoadedRules));
                OnPropertyChanged(nameof(HasParsingErrors));

                if (HasParsingErrors)
                {
                    State = AuditState.ERROR_DE_REGLAS;
                    StatusMessage = $"Se cargaron {Rules.Count} reglas con {ParsingErrors.Count} advertencia(s)/error(es).";
                }
                else
                {
                    State = AuditState.SIN_REVISAR;
                    StatusMessage = $"Se cargaron correctamente {Rules.Count} reglas desde {dialog.FileNames.Length} archivo(s).";
                }
            }
        }

        private void LoadSingleRule()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Cargar 1 Regla (.md) para probar",
                Filter = "Archivos Markdown (*.md)|*.md|Todos los archivos (*.*)|*.*",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                var parseResult = RuleParser.ParseMarkdownFiles(new[] { dialog.FileName });
                int addedCount = 0;

                foreach (var rule in parseResult.Rules)
                {
                    if (Rules.Any(r => r.Id.Equals(rule.Id, StringComparison.OrdinalIgnoreCase)))
                    {
                        rule.Id = $"R{Rules.Count + 1:D2}";
                    }
                    Rules.Add(rule);
                    SelectedRule = rule;
                    addedCount++;
                }

                SaveSessionRules();

                OnPropertyChanged(nameof(LoadedRulesCount));
                OnPropertyChanged(nameof(HasLoadedRules));
                StatusMessage = $"Se agregó {addedCount} regla(s) desde '{Path.GetFileName(dialog.FileName)}'. Total en Revisión: {Rules.Count}.";
            }
        }

        private void SaveRules()
        {
            var dialog = new SaveFileDialog
            {
                Title = "Guardar Conjunto de Reglas (Revisión)",
                FileName = "Revision_Reglas_BIM.md",
                Filter = "Archivos Markdown (*.md)|*.md"
            };

            if (dialog.ShowDialog() == true)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("# CONJUNTO DE REGLAS BIM - AUDIBIM");
                sb.AppendLine($"# Creado: {DateTime.Now:dd/MM/yyyy HH:mm}");
                sb.AppendLine();

                foreach (var r in Rules)
                {
                    sb.AppendLine("## REGLA");
                    sb.AppendLine($"ID: {r.Id}");
                    sb.AppendLine($"NOMBRE: {r.Name}");
                    sb.AppendLine($"DESCRIPCION: {r.Description}");
                    sb.AppendLine($"CATEGORIA: {r.Category}");
                    sb.AppendLine($"PARAMETRO: {r.ParameterName}");
                    sb.AppendLine($"OPERADOR: {r.Operator}");
                    sb.AppendLine($"VALOR: {r.Value}");
                    sb.AppendLine($"SEVERIDAD: {r.Severity}");
                    sb.AppendLine($"COLOR: {r.HexColor}");
                    sb.AppendLine($"ACTIVA: {r.IsActive.ToString().ToLower()}");
                    sb.AppendLine();
                }

                File.WriteAllText(dialog.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                MessageBox.Show($"Reglas guardadas exitosamente en:\n{dialog.FileName}", "Reglas Guardadas", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExportPdfReport()
        {
            if (!HasAuditResults)
            {
                MessageBox.Show("Debe ejecutar la revisión antes de exportar el informe.", "Sin Resultados", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Exportar Informe PDF de Incumplimientos",
                FileName = $"Informe_Incumplimientos_{DateTime.Now:yyyyMMdd_HHmm}.pdf",
                Filter = "Documento PDF (*.pdf)|*.pdf|Página Web HTML (*.html)|*.html"
            };

            if (dialog.ShowDialog() == true)
            {
                var ruleResultsList = RuleTabs.Select(t => new RuleAuditResult
                {
                    Rule = t.Rule,
                    TestedElements = t.AllElements.ToList()
                });

                string exportedPath = PdfReportService.SaveAndExportPdfReport(
                    dialog.FileName,
                    ActiveProjectName,
                    Rules.Count(r => r.IsActive),
                    Summary.TotalElementsEvaluated,
                    Summary.TotalNonCompliantElements,
                    Summary.GlobalCompliancePercentage,
                    ruleResultsList);

                StatusMessage = $"Informe de auditoría exportado en: {exportedPath}";

                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = exportedPath,
                        UseShellExecute = true
                    });
                }
                catch
                {
                    // Ignore launch error
                }
            }
        }

        private void DownloadTemplate()
        {
            var dialog = new SaveFileDialog
            {
                Title = "Guardar Plantilla de Reglas BIM",
                FileName = "BIM_Quality_Rules_Template.md",
                Filter = "Archivo Markdown (*.md)|*.md"
            };

            if (dialog.ShowDialog() == true)
            {
                bool success = MarkdownTemplateService.SaveTemplateToFile(dialog.FileName);
                if (success)
                {
                    MessageBox.Show(
                        $"Plantilla guardada exitosamente en:\n{dialog.FileName}\n\nPuede editar este archivo con Bloc de Notas, VS Code u Obsidian.",
                        "Plantilla Descargada",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }

        private void AddNewRule()
        {
            int nextNumber = Rules.Count + 1;
            var newRule = new RuleDefinition
            {
                Id = $"R{nextNumber:D2}",
                Name = $"Nueva Regla {nextNumber}",
                Description = "Descripción de la regla...",
                Category = "Walls",
                ParameterName = "Comments",
                Operator = RuleOperator.NOT_EMPTY,
                Value = "",
                Severity = RuleSeverity.Media,
                HexColor = "#2563EB",
                IsActive = true,
                SourceFile = "Personalizada"
            };

            Rules.Add(newRule);
            SelectedRule = newRule;
            SaveSessionRules();

            OnPropertyChanged(nameof(LoadedRulesCount));
            OnPropertyChanged(nameof(HasLoadedRules));
            StatusMessage = $"Regla {newRule.Id} agregada.";
        }

        private void DeleteSelectedRule()
        {
            if (SelectedRule != null)
            {
                string deletedId = SelectedRule.Id;
                Rules.Remove(SelectedRule);
                SelectedRule = Rules.FirstOrDefault();
                SaveSessionRules();

                OnPropertyChanged(nameof(LoadedRulesCount));
                OnPropertyChanged(nameof(HasLoadedRules));
                StatusMessage = $"Regla {deletedId} eliminada.";
            }
        }

        private void ClearAllRules()
        {
            if (MessageBox.Show("¿Está seguro de que desea borrar todas las reglas cargadas?", "Confirmar Eliminación", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Rules.Clear();
                SelectedRule = null;
                RuleTabs.Clear();
                HasAuditResults = false;
                SaveSessionRules();

                OnPropertyChanged(nameof(LoadedRulesCount));
                OnPropertyChanged(nameof(HasLoadedRules));
                StatusMessage = "Todas las reglas fueron eliminadas.";
            }
        }

        public void ExecuteRules()
        {
            if (EventController != null)
            {
                State = AuditState.EN_PROCESO;
                StatusMessage = "Ejecutando auditoría sobre el modelo Revit activo...";

                EventController.EnqueueTask(app =>
                {
                    UIDocument? uidoc = app.ActiveUIDocument;
                    Document? doc = uidoc?.Document;

                    if (doc == null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            StatusMessage = "No hay ningún documento de Revit activo.";
                            State = AuditState.SIN_REVISAR;
                        });
                        return;
                    }

                    // Run audit ONLY for rules that are active (IsActive == true)
                    var auditResult = AuditService.RunAudit(doc, Rules.Where(r => r.IsActive));

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Summary = auditResult.Summary;
                        RuleTabs.Clear();

                        foreach (var ruleAudit in auditResult.RuleResults)
                        {
                            var tabVm = new RuleTabViewModel(ruleAudit.Rule)
                            {
                                EventController = EventController
                            };

                            foreach (var elemRes in ruleAudit.TestedElements)
                            {
                                tabVm.AllElements.Add(elemRes);
                            }
                            tabVm.RefreshMetrics();
                            RuleTabs.Add(tabVm);
                        }

                        SelectedRuleTab = RuleTabs.FirstOrDefault();
                        HasAuditResults = true;

                        if (Summary.TotalNonCompliantElements == 0)
                        {
                            State = AuditState.CORREGIDO;
                            StatusMessage = "✓ Todos los elementos cumplen todas las reglas ejecutadas. (100% Cumplimiento)";
                        }
                        else
                        {
                            State = AuditState.CON_ERRORES;
                            StatusMessage = $"Auditoría completada. {Summary.TotalNonCompliantElements} elemento(s) con incumplimientos ({Summary.GlobalCompliancePercentage}% Cumplimiento global).";
                        }
                    });
                });
            }
        }

        private void RecheckRules()
        {
            ExecuteRules();
        }
    }
}
