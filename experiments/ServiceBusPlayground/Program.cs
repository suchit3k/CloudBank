//////using Azure.Messaging.ServiceBus;

//////const string connectionString = "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";
//////const string topicName = "transaction-events";

//////await using var client = new ServiceBusClient(connectionString);
//////Console.WriteLine("Connected to Service Bus.");

//////ServiceBusSender sender = client.CreateSender(topicName);

//////var messageBody = new
//////{
//////    TransactionId = Guid.NewGuid(),
//////    Amount = 100.00m,
//////    Status = "Completed",
//////    OccurredAtUtc = DateTime.UtcNow
//////};

//////string json = System.Text.Json.JsonSerializer.Serialize(messageBody);

//////var message = new ServiceBusMessage(json)
//////{
//////    ContentType = "application/json",
//////    Subject = "TransactionCompleted"
//////};

//////await sender.SendMessageAsync(message);

//////Console.WriteLine($"Sent message: {json}");

////using Azure.Messaging.ServiceBus;

////const string connectionString = "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";
////const string topicName = "transaction-events";
////const string subscriptionName = "notifications-sub";

////await using var client = new ServiceBusClient(connectionString);
////Console.WriteLine("Connected to Service Bus.");

////ServiceBusReceiver receiver = client.CreateReceiver(topicName, subscriptionName);

////Console.WriteLine("Waiting for a message...");

////ServiceBusReceivedMessage message = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(30));

////if (message is null)
////{
////    Console.WriteLine("No message received within 30 seconds.");
////}
////else
////{
////    string body = message.Body.ToString();
////    Console.WriteLine($"Received message: {body}");
////    Console.WriteLine($"Subject: {message.Subject}");
////    Console.WriteLine($"MessageId: {message.MessageId}");

////    await receiver.CompleteMessageAsync(message);
////    Console.WriteLine("Message marked as complete.");
////}

//using Azure.Messaging.ServiceBus;

//const string connectionString = "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";
//const string topicName = "transaction-events";

//await using var client = new ServiceBusClient(connectionString);
//ServiceBusSender sender = client.CreateSender(topicName);

//const string validPayload = "{\"TransactionId\":\"00000000-0000-0000-0000-000000000001\",\"Amount\":1,\"Currency\":\"USD\"}";

//// Test A: re-send an event Audit has ALREADY stored (paste the real MessageId)
//await SendAsync("d2180438-33eb-4b34-bee0-960dfd4630ba", validPayload);

//// Test B: a brand-new event, sent TWICE with the same MessageId
//await SendAsync("aaaaaaaa-0000-0000-0000-000000000001", validPayload);
//await SendAsync("aaaaaaaa-0000-0000-0000-000000000001", validPayload);

//// Test C: a poison message (payload is not valid JSON)
//await SendAsync("bbbbbbbb-0000-0000-0000-000000000001", "this is not json");

//Console.WriteLine("All test messages sent.");

//async Task SendAsync(string messageId, string payload)
//{
//    var message = new ServiceBusMessage(payload)
//    {
//        MessageId = messageId,
//        Subject = "TransactionCompletedEvent",
//        ContentType = "application/json"
//    };
//    await sender.SendMessageAsync(message);
//    Console.WriteLine($"Sent MessageId {messageId}");
//}
using Azure.Messaging.ServiceBus;

const string connectionString = "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";
const string topicName = "transaction-events";
const string subscriptionName = "audit-sub";

string mode = args.Length > 0 ? args[0] : "dlq";

await using var client = new ServiceBusClient(connectionString);

if (mode == "send")
{
    ServiceBusSender sender = client.CreateSender(topicName);
    var message = new ServiceBusMessage("{\"TransactionId\":\"00000000-0000-0000-0000-000000000002\",\"Amount\":2,\"Currency\":\"USD\"}")
    {
        MessageId = "cccccccc-0000-0000-0000-000000000001",
        Subject = "TransactionCompletedEvent",
        ContentType = "application/json"
    };
    await sender.SendMessageAsync(message);
    Console.WriteLine("Sent MessageId cccccccc-0000-0000-0000-000000000001");
}
else if (mode == "resubmit")
{
    string targetMessageId = args.Length > 1
        ? args[1]
        : throw new ArgumentException("Usage: dotnet run resubmit <messageId>");

    ServiceBusReceiver dlqReceiver = client.CreateReceiver(topicName, subscriptionName,
        new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });
    ServiceBusSender sender = client.CreateSender(topicName);

    var messages = await dlqReceiver.ReceiveMessagesAsync(maxMessages: 10, maxWaitTime: TimeSpan.FromSeconds(5));

    foreach (var dlqMessage in messages)
    {
        if (dlqMessage.MessageId == targetMessageId)
        {
            // Copy body, MessageId, Subject, and properties into a fresh message
            await sender.SendMessageAsync(new ServiceBusMessage(dlqMessage));
            // Only remove it from the DLQ after the resubmit succeeded
            await dlqReceiver.CompleteMessageAsync(dlqMessage);
            Console.WriteLine($"Resubmitted {dlqMessage.MessageId}");
        }
        else
        {
            // Not the one we want: release it, leave it in the DLQ
            await dlqReceiver.AbandonMessageAsync(dlqMessage);
        }
    }
}
else
{
    ServiceBusReceiver dlqReceiver = client.CreateReceiver(topicName, subscriptionName,
        new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });

    IReadOnlyList<ServiceBusReceivedMessage> messages = await dlqReceiver.PeekMessagesAsync(maxMessages: 10);

    if (messages.Count == 0)
        Console.WriteLine("Dead-letter queue is empty.");

    foreach (var m in messages)
    {
        Console.WriteLine("----");
        Console.WriteLine($"MessageId:   {m.MessageId}");
        Console.WriteLine($"Reason:      {m.DeadLetterReason}");
        Console.WriteLine($"Description: {m.DeadLetterErrorDescription}");
        Console.WriteLine($"Deliveries:  {m.DeliveryCount}");
        Console.WriteLine($"Body:        {m.Body}");
    }
}