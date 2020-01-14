---
page_type: sample
languages:
- csharp
products:
- azure
extensions:
- services: Service-Bus
- platforms: dotnet
---

# Getting started on managing Service Bus Queues with advanced features in C# - sessions, dead-lettering, de-duplication and auto-deletion of idle entries #

 Azure Service Bus basic scenario sample.
 - Create namespace.
 - Add a queue in namespace with features session and dead-lettering.
 - Create another queue with auto-forwarding to first queue. [Remove]
 - Create another queue with dead-letter auto-forwarding to first queue. [Remove]
 - Create second queue with Deduplication and AutoDeleteOnIdle feature
 - Update second queue to change time for AutoDeleteOnIdle.
 - Update first queue to disable dead-letter forwarding and with new Send authorization rule
 - Update queue to remove the Send Authorization rule.
 - Get default authorization rule.
 - Get the keys from authorization rule to connect to queue.
 - Send a "Hello" message to queue using Data plan sdk for Service Bus.
 - Delete queue
 - Delete namespace


## Running this Sample ##

To run this sample:

Set the environment variable `AZURE_AUTH_LOCATION` with the full path for an auth file. See [how to create an auth file](https://github.com/Azure/azure-libraries-for-net/blob/master/AUTH.md).

    git clone https://github.com/Azure-Samples/service-bus-dotnet-manage-queue-with-advanced-features.git

    cd service-bus-dotnet-manage-queue-with-advanced-features

    dotnet build

    bin\Debug\net452\ServiceBusQueueAdvanceFeatures.exe

## More information ##

[Azure Management Libraries for C#](https://github.com/Azure/azure-sdk-for-net/tree/Fluent)
[Azure .Net Developer Center](https://azure.microsoft.com/en-us/develop/net/)
If you don't have a Microsoft Azure subscription you can get a FREE trial account [here](http://go.microsoft.com/fwlink/?LinkId=330212)

---

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.