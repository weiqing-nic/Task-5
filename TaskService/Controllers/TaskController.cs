using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RabbitMQ.Client;
using task.DBContext;
using task.Models;

namespace task.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        private readonly TaskDbContext _context;

        public TaskController(TaskDbContext context)
        {
            _context = context;
        }

        // GET: api/Task
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskClass>>> GetTask()
        {
            return await _context.Task.ToListAsync();
        }

        // GET: api/Task/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TaskClass>> GetTaskClass(int id)
        {
            var taskClass = await _context.Task.FindAsync(id);

            if (taskClass == null)
            {
                return NotFound();
            }

            return taskClass;
        }

        // PUT: api/Task/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTaskClass(int id, TaskClass taskClass)
        {
            if (id != taskClass.Id)
            {
                return BadRequest();
            }

            _context.Entry(taskClass).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TaskClassExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Task
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<TaskClass>> PostTaskClass(TaskClass taskClass)
        {
            taskClass.Status = "Failed";
            _context.Task.Add(taskClass);
            await _context.SaveChangesAsync();

            var factory = new ConnectionFactory()
            {
                //HostName = "localhost" , 
                //Port = 30724
                HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST"),
                Port = Convert.ToInt32(Environment.GetEnvironmentVariable("RABBITMQ_PORT"))
            };

            Console.WriteLine(factory.HostName + ":" + factory.Port);
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {


                channel.QueueDeclare(queue: "tasks",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(taskClass));

                channel.BasicPublish(exchange: "",
                                     routingKey: "tasks",
                                     basicProperties: null,
                                     body: body);
            }

            return CreatedAtAction("GetTaskClass", new { id = taskClass.Id }, taskClass);
        }

        // DELETE: api/Task/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTaskClass(int id)
        {
            var taskClass = await _context.Task.FindAsync(id);
            if (taskClass == null)
            {
                return NotFound();
            }

            _context.Task.Remove(taskClass);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TaskClassExists(int id)
        {
            return _context.Task.Any(e => e.Id == id);
        }
    }
}
