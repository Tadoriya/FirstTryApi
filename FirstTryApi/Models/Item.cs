namespace FirstTryApi.Models;

public class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int Price { get; set; }
    public int MaxQuantity { get; set; }
    public int ClickValue { get; set; }
}