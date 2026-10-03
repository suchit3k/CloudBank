//using Azure.Messaging.ServiceBus;

//const string connectionString = "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";
//const string topicName = "transaction-events";

//await using var client = new ServiceBusClient(connectionString);
//Console.WriteLine("Connected to Service Bus.");

//ServiceBusSender sender = client.CreateSender(topicName);

//var messageBody = new
//{
//    TransactionId = Guid.NewGuid(),
//    Amount = 100.00m,
//    Status = "Completed",
//    OccurredAtUtc = DateTime.UtcNow
//};

//string json = System.Text.Json.JsonSerializer.Serialize(messageBody);

//var message = new ServiceBusMessage(json)
//{
//    ContentType = "application/json",
//    Subject = "TransactionCompleted"
//};

//await sender.SendMessageAsync(message);

//Console.WriteLine($"Sent message: {json}");

using Azure.Messaging.ServiceBus;

const string connectionString = "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";
const string topicName = "transaction-events";
const string subscriptionName = "audit-sub";

await using var client = new ServiceBusClient(connectionString);
Console.WriteLine("Connected to Service Bus.");

ServiceBusReceiver receiver = client.CreateReceiver(topicName, subscriptionName);

Console.WriteLine("Waiting for a message...");

ServiceBusReceivedMessage message = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(30));

if (message is null)
{
    Console.WriteLine("No message received within 30 seconds.");
}
else
{
    string body = message.Body.ToString();
    Console.WriteLine($"Received message: {body}");
    Console.WriteLine($"Subject: {message.Subject}");
    Console.WriteLine($"MessageId: {message.MessageId}");

    await receiver.CompleteMessageAsync(message);
    Console.WriteLine("Message marked as complete.");
}