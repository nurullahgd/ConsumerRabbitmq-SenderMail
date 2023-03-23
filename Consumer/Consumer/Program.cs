using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Quartz;
using Quartz.Impl;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;


namespace Consumer
{
    public class Program
    {
        static async Task Main(string[] args)
        {

            var options = new DbContextOptionsBuilder<PostgreSqlDbContext>()
                .UseNpgsql("Server=localhost;Port=5432;Database=BookMagazineDatabase;User Id=postgres;Password=1234;Pooling=true;")
                .Options;
            addedMessage();
            
            var scheduler = await StdSchedulerFactory.GetDefaultScheduler();
            scheduler.Start();

            var job = JobBuilder.Create<EmailJob>()
                .WithIdentity("emailJob", "emailGroup")
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity("emailTrigger", "emailGroup")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(20) 
                    .RepeatForever())
                .Build();

            await scheduler.ScheduleJob(job, trigger);
            Console.ReadKey();
        }

        public static void addedMessage()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();

            channel.QueueDeclare(queue: "article-added-queue",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine("[Article ADDED] Received message: {0}", message);

                var article = JsonConvert.DeserializeObject<Article>(message);
                SendEmail("mokek26388@loongwin.com", "Yeni Makale Eklendi", $"Yeni bir makale eklendi: {article.Title}");
                
            };
            channel.BasicConsume(queue: "article-added-queue",
                                 autoAck: true,
                                 consumer: consumer);
            
        }
        public static void SendEmail(string receiver, string subject, string body)
        {
            using(var message = new MailMessage("minerva.langworth35@ethereal.email", receiver))
            {
                message.Subject = subject;
                message.Body = body;

                using(var client = new SmtpClient("smtp.ethereal.email", 587))
                {
                    client.UseDefaultCredentials = false;
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential("minerva.langworth35@ethereal.email", "SRx2aXgRwseYJftmXU");
                    client.Send(message);
                }
            }
        }

        public class EmailJob : IJob
        {
            public Task Execute(IJobExecutionContext context)
            {
                // Son 10 makaleyi veritabanından al
                var articles = GetLatestArticles(10);

                // E-posta gönderme kodu
                string to = "mokek26388@loongwin.com";
                string from = "minerva.langworth35 @ethereal.email"; 
                string subject = "Aylik makaleler";
                string body = "Merhaba, işte son 10 makaleniz:\n\n";

                foreach(var article in articles)
                {
                    body += $"{article.Title}\n";
                }

                using(MailMessage message = new MailMessage(from, to, subject, body))
                {
                    using(SmtpClient client = new SmtpClient("smtp.ethereal.email", 587))
                    {
                        client.EnableSsl = true;
                        client.UseDefaultCredentials = false;
                        client.Credentials = new NetworkCredential("minerva.langworth35@ethereal.email", "SRx2aXgRwseYJftmXU");
                        client.Send(message);
                    }
                }
                return Task.CompletedTask;
            }

            private List<Article> GetLatestArticles(int count)
            {
                // Veritabanından son 10 makaleyi al ve geri döndür
                var options = new DbContextOptionsBuilder<PostgreSqlDbContext>()
                .UseNpgsql("Server=localhost;Port=5432;Database=BookMagazineDatabase;User Id=postgres;Password=1234;Integrated Security=true;Pooling=true;")
                .Options;

                using(var db = new PostgreSqlDbContext(options))
                {
                    var returndata = db.Articles.OrderByDescending(a => a.CreatedTime).Take(count).ToList();
                    return returndata;
                }
            }
        }

        // Redis'e yazma işlemi
        //var redis = ConnectionMultiplexer.Connect("localhost");
        //var db = redis.GetDatabase();
        //db.StringSet(article.Id.ToString(), JsonConvert.SerializeObject(article),TimeSpan.FromDays(1));
        //db.KeyExpire(article.Id.ToString(), TimeSpan.FromDays(1));
        //Console.WriteLine("Written to Redis: {0}", message);

    }
}
