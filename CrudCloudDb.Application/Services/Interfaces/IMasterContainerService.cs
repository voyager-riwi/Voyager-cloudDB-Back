using CrudCloudDb.Core.Enums;

namespace CrudCloudDb.Application.Services.Interfaces
{
    /// <summary>
    /// Servicio para gestionar contenedores maestros compartidos
    /// </summary>
    public interface IMasterContainerService
    {
        /// <summary>
        /// Obtiene o crea un contenedor maestro para un motor de BD
        /// </summary>
        Task<MasterContainerInfo> GetOrCreateMasterContainerAsync(DatabaseEngine engine);
        
        /// <summary>
        /// Verifica si un contenedor maestro existe y está corriendo
        /// </summary>
        Task<bool> IsMasterContainerRunningAsync(DatabaseEngine engine);
        
        /// <summary>
        /// Obtiene información del contenedor maestro
        /// </summary>
        Task<MasterContainerInfo?> GetMasterContainerInfoAsync(DatabaseEngine engine);
    }
    
    /// <summary>
    /// Información de un contenedor maestro
    /// </summary>
    public class MasterContainerInfo
    {
        public string ContainerId { get; set; }
        public DatabaseEngine Engine { get; set; }
        public int Port { get; set; }
        public string Host { get; set; }
        public string AdminUsername { get; set; }
        public string AdminPassword { get; set; }
        public bool IsRunning { get; set; }
    }
}