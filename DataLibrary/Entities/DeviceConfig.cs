using System.ComponentModel.DataAnnotations;

namespace DataLibrary.Entities
{
    public class DeviceConfig
    {
        [Key]
        public string? Id { get; set; }
        public string ConnectionString { get; set; } = null!;
        public string? Type { get; set; }
        public string? Location { get; set; }
    }
}
