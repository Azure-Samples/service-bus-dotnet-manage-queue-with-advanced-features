// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Samples.Common;
using Azure.ResourceManager.ServiceBus;
using Azure.ResourceManager.ServiceBus.Models;

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
        private static ResourceIdentifier? _resourceGroupId = null;
        public static async Task RunSample(ArmClient client)
        {
            try
            {
                //============================================================
             
                // Create a namespace.

                // Get default subscription
                SubscriptionResource subscription = await client.GetDefaultSubscriptionAsync();

                // Create a resource group in the USWest region
                var rgName = Utilities.CreateRandomName("rgSB04_");
                Utilities.Log("Creating resource group with name : " + rgName);
                var rgLro = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, rgName, new ResourceGroupData(AzureLocation.WestUS));
                var resourceGroup = rgLro.Value;
                _resourceGroupId = resourceGroup.Id;
                Utilities.Log("Created resource group with name: " + resourceGroup.Data.Name + "...");

                //create namespace and wait for completion
                var nameSpaceName = Utilities.CreateRandomName("nameSpace");
                Utilities.Log("Creating namespace " + nameSpaceName + " in resource group " + rgName + "...");
                var namespaceCollection = resourceGroup.GetServiceBusNamespaces();
                var data = new ServiceBusNamespaceData(AzureLocation.WestUS)
                {
                    Sku = new ServiceBusSku(ServiceBusSkuName.Standard),
                };
                var serviceBusNamespace = (await namespaceCollection.CreateOrUpdateAsync(WaitUntil.Completed, nameSpaceName, data)).Value;
                Utilities.Log("Created service bus " + serviceBusNamespace.Data.Name);

                //============================================================

                // Add a queue in namespace with features session and dead-lettering.
                var queue1Name = Utilities.CreateRandomName("queue1_");
                Utilities.Log("Creating first queue " + queue1Name + ", with session, time to live and move to dead-letter queue features...");
                var queueCollection = serviceBusNamespace.GetServiceBusQueues();
                var queueData = new ServiceBusQueueData()
                {
                    RequiresSession = true,
                    DefaultMessageTimeToLive = TimeSpan.FromMinutes(10),
                    MaxDeliveryCount = 40,
                };
                var queue = (await queueCollection.CreateOrUpdateAsync(WaitUntil.Completed, queue1Name, queueData)).Value;
                Utilities.Log("Created first queue with name : " + queue.Data.Name);

                //============================================================

                // Create second queue with Deduplication and AutoDeleteOnIdle feature
                var queue2Name = Utilities.CreateRandomName("queue2_");
                Utilities.Log("Creating second queue " + queue2Name + ", with De-duplication and AutoDeleteOnIdle features...");
                var queue2Data = new ServiceBusQueueData()
                {
                    MaxSizeInMegabytes = 2048,
                    DefaultMessageTimeToLive = TimeSpan.FromMinutes(10),
                    AutoDeleteOnIdle = TimeSpan.FromMinutes(10),
                };
                var queue2 = (await queueCollection.CreateOrUpdateAsync(WaitUntil.Completed, queue2Name, queue2Data)).Value;
                Utilities.Log("Created second queue with name : " + queue2.Data.Name);

                //============================================================
                
                // Update second queue to change time for AutoDeleteOnIdle.
                Utilities.Log("Updating second queue to change its auto deletion time");
                var queue2UpdateData = new ServiceBusQueueData()
                {
                    AutoDeleteOnIdle = TimeSpan.FromMinutes(5),
                    MaxSizeInMegabytes = 2048,
                    DefaultMessageTimeToLive = TimeSpan.FromMinutes(10),
                };
                _ = await queue2.UpdateAsync(WaitUntil.Completed, queue2UpdateData);
                Utilities.Log("Updated second queue to change its auto deletion time");

                //=============================================================
               
                // Update first queue to disable dead-letter forwarding and with new Send authorization rule
                Utilities.Log("Update first queue to disable dead-letter forwarding and with new Send authorization rule");
                var queueUpdateData = new ServiceBusQueueData()
                {
                    DeadLetteringOnMessageExpiration = true,
                    RequiresSession = true,
                    DefaultMessageTimeToLive = TimeSpan.FromMinutes(10),
                    MaxDeliveryCount = 40,
                };
                _ = await queue.UpdateAsync(WaitUntil.Completed, queueUpdateData);
                
                // Create sendRule 
                var sendRuleName = Utilities.CreateRandomName("SendRule");
                Utilities.Log("Creating rule : " + sendRuleName + " in queue " + queue.Data.Name + "...");
                var ruleCollection = queue.GetServiceBusQueueAuthorizationRules();
                var sendRuleData = new ServiceBusAuthorizationRuleData()
                {
                    Rights =
                    {
                      ServiceBusAccessRight.Send
                    }
                };
                var sendRule = (await ruleCollection.CreateOrUpdateAsync(WaitUntil.Completed, sendRuleName, sendRuleData)).Value;
                Utilities.Log("Created sendrule ：" + sendRule.Data.Name);
                Utilities.Log("Updated first queue to change dead-letter forwarding");

                //=============================================================
                
                // Get connection string for default authorization rule of namespace
                Utilities.Log("Getting number of authorization rule for namespace...");
                var namespaceAuthorizationRules = serviceBusNamespace.GetServiceBusNamespaceAuthorizationRules().ToList();
                Utilities.Log("Number of authorization rule for namespace :" + namespaceAuthorizationRules.Count());
                
                //=============================================================
                
                // Update first queue to remove Send Authorization rule.
                Utilities.Log("Updating first queue to remove Send Authorization rule...");
                var queues = serviceBusNamespace.GetServiceBusQueues().ToList();
                _ = await queues.First().GetServiceBusQueueAuthorizationRule(sendRuleName).Value.DeleteAsync(WaitUntil.Completed);
                Utilities.Log("Updated first queue to remove Send Authorization rule...");

                //=============================================================

                // Delete a queue and namespace
                Utilities.Log("Deleting queue " + queue1Name + "in namespace " + nameSpaceName + "...");
                _ = await queue.DeleteAsync(WaitUntil.Completed);
                Utilities.Log("Deleted queue " + queue1Name);

                Utilities.Log("Deleting namespace " + nameSpaceName + "...");
                try
                {
                    _ = await serviceBusNamespace.DeleteAsync(WaitUntil.Completed);
                }
                catch (Exception)
                {
                }
                Console.WriteLine("Deleted namespace " + nameSpaceName + "...");
            }
            finally
            {
                try
                {
                    if (_resourceGroupId is not null)
                    {
                        Utilities.Log($"Deleting Resource Group: {_resourceGroupId}");
                        await client.GetResourceGroupResource(_resourceGroupId).DeleteAsync(WaitUntil.Completed);
                        Utilities.Log($"Deleted Resource Group: {_resourceGroupId}");
                    }
                }
                catch (NullReferenceException)
                {
                    Utilities.Log("Did not create any resources in Azure. No clean up is necessary");
                }
                catch (Exception g)
                {
                    Utilities.Log(g);
                }
            }
        }
        public static async Task Main(string[] args)
        {
            try
            {
                var clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
                var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
                var tenantId = Environment.GetEnvironmentVariable("TENANT_ID");
                var subscription = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID");
                ClientSecretCredential credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                ArmClient client = new ArmClient(credential, subscription);
                await RunSample(client);
            }
            catch (Exception e)
            {
                Utilities.Log(e);
            }
        }
    }
}

