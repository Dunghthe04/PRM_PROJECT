namespace Api.Models;

public class FeeCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DefaultAmount { get; set; }
}
