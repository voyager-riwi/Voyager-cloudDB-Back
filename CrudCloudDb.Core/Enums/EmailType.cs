namespace CrudCloudDb.Core.Enums
{
    /// <summary>
    /// Tipos de emails que se pueden enviar
    /// </summary>
    public enum EmailType
    {
        AccountCreated = 1,
        DatabaseCreated = 2,
        DatabaseDeleted = 3,
        PasswordReset = 4,
        PlanChanged = 5,
        AccountPasswordReset = 6,
        Welcome = 7,
        Other = 99
    }
}