namespace DTO
{
    public class ExportFromInstanceDTO
    {
        public int InstanceId { get; set; }
        public List<List<object>> Items { get; set; } = [];
    }
}
