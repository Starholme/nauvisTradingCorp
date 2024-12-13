namespace DAL.DBO
{
    public class Vault
    {
        public int Id { get; set; }
        public Guid OwnerId { get; set; }

        public virtual List<ItemStack>? Items { get; set; }

        public virtual List<Instance>? Instances { get; set; }
    }
}
