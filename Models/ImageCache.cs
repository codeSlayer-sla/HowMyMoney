using System;
using System.ComponentModel.DataAnnotations;

namespace HowsMyMoney.Models;

public class ImageCache
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string Url { get; set; } = string.Empty;
    
    [Required]
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    public string ContentType { get; set; } = "image/jpeg";
}
