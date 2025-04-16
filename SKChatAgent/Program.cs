using Azure;
using Azure.AI.Projects;
using Azure.Identity;
using MarkAgentService.CommonLib;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

// Create an Azure AI Client from a connection string, copied from your Azure AI Foundry project.
// At the moment, it should be in the format "<HostName>;<AzureSubscriptionId>;<ResourceGroup>;<ProjectName>"
// Customer needs to login to Azure subscription via Azure CLI and set the environment variables

#pragma warning disable SKEXP0110

namespace MarkAgentService.SKChatAgent
{
    internal class Program
    {


        static async Task Main(string[] args)
        {
            Console.WriteLine("SKChatAgent");

            AppSettings setx = new();

            AgentsClient client = new AgentsClient(setx.aiProjectConnectionString, new DefaultAzureCredential());

            FileSearchToolResource fileSearchToolResource = new FileSearchToolResource();
            fileSearchToolResource.VectorStoreIds.Add(setx.vectorStoreId);

            Response<Azure.AI.Projects.Agent> agentResponse = await client.CreateAgentAsync(
                    model: "gpt-4o-mini",
                    name: "Mark Q&A Agent",
                    instructions: "You are a helpful agent that use info from files to help answer questions.",
                    tools: new List<ToolDefinition> { new FileSearchToolDefinition() },
                    toolResources: new ToolResources() { FileSearch = fileSearchToolResource });
            Azure.AI.Projects.Agent agent = agentResponse.Value;

            //Response<AgentThread> threadResponse = await client.CreateThreadAsync();
            //AgentThread thread = threadResponse.Value;


            // SK
            AzureAIAgent aiagent = new(agent, client);
            Microsoft.SemanticKernel.Agents.AgentThread aiagentThread = new AzureAIAgentThread(aiagent.Client);

            Dictionary<string, AgentFile> agentFileCache = new();

            while (true)
            {
                Console.Write("Prompt: ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    break;
                }

                ChatMessageContent message = new(AuthorRole.User, input);

                List<AnnotationContent> footnotes = [];

                await foreach (ChatMessageContent response in aiagent.InvokeAsync(message, aiagentThread))
                {
                    footnotes.AddRange(response.Items.OfType<AnnotationContent>());

                    Console.WriteLine(response.Content);

                }

                foreach (AnnotationContent footnote in footnotes)
                {
                    // Check if the FileId is already in the cache
                    if (!agentFileCache.TryGetValue(footnote.FileId!, out AgentFile? agentFile))
                    {
                        // If not in cache, make the API call and store the result in the cache
                        agentFile = await aiagent.Client.GetFileAsync(footnote.FileId);
                        agentFileCache[footnote.FileId!] = agentFile;
                    }

                    Console.WriteLine($"#{footnote.Quote.Replace("【", "[").Replace("†source】", "]")} - {footnote.FileId} - {agentFile.Filename} (Index: {footnote.StartIndex} - {footnote.EndIndex})");
                }

            }

            try
            {
                await aiagentThread.DeleteAsync();
            }
            catch { };

            await aiagent.Client.DeleteAgentAsync(agent.Id);

        }
    }
}
