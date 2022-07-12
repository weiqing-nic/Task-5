using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TaskProcessor.DBContext;
using TaskProcessor.Models;

namespace TaskProcessor.Background
{
    public class ConsumerRabbitMQ : BackgroundService
    {
        private readonly ILogger _logger;
        private IConnection _connection;
        private IModel _channel;

        private readonly IServiceProvider _serviceProvider;

        public ConsumerRabbitMQ(ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
        {
            this._logger = loggerFactory.CreateLogger<ConsumerRabbitMQ>();
            _serviceProvider = serviceProvider;
            InitRabbitMQ();
        }

        private void InitRabbitMQ()
        {
            var factory = new ConnectionFactory
            {

                // HostName = "localhost" , 
                // Port = 30724
                HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST"),
                Port = Convert.ToInt32(Environment.GetEnvironmentVariable("RABBITMQ_PORT"))

            };

            // create connection  
            _connection = factory.CreateConnection();

            // create channel  
            _channel = _connection.CreateModel();

            //_channel.ExchangeDeclare("demo.exchange", ExchangeType.Topic);
            _channel.QueueDeclare("tasks", false, false, false, null);
            // _channel.QueueBind("demo.queue.log", "demo.exchange", "demo.queue.*", null);
            // _channel.BasicQos(0, 1, false);

            _connection.ConnectionShutdown += RabbitMQ_ConnectionShutdown;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (ch, ea) =>
            {
                // received message  
                //var content = System.Text.Encoding.UTF8.GetString(ea.Body.ToArray());
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                TaskClass newMessage = JsonConvert.DeserializeObject<TaskClass>(message);
                // handle the received message  
                HandleMessage(newMessage);
                _channel.BasicAck(ea.DeliveryTag, false);
            };

            consumer.Shutdown += OnConsumerShutdown;
            consumer.Registered += OnConsumerRegistered;
            consumer.Unregistered += OnConsumerUnregistered;
            consumer.ConsumerCancelled += OnConsumerConsumerCancelled;

            _channel.BasicConsume("tasks", false, consumer);
            return Task.CompletedTask;
        }

        private async void HandleMessage(TaskClass content)
        {
            // we just print this message   
            using (var scope = _serviceProvider.CreateScope())
            {
                TaskDbContext _context = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
                _context.Task.Add(content);
                await _context.SaveChangesAsync();
                string outputstring = "Output: " + content.Id.ToString() + "," + content.Description + "," + content.Priority + "," + content.CustomerId;

                var factory = new ConnectionFactory()
                {
                    //HostName = "localhost" , 
                    //Port = 30724
                    HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST"),
                    Port = Convert.ToInt32(Environment.GetEnvironmentVariable("RABBITMQ_PORT"))
                };
                _logger.LogInformation($"consumers received {outputstring}");
                Console.WriteLine($"consumer received {outputstring}");
                Console.WriteLine($"consumer received {outputstring}");

                content.Status = "Completed";
                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {


                    channel.QueueDeclare(queue: "task-processed",
                                         durable: false,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);
                    
                    var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(content));

                    channel.BasicPublish(exchange: "",
                                         routingKey: "task-processed",
                                         basicProperties: null,
                                         body: body);
                }
            }

        }

        private void OnConsumerConsumerCancelled(object sender, ConsumerEventArgs e) { }
        private void OnConsumerUnregistered(object sender, ConsumerEventArgs e) { }
        private void OnConsumerRegistered(object sender, ConsumerEventArgs e) { }
        private void OnConsumerShutdown(object sender, ShutdownEventArgs e) { }
        private void RabbitMQ_ConnectionShutdown(object sender, ShutdownEventArgs e) { }

        public override void Dispose()
        {
            _channel.Close();
            _connection.Close();
            base.Dispose();
        }
    }
}
