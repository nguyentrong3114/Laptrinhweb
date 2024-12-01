namespace shopflowerproject.Models;

public class ShoppingCart
{
    public List<Flowers> Flowers { get; set; } = new List<Flowers>();
    public Flowers flower { get; set; }
    public decimal? ThanhTien {get;set;}
    public decimal? ThanhToan {get;set;}
}