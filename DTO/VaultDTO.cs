using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO
{
    public class VaultDTO
    {
        public int Id { get; set; }
        public Guid OwnerId { get; set; }

        public virtual List<ItemStackDTO> Items { get; set; } = [];
    }
}
