namespace LexisCampusDMS.Core.Domain.Exceptions;

public class EntityNotFoundException : Exception
{
    public string ErrorCode { get; } = "ENTITY_NOT_FOUND";
    public string EntityName { get; }
    public object Key { get; }

    public EntityNotFoundException(string entityName, object key)
        : base($"El registro '{entityName}' con identificador '{key}' no fue encontrado.")
    {
        EntityName = entityName;
        Key = key;
    }

    public EntityNotFoundException(string message) : base(message)
    {
        EntityName = string.Empty;
        Key = string.Empty;
    }
}
