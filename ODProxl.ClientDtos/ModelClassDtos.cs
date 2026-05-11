namespace ODProxl.ClientDtos;

public class ModelClassDto
{
    public int ClassId { get; set; }
    public int ModelId { get; set; }
    public int ParentClassId { get; set; }
    public string? ClassName { get; set; }
    public int ClassSuffix { get; set; }
    public string? ClassType { get; set; }
    public string? ClassCategories { get; set; }
    public string? Status { get; set; }
}

public class CreateModelClassDto
{
    public int ModelId { get; set; }
    public int ParentClassId { get; set; }
    public string? ClassName { get; set; }
    public int ClassSuffix { get; set; }
    public string? ClassType { get; set; }
    public string? ClassCategories { get; set; }
    public string? Status { get; set; }
}

public class UpdateModelClassDto
{
    //public int ModelId { get; set; }
    //public int ParentClassId { get; set; }
    public string? ClassName { get; set; }
    public int ClassSuffix { get; set; }
    public string? ClassType { get; set; }
    public string? ClassCategories { get; set; }
    public string? Status { get; set; }
}
