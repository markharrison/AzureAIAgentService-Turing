using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MarkAgentService.CommonLib;
using Azure.Identity;
using Azure.AI.Projects;
using Azure;

// Create an Azure AI Client from a connection string, copied from your Azure AI Foundry project.
// At the moment, it should be in the format "<HostName>;<AzureSubscriptionId>;<ResourceGroup>;<ProjectName>"
// Customer needs to login to Azure subscription via Azure CLI and set the environment variables

namespace MarkAgentService.ChatAgent
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("ChatAgent");

            AppSettings setx = new();

            AgentsClient client = new AgentsClient(setx.aiProjectConnectionString, new DefaultAzureCredential());

            FileSearchToolResource fileSearchToolResource = new FileSearchToolResource();
            fileSearchToolResource.VectorStoreIds.Add(setx.vectorStoreId);

            Response<Agent> agentResponse = await client.CreateAgentAsync(
                    model: "gpt-4o-mini",
                    name: "Mark Q&A Agent",
                    instructions: "You are a helpful agent that use info from files to help answer questions.",
                    tools: new List<ToolDefinition> { new FileSearchToolDefinition() },
                    toolResources: new ToolResources() { FileSearch = fileSearchToolResource });
            Agent agent = agentResponse.Value;

            Response<AgentThread> threadResponse = await client.CreateThreadAsync();
            AgentThread thread = threadResponse.Value;

            while (true)
            {
                Console.Write("Prompt: ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    break;
                }

                Response<ThreadMessage> messageResponse = await client.CreateMessageAsync(thread.Id, MessageRole.User, input);
                ThreadMessage message = messageResponse.Value;

                Response<ThreadRun> runResponse = await client.CreateRunAsync(thread, agent);

                do
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                    runResponse = await client.GetRunAsync(thread.Id, runResponse.Value.Id);
                }
                while (runResponse.Value.Status == RunStatus.Queued
                    || runResponse.Value.Status == RunStatus.InProgress) ;


                Response<PageableList<ThreadMessage>> afterRunMessagesResponse
                    = await client.GetMessagesAsync(thread.Id);
                IReadOnlyList<ThreadMessage> messages = afterRunMessagesResponse.Value.Data;

                foreach (ThreadMessage threadMessage in messages)
                {
                    Console.Write($"{threadMessage.CreatedAt:yyyy-MM-dd HH:mm:ss} - {threadMessage.Role,10}: ");
                    foreach (MessageContent contentItem in threadMessage.ContentItems)
                    {
                        if (contentItem is MessageTextContent textItem)
                        {
                            Console.Write(textItem.Text);
                        }
                        else if (contentItem is MessageImageFileContent imageFileItem)
                        {
                            Console.Write($"<image from ID: {imageFileItem.FileId}");
                        }
                        Console.WriteLine();
                    }
                }
            }

            await client.DeleteThreadAsync(thread.Id);
            await client.DeleteAgentAsync(agent.Id);

        }
    }
}
