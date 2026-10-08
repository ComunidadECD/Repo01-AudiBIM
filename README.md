# 🏢 AudiBIM - BIM Quality Auditor para Autodesk Revit 2027

**AudiBIM** es una aplicación / Add-in para Autodesk Revit diseñada para realizar **auditorías automáticas de calidad en modelos BIM** mediante reglas configurables en archivos Markdown (`.md`).

---

## 🌟 Características Principales

- 📐 **Auditoría basada en Markdown (`.md`)**: Carga reglas legibles tanto por humanos como por el programa.
- ✏️ **Editor de Regla Integrado**: Edita o crea reglas con selección de categorías y parámetros extraídos directamente del modelo activo de Revit.
- 🎨 **Selector de Colores**: Asigna colores personalizados a cada regla directamente en la tabla de configuración.
- 🎯 **Acciones en Bloque en Revit**: Selecciona, aisla o resalta elementos en bloque (soporta selección múltiple con `Shift` / `Ctrl`).
- 🔍 **Inspector Flotante en Vivo**: Muestra directamente sobre la vista de Revit un cuadro flotante con la evaluación de reglas al pasar el cursor o hacer clic en los elementos.
- 📄 **Informe Ejecutivo en PDF**: Genera y exporta informes estructurados en PDF/HTML con la métrica global e incumplimientos detallados.
- 💾 **Persistencia de Sesión**: Guarda las reglas automáticamente para que no se pierdan al cerrar la interfaz o reiniciar Revit.

---

## 🏗️ Estructura del Proyecto

```
P1 Auditoria BIM/
├── App.cs                          # Registro del Ribbon, Panel "Auditoria" y Botón "AudiBIM"
├── BIMQualityAuditor.csproj        # Proyecto C# (.NET 10 para Revit 2027)
├── BIMQualityAuditor.addin         # Manifiesto Add-in para Revit 2027
├── Commands/
│   └── OpenAuditorCommand.cs       # Comando IExternalCommand y gestión de eventos
├── Core/
│   └── Enums/                      # Enums de Operadores, Severidad y Estados de Auditoría
├── Models/
│   └── RuleModels.cs               # Modelos de Reglas, Violaciones y Resultados
├── Parsing/
│   └── RuleParser.cs               # Parser tolerante para archivos Markdown (.md)
├── Revit/
│   └── ExternalEventController.cs  # Manejador seguro IExternalEventHandler para WPF Modeless
├── Services/
│   ├── AuditService.cs             # Coordinador principal de auditoría
│   ├── ElementCollectorService.cs  # Colector de elementos de modelo físicos
│   ├── GraphicOverrideService.cs   # Servicio de resaltado y sobrescritura de gráficos
│   ├── IsolationService.cs         # Servicio de ocultar/aislar elementos temporalmente
│   ├── ModelSchemaService.cs       # Extractor dinámico de categorías y parámetros del modelo
│   ├── ParameterService.cs         # Extractor seguro de valores de parámetros de Revit
│   ├── PdfReportService.cs         # Generador de informes ejecutivos PDF/HTML
│   ├── RuleEngine.cs               # Motor determinista de evaluación de reglas
│   └── RulePersistenceService.cs   # Guardado y restauración de sesión en AppData
└── UI/
    ├── ViewModels/                 # MainViewModel y RuleTabViewModel
    └── Views/                      # MainWindow (WPF) y RevitHoverTooltipWindow
```

---

## 🛠️ Requisitos de Desarrollo

- **Autodesk Revit 2027**
- **.NET SDK 10.0 (10.0.100 o posterior)**
- **Visual Studio compatible con .NET 10 / Visual Studio Code**

---

## 🚀 Instalación y Compilación

1. Clona el repositorio:
   ```bash
   git clone https://github.com/ComunidadECD/Repo01-AudiBIM.git
   ```

2. Compila el proyecto con .NET CLI o Visual Studio:
   ```bash
   dotnet build BIMQualityAuditor.csproj -c Release
   ```

3. Instala el manifiesto `.addin` en la carpeta de Add-ins de Revit:
   Copiar `BIMQualityAuditor.addin` a:
   `C:\Users\<TuUsuario>\AppData\Roaming\Autodesk\Revit\Addins\2027\`

4. Asegúrate de que la ruta del ensamblado `<Assembly>` dentro del archivo `.addin` apunte a la DLL compilada (`bin\Release\net10.0-windows\BIMQualityAuditor.dll`). Copia también los archivos de imagen de esa carpeta junto a la DLL.

5. Abre Revit 2027. Encontrarás el Add-in en la pestaña **Automatización BIM** -> Panel **Auditoria** -> Botón **AudiBIM**.

---

## 📄 Formato de Reglas en Markdown (`.md`)

```markdown
## REGLA
ID: R01
NOMBRE: Muros con código
DESCRIPCION: Todos los muros deben tener el parámetro Código correctamente definido.
CATEGORIA: Walls
PARAMETRO: Código
OPERADOR: NOT_EMPTY
VALOR: 
SEVERIDAD: Alta
COLOR: #E53935
ACTIVA: true
```

---

## 📜 Licencia y Comunidad

Desarrollado para la comunidad de aprendizaje **ECD (Escuela de Capacitación Digital)**.
