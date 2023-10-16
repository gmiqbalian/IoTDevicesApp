using System.ComponentModel.DataAnnotations;

namespace DataLibrary.Entities
{
    public class DeviceConfig
    {
        [Key]
        public string DeviceId { get; set; } = null!;
        public string ConnectionString { get; set; } = null!;
        public string? InstalledIn { get; set; }
        public string? Type { get; set; }
    }
}
