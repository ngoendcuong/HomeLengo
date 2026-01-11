namespace HomeLengo.Models;

public class PropertyViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "VNĐ";
    public string Status { get; set; } = string.Empty;
    public bool IsVip { get; set; }
    public string Image { get; set; } = string.Empty;
}

