using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MarkAgentService.CommonLib;
using Azure.Identity;
using Azure.AI.Projects;
using Azure;


namespace MarkAgentService.StoreExplorer
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.Clear();
            Console.WriteLine("StoreExplorer\n");

            AppSettings setx = new();
            AgentsClient client = new AgentsClient(setx.aiProjectConnectionString, new DefaultAzureCredential());

            try
            {
                var vectorStores = await client.GetVectorStoresAsync();
                foreach (var store in vectorStores.Value.Data)
                {
                    Console.WriteLine($"Vector Store Id: {store.Id}, Name: {store.Name}, Status {store.Status}, FileCount: {store.FileCounts.Completed}");
                }

                Console.Write("\nEnter D to delete all: ");
                var input = Console.ReadLine();
                if (input != null && input.Equals("d", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var store in vectorStores.Value.Data)
                    {
                        client.DeleteVectorStoreAsync(store.Id).GetAwaiter().GetResult();
                        Console.WriteLine($"Vector Store {store.Id} deleted.");
                    }
                }
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Environment.Exit(-1);
            }

            Environment.Exit(0);
        }
    }
}
