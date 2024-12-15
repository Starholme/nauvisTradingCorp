using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO
{
    public class ImportFullfillmentForInstanceJSON
    {
        public int InstanceId { get; set; }
        public Dictionary<string, int> Items { get; set; } = [];
    }
}
