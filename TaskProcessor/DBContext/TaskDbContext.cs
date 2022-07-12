using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskProcessor.Models;

namespace TaskProcessor.DBContext
{
    public class TaskDbContext : DbContext
    {
        public TaskDbContext(DbContextOptions options) : base(options)
        {

        }

        public DbSet<TaskClass> Task { get; set; }
        public DbSet<TaskProcess> TaskProcesses { get; set; }

    }
}
