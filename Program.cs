using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Npgsql;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// RabbitMQ configuration
var connectionFactory = new ConnectionFactory()
{
    HostName = "localhost",
    UserName = "admin",
    Password = "admin"
};

var queueName = "Payments";

using var connection = connectionFactory.CreateConnection();
using var channel = connection.CreateModel();
using var client = new HttpClient();

channel.QueueDeclare(
    queue: queueName,
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: null
);

Console.WriteLine("[*] Waiting for messages...");

var consumer = new EventingBasicConsumer(channel);
consumer.Received += async (model, ea) =>
{
    var body = ea.Body.ToArray();
    var jsonMessage = Encoding.UTF8.GetString(body);
    var payment = JsonSerializer.Deserialize<PaymentMessage>(jsonMessage);

    Console.WriteLine("Received payment message: {0}", jsonMessage);

    var cts = new System.Threading.CancellationTokenSource();
    cts.CancelAfter(TimeSpan.FromSeconds(120));

    try
    {
        var destinyTask = client.PostAsJsonAsync($"http://localhost:5039/payments/pix", payment.DTO, cts.Token);
        var destinyResponse = await destinyTask;

        if (destinyResponse.IsSuccessStatusCode)
        {
            await ProcessPayment(payment, "SUCCESS");

            channel.BasicAck(ea.DeliveryTag, false);
        }
        else
        {
            await ProcessPayment(payment, "FAILED");

            channel.BasicAck(ea.DeliveryTag, false);
        }
    }
    catch
    {
        await ProcessPayment(payment, "FAILED");

        channel.BasicAck(ea.DeliveryTag, false);
    }

    Console.WriteLine("Payment processed.");
};

async Task ProcessPayment(PaymentMessage payment, string status)
{
    await UpdatePaymentStatusAsync(payment.PaymentId, status);

    Console.WriteLine($"Transaction marked as {status}!");

    var originBody = new PaymentStatusDTO
    {
        Id = payment.PaymentId,
        Status = status
    };
    await client.PatchAsJsonAsync($"http://localhost:5039/payments/pix", originBody);
}

async Task UpdatePaymentStatusAsync(int paymentId, string status)
{
    try
    {
        var body = new PaymentStatusDTO
        {
            Id = paymentId,
            Status = status
        };
        await client.PutAsJsonAsync("http://localhost:8080/payments/update", body);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error updating payment status: {ex.Message}");
    }
}


channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

Console.WriteLine("Waiting for payment messages...");
Console.ReadLine();

// Start the web application
app.Run();
