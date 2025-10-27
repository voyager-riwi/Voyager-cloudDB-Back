using CrudCloudDb.Core.Enums;

namespace CrudCloudDb.Core.Entities;

public class DatabaseInstance
{
    // Faltan TODAS estas propiedades:
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DatabaseEngine Engine { get; set; }
    public string Name { get; set; }
    public string ContainerId { get; set; }
    public int Port { get; set; }
    public string DatabaseName { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public DatabaseStatus Status { get; set; }
    public string ConnectionString { get; set; }
    public bool CredentialsViewed { get; set; }
    public DateTime CreatedAt { get; set; }
}