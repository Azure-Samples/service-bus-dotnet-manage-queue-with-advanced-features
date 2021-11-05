// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


using System;
using System.Linq;
using System.Text;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ServiceBus.Fluent;

namespace ServiceBusQueueAdvanceFeatures
{
    public class Program
    {
        /**
         * Azure Service Bus basic scenario sample.
         * - Create namespace.
         * - Add a queue in namespace with features session and dead-lettering.
         * - Create another queue with auto-forwarding to first queue. [Remove]
         * - Create another queue with dead-letter auto-forwarding to first queue. [Remove]
         * - Create second queue with Deduplication and AutoDeleteOnIdle feature
         * - Update second queue to change time for AutoDeleteOnIdle.
         * - Update first queue to disable dead-letter forwarding and with new Send authorization rule
         * - Update queue to remove the Send Authorization rule.
         * - Get default authorization rule.
         * - Get the keys from authorization rule to connect to queue.
         * - Delete queue
         * - Delete namespace
         */
        public static void RunSample(IAzure azure)
        {
            var rgName = SdkContext.RandomResourceName("rgSB04_", 24);
            var namespaceName = SdkContext.RandomResourceName("namespace", 20);
            var queue1Name = SdkContext.RandomResourceName("queue1_", 24);
            var queue2Name = SdkContext.RandomResourceName("queue2_", 24);
            var sendRuleName = "SendRule";

            try
            {
                //============================================================
                // Create a namespace.

                Console.WriteLine("Creating name space " + namespaceName + " in resource group " + rgName + "...");

                var serviceBusNamespace = azure.ServiceBusNamespaces
                        .Define(namespaceName)
                        .WithRegion(Region.USWest)
                        .WithNewResourceGroup(rgName)
                        .WithSku(NamespaceSku.Standard)
                        .Create();

                Console.WriteLine("Created service bus " + serviceBusNamespace.Name);
                PrintServiceBusNamespace(serviceBusNamespace);

                //============================================================
                // Add a queue in namespace with features session and dead-lettering.
                Console.WriteLine("Creating first queue " + queue1Name + ", with session, time to live and move to dead-letter queue features...");

                var firstQueue = serviceBusNamespace.Queues.Define(queue1Name)
                        .WithSession()
                        .WithDefaultMessageTTL(TimeSpan.FromMinutes(10))
                        .WithExpiredMessageMovedToDeadLetterQueue()
                        .WithMessageMovedToDeadLetterQueueOnMaxDeliveryCount(40)
                        .Create();
                PrintQueue(firstQueue);

                //============================================================
                // Create second queue with Deduplication and AutoDeleteOnIdle feature

                Console.WriteLine("Creating second queue " + queue2Name + ", with De-duplication and AutoDeleteOnIdle features...");

                var secondQueue = serviceBusNamespace.Queues.Define(queue2Name)
                        .WithSizeInMB(2048)
                        .WithDuplicateMessageDetection(TimeSpan.FromMinutes(10))
                        .WithDeleteOnIdleDurationInMinutes(10)
                        .Create();

                Console.WriteLine("Created second queue in namespace");

                PrintQueue(secondQueue);

                //============================================================
                // Update second queue to change time for AutoDeleteOnIdle.

                secondQueue = secondQueue.Update()
                        .WithDeleteOnIdleDurationInMinutes(5)
                        .Apply();

                Console.WriteLine("Updated second queue to change its auto deletion time");

                PrintQueue(secondQueue);

                //=============================================================
                // Update first queue to disable dead-letter forwarding and with new Send authorization rule
                secondQueue = firstQueue.Update()
                        .WithoutExpiredMessageMovedToDeadLetterQueue()
                        .WithNewSendRule(sendRuleName)
                        .Apply();

                Console.WriteLine("Updated first queue to change dead-letter forwarding");

                PrintQueue(secondQueue);

                //=============================================================
                // Get connection string for default authorization rule of namespace

                var namespaceAuthorizationRules = serviceBusNamespace.AuthorizationRules.List();
                Console.WriteLine("Number of authorization rule for namespace :" + namespaceAuthorizationRules.Count());


                foreach (var namespaceAuthorizationRule in namespaceAuthorizationRules)
                {
                    PrintNamespaceAuthorizationRule(namespaceAuthorizationRule);
                }

                Console.WriteLine("Getting keys for authorization rule ...");

                var keys = namespaceAuthorizationRules.FirstOrDefault().GetKeys();
                PrintKeys(keys);

                //=============================================================
                // Update first queue to remove Send Authorization rule.
                firstQueue.Update().WithoutAuthorizationRule(sendRuleName).Apply();


                //=============================================================
                // Delete a queue and namespace
                Console.WriteLine("Deleting queue " + queue1Name + "in namespace " + namespaceName + "...");
                serviceBusNamespace.Queues.DeleteByName(queue1Name);
                Console.WriteLine("Deleted queue " + queue1Name + "...");

                Console.WriteLine("Deleting namespace " + namespaceName + "...");
                // This will delete the namespace and queue within it.
                try
                {
                    azure.ServiceBusNamespaces.DeleteById(serviceBusNamespace.Id);
                }
                catch (Exception)
                {
                }
                Console.WriteLine("Deleted namespace " + namespaceName + "...");

            }
            finally
            {
                try
                {
                    Console.WriteLine("Deleting Resource Group: " + rgName);
                    azure.ResourceGroups.BeginDeleteByName(rgName);
                    Console.WriteLine("Deleted Resource Group: " + rgName);
                }
                catch (NullReferenceException)
                {
                    Console.WriteLine("Did not create any resources in Azure. No clean up is necessary");
                }
                catch (Exception g)
                {
                    Console.WriteLine(g);
                }
            }
        }

        public static void Main(string[] args)
        {
            try
            {
                //=================================================================
                // Authenticate
                var credentials = SdkContext.AzureCredentialsFactory.FromFile(Environment.GetEnvironmentVariable("AZURE_AUTH_LOCATION"));

                var azure = Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(credentials)
                    .WithDefaultSubscription();

                // Print selected subscription
                Console.WriteLine("Selected subscription: " + azure.SubscriptionId);

                RunSample(azure);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        static void PrintServiceBusNamespace(IServiceBusNamespace serviceBusNamespace)
        {
            var builder = new StringBuilder()
                    .Append("Service bus Namespace: ").Append(serviceBusNamespace.Id)
                    .Append("\n\tName: ").Append(serviceBusNamespace.Name)
                    .Append("\n\tRegion: ").Append(serviceBusNamespace.RegionName)
                    .Append("\n\tResourceGroupName: ").Append(serviceBusNamespace.ResourceGroupName)
                    .Append("\n\tCreatedAt: ").Append(serviceBusNamespace.CreatedAt)
                    .Append("\n\tUpdatedAt: ").Append(serviceBusNamespace.UpdatedAt)
                    .Append("\n\tDnsLabel: ").Append(serviceBusNamespace.DnsLabel)
                    .Append("\n\tFQDN: ").Append(serviceBusNamespace.Fqdn)
                    .Append("\n\tSku: ")
                    .Append("\n\t\tCapacity: ").Append(serviceBusNamespace.Sku.Capacity)
                    .Append("\n\t\tSkuName: ").Append(serviceBusNamespace.Sku.Name)
                    .Append("\n\t\tTier: ").Append(serviceBusNamespace.Sku.Tier);

            Console.WriteLine(builder.ToString());
        }

        static void PrintQueue(IQueue queue)
        {
            StringBuilder builder = new StringBuilder()
                    .Append("Service bus Queue: ").Append(queue.Id)
                    .Append("\n\tName: ").Append(queue.Name)
                    .Append("\n\tResourceGroupName: ").Append(queue.ResourceGroupName)
                    .Append("\n\tCreatedAt: ").Append(queue.CreatedAt)
                    .Append("\n\tUpdatedAt: ").Append(queue.UpdatedAt)
                    .Append("\n\tAccessedAt: ").Append(queue.AccessedAt)
                    .Append("\n\tActiveMessageCount: ").Append(queue.ActiveMessageCount)
                    .Append("\n\tCurrentSizeInBytes: ").Append(queue.CurrentSizeInBytes)
                    .Append("\n\tDeadLetterMessageCount: ").Append(queue.DeadLetterMessageCount)
                    .Append("\n\tDefaultMessageTtlDuration: ").Append(queue.DefaultMessageTtlDuration)
                    .Append("\n\tDuplicateMessageDetectionHistoryDuration: ").Append(queue.DuplicateMessageDetectionHistoryDuration)
                    .Append("\n\tIsBatchedOperationsEnabled: ").Append(queue.IsBatchedOperationsEnabled)
                    .Append("\n\tIsDeadLetteringEnabledForExpiredMessages: ").Append(queue.IsDeadLetteringEnabledForExpiredMessages)
                    .Append("\n\tIsDuplicateDetectionEnabled: ").Append(queue.IsDuplicateDetectionEnabled)
                    .Append("\n\tIsExpressEnabled: ").Append(queue.IsExpressEnabled)
                    .Append("\n\tIsPartitioningEnabled: ").Append(queue.IsPartitioningEnabled)
                    .Append("\n\tIsSessionEnabled: ").Append(queue.IsSessionEnabled)
                    .Append("\n\tDeleteOnIdleDurationInMinutes: ").Append(queue.DeleteOnIdleDurationInMinutes)
                    .Append("\n\tMaxDeliveryCountBeforeDeadLetteringMessage: ").Append(queue.MaxDeliveryCountBeforeDeadLetteringMessage)
                    .Append("\n\tMaxSizeInMB: ").Append(queue.MaxSizeInMB)
                    .Append("\n\tMessageCount: ").Append(queue.MessageCount)
                    .Append("\n\tScheduledMessageCount: ").Append(queue.ScheduledMessageCount)
                    .Append("\n\tStatus: ").Append(queue.Status)
                    .Append("\n\tTransferMessageCount: ").Append(queue.TransferMessageCount)
                    .Append("\n\tLockDurationInSeconds: ").Append(queue.LockDurationInSeconds)
                    .Append("\n\tTransferDeadLetterMessageCount: ").Append(queue.TransferDeadLetterMessageCount);

            Console.WriteLine(builder.ToString());
        }

        static void PrintNamespaceAuthorizationRule(INamespaceAuthorizationRule namespaceAuthorizationRule)
        {
            StringBuilder builder = new StringBuilder()
                    .Append("Service bus queue authorization rule: ").Append(namespaceAuthorizationRule.Id)
                    .Append("\n\tName: ").Append(namespaceAuthorizationRule.Name)
                    .Append("\n\tResourceGroupName: ").Append(namespaceAuthorizationRule.ResourceGroupName)
                    .Append("\n\tNamespace Name: ").Append(namespaceAuthorizationRule.NamespaceName);

            var rights = namespaceAuthorizationRule.Rights;
            builder.Append("\n\tNumber of access rights in queue: ").Append(rights.Count());
            foreach (var right in rights)
            {
                builder.Append("\n\t\tAccessRight: ")
                        .Append("\n\t\t\tName :").Append(right.ToString());
            }

            Console.WriteLine(builder.ToString());
        }

        static void PrintKeys(IAuthorizationKeys keys)
        {
            StringBuilder builder = new StringBuilder()
                    .Append("Authorization keys: ")
                    .Append("\n\tPrimaryKey: ").Append(keys.PrimaryKey)
                    .Append("\n\tPrimaryConnectionString: ").Append(keys.PrimaryConnectionString)
                    .Append("\n\tSecondaryKey: ").Append(keys.SecondaryKey)
                    .Append("\n\tSecondaryConnectionString: ").Append(keys.SecondaryConnectionString);

            Console.WriteLine(builder.ToString());
        }
    }
}
