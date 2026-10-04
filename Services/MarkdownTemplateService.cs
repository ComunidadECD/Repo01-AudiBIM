using System;
using System.IO;

namespace BIMQualityAuditor.Services
{
    public static class MarkdownTemplateService
    {
        public static string GetTemplateContent()
        {
            return @"# PLANTILLA DE REGLAS BIM QUALITY AUDITOR
# ================================================
# Formato de definición de reglas para revisión de calidad en Revit.
# Puede editar este archivo con Bloc de Notas, VS Code, Obsidian, etc.
# 
# OPERADORES DISPONIBLES:
# - NOT_EMPTY  : El parámetro no debe estar vacío.
# - EMPTY      : El parámetro debe estar vacío.
# - EQUALS     : El valor del parámetro debe ser exactamente igual al VALOR especificado.
# - NOT_EQUALS : El valor del parámetro debe ser diferente al VALOR especificado.
# - CONTAINS   : El valor del parámetro debe contener el texto del VALOR especificado.
# - NOT_CONTAINS: El valor del parámetro NO debe contener el texto del VALOR especificado.
#
# SEVERIDADES DISPONIBLES: Alta, Media, Baja, Critica
# CATEGORIAS: Usar nombre de categoría en inglés (ej. Walls, Doors, Windows, Floors) o ALL_MODEL_ELEMENTS para todos.

---

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

---

## REGLA

ID: R02
NOMBRE: Puertas con tipo definido
DESCRIPCION: Todas las puertas deben tener un tipo válido asignado.
CATEGORIA: Doors
PARAMETRO: Type Name
OPERADOR: NOT_EMPTY
VALOR: 
SEVERIDAD: Media
COLOR: #FB8C00
ACTIVA: true

---

## REGLA

ID: R03
NOMBRE: Elementos con nivel
DESCRIPCION: Los elementos revisados deben estar asociados a un nivel.
CATEGORIA: ALL_MODEL_ELEMENTS
PARAMETRO: Level
OPERADOR: NOT_EMPTY
VALOR: 
SEVERIDAD: Alta
COLOR: #8E24AA
ACTIVA: true
";
        }

        public static bool SaveTemplateToFile(string targetPath)
        {
            try
            {
                string content = GetTemplateContent();
                File.WriteAllText(targetPath, content, System.Text.Encoding.UTF8);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
