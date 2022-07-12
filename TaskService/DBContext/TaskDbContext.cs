using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using task.Models;

namespace task.DBContext
{
    public class TaskDbContext : DbContext
    {
        public TaskDbContext(DbContextOptions options) : base(options)
        {

        }

        public DbSet<TaskClass> Task { get; set; }
    }
}
  
