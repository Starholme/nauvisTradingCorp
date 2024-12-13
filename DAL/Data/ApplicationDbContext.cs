using DAL.DBO;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DAL.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public required DbSet<Instance> Instances { get; set; }
        public required DbSet<ItemStack> Items { get; set; }
        public required DbSet<Vault> Vaults { get; set; }

    }
}
