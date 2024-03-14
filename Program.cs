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

// Database
var connString = "Host=localhost;Username=postgres;Password=151099;Database=pix-dotnet";
await using var conn = new NpgsqlConnection(connString);
await conn.OpenAsync();

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
    durable: false,
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
        var requestTask = client.PostAsJsonAsync("http://localhost:5039/payments/pix", payment.DTO, cts.Token);
        var response = await requestTask;

        if (response.IsSuccessStatusCode)
        {
            await using (var cmd = new NpgsqlCommand("UPDATE \"Payments\" SET \"Status\" = (@status) WHERE \"Id\" = @id", conn))
            {
                cmd.Parameters.AddWithValue("status", "SUCCESS");
                cmd.Parameters.AddWithValue("id", payment.Response.Id);
                await cmd.ExecuteNonQueryAsync();
            }

            Console.WriteLine("Transaction updated!");
        }
        else
        {
            Console.WriteLine($"Request failed with status code {response.StatusCode}");

            await using (var cmd = new NpgsqlCommand("UPDATE \"Payments\" SET \"Status\" = (@status) WHERE \"Id\" = @id", conn))
            {
                cmd.Parameters.AddWithValue("status", "FAILED");
                cmd.Parameters.AddWithValue("id", payment.Response.Id);
                await cmd.ExecuteNonQueryAsync();
            }

            Console.WriteLine("Transaction marked as FAILED!");
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Request timed out!");

        await using (var cmd = new NpgsqlCommand("UPDATE \"Payments\" SET \"Status\" = (@status) WHERE \"Id\" = @id", conn))
        {
            cmd.Parameters.AddWithValue("status", "FAILED");
            cmd.Parameters.AddWithValue("id", payment.Response.Id);
            await cmd.ExecuteNonQueryAsync();
        }

        Console.WriteLine("Transaction marked as FAILED!");
    }
    catch (Exception e)
    {
        Console.WriteLine($"Erro ao fazer a requisição: {e.Message}");
    }
};

channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

Console.WriteLine("Waiting for payment messages...");
Console.ReadLine();

// Start the web application
app.Run();
