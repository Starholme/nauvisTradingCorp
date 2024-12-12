
namespace DAL.DBO
{
    public class ItemStack
    {
        public int Id {  get; set; }
        public string Name { get; set; } = "";
        public string Quality { get; set; } = "";
        public int Quantity { get; set; }

        public int VaultId { get; set; }
        public Vault Vault { get; set; } = new ();
    }
}
