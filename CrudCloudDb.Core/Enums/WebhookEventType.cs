namespace CrudCloudDb.Core.Enums
{
    public enum WebhookEventType
    {
        UserCreated,
        DatabaseCreated,
        DatabaseDeleted,
        SubscriptionStarted,
        SubscriptionCancelled,
        ProductionError
    }
}