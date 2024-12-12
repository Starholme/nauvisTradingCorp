using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO
{
    public class InstanceDTO
    {
        public string Name { get; set; } = "";
        public int InstanceId { get; set; }
        public string Status { get; set; } = "";
        public int Port { get; set; } 
    }
}
