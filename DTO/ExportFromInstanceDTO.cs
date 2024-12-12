namespace DTO
{
    public class ExportFromInstanceDTO
    {
        public string InstanceId { get; set; } = "";
        public List<ItemStackDTO> Items { get; set; } = [];
    }
}
