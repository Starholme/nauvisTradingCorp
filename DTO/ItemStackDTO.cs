namespace DTO
{
    public class ItemStackDTO
    {
        public string NameAndQuality { get; set; } = "";
        public int Quantity { get; set; }
        public string Name
        {
            get
            {
                var split = NameAndQuality.Split(':');
                if (split.Length < 2) { return ""; }
                else return split[0];
            }
            set
            {
                var split = NameAndQuality.Split(':');
                string quality = "";
                if (split.Length == 2) { quality = split[1]; }
                NameAndQuality = value + ":" + quality;
            }
        }
        public string Quality
        {
            get
            {
                var split = NameAndQuality.Split(':');
                if (split.Length < 2) { return ""; }
                else return split[1];
            }
            set
            {
                var split = NameAndQuality.Split(':');
                string name = "";
                if (split.Length == 2) { name = split[0]; }
                NameAndQuality = name + ":" + value;
            }
        }
    }
}
