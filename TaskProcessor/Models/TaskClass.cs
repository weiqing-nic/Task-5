using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskProcessor.Models
{
    public class TaskClass
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public string CustomerId { get; set; }
    }
}
