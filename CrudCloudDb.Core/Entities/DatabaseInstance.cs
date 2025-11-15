using CrudCloudDb.Core.Enums;

namespace CrudCloudDb.Core.Entities
{
    public class DatabaseInstance
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }
        
        public DatabaseEngine Engine { get; set; }
        public string Name { get; set; }
        
        // ⭐ NUEVO: ID del contenedor maestro compartido
        public string MasterContainerId { get; set; }
        
        // ⭐ Ya no es el ID del contenedor individual, sino del maestro
        public string ContainerId => MasterContainerId;
        
        public int Port { get; set; }
        public string DatabaseName { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        
        public DatabaseStatus Status { get; set; }
        public string ConnectionString { get; set; }
        
        public bool CredentialsViewed { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }  
    }
}