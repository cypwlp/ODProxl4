namespace ODProxl.ClientDtos;

public record ModelDto
{
    public int RowIndex { get; set; }
    public int ModelId { get; set; }
    public string ModelName { get; set; }
    public string ModelPath { get; set; }
    public string ModelCategories { get; set; }
    public string Status { get; set; }
}

public record CreateModelDto
{
    public string ModelName { get; set; }
    public string ModelPath { get; set; }
    public string ModelCategories { get; set; }
    public string Status { get; set; }
}

public record UpdateModelDto
{
    //public string ModelName { get; set; }
    //public string ModelPath { get; set; }
    public string ModelCategories { get; set; }
    public string Status { get; set; }
}
