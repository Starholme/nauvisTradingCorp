namespace DAL.DBO
{
    public class Instance
    {
        public int Id { get; set; }
        public string ClusterInstanceId { get; set; } = "";
        
        public int VaultId { get; set; }
        public virtual Vault Vault { get; set; } = new ();
    }
}
