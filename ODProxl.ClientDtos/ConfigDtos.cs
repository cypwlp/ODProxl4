namespace ODProxl.ClientDtos
{
    public record ConfigDto
    {
        public int CgId { get; set; }
        public string? CgUserAccount { get; set; }
        public string? CgType { get; set; }
        public string? CgModuleName { get; set; }
        public string? CgKey { get; set; }
        public string? CgValue { get; set; }
        public DateTime CgCreationTime { get; set; }
        public DateTime CgEditTime { get; set; }
    }

    public record CreateUserConfigDto
    {
        public string ConfigUserAccount { get; set; }
        public string ConfigKey { get; set; }
        public string ConfigValue { get; set; }
        //public string? ConfigType { get; set; }
        // public string? ConfigModuleName { get; set; }
    }

    public record UpdateUserConfigDto
    {
        public string ConfigKey { get; set; }
        public string ConfigValue { get; set; }
    }
}
