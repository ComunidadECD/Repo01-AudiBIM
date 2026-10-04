namespace BIMQualityAuditor.Core.Enums
{
    public enum RuleOperator
    {
        NOT_EMPTY,
        EMPTY,
        EQUALS,
        NOT_EQUALS,
        CONTAINS,
        NOT_CONTAINS,
        GREATER_THAN,
        LESS_THAN,
        BETWEEN,
        REGEX,
        EXISTS,
        NOT_EXISTS
    }

    public enum RuleSeverity
    {
        Baja,
        Media,
        Alta,
        Critica
    }

    public enum AuditState
    {
        SIN_REVISAR,
        EN_PROCESO,
        CON_ERRORES,
        CORREGIDO,
        ERROR_DE_REGLAS
    }
}
