using Azure;
using Azure.AI.Projects;
using Azure.Identity;
using MarkAgentService.CommonLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace MarkAgentService.FileUploader
{
    internal class Program
    {

        static AgentsClient? client;
        static async Task Main(string[] args)
        {
            Console.WriteLine("FileUploader");

            AppSettings setx = new();

            client = new AgentsClient(setx.aiProjectConnectionString, new DefaultAzureCredential());

            List<string> fileIds = new List<string>();

 //           fileIds.Add(await DoFileUpload("C:\\Dev\\markagent\\data\\turing\\artificial_intelligence_and_the_turing_test.docx"));
            fileIds.Add(await DoFileUpload("C:\\Dev\\data\\turing\\artificial_intelligence_and_the_turing_test.pdf"));
 //           fileIds.Add(await DoFileUpload("C:\\Dev\\data\\turing\\artificial_intelligence_and_the_turing_test.txt"));
            fileIds.Add(await DoFileUpload("C:\\Dev\\data\\turing\\legacy_of_turing.docx"));
            fileIds.Add(await DoFileUpload("C:\\Dev\\data\\turing\\persecution_and_tragic_end.docx"));
            fileIds.Add(await DoFileUpload("C:\\Dev\\data\\turing\\the_early_life_of_alan_turing.docx"));
            fileIds.Add(await DoFileUpload("C:\\Dev\\data\\turing\\the_turing_machine_and_theoretical_computing.docx"));
            fileIds.Add(await DoFileUpload("C:\\Dev\\data\\turing\\turing_and_the_enigma_code.docx"));
            fileIds.Add(await DoFileUpload("C:\\Dev\\data\\turing\\turing_posthumous_recognition.docx"));


            if (fileIds.Count == 0)
            {
                Console.WriteLine("No files were successfully uploaded. Cannot create vector store.");
                Environment.Exit(-1);
            }

            await DoCreateVectorStore(fileIds);

            Environment.Exit(0);
        }

        static async Task<string> DoFileUpload(string docFilePath)
        {

            if (!File.Exists(docFilePath))
            {
                Console.WriteLine($"File not found: {docFilePath}");
                Environment.Exit(1);
            }

            try
            {
                Console.Write("Uploading ... ");
                Response<AgentFile> uploadAgentFileResponse = await client!.UploadFileAsync(
                                filePath: docFilePath,
                                purpose: AgentFilePurpose.Agents);

                AgentFile uploadedAgentFile = uploadAgentFileResponse.Value;

                // Wait for file processing to complete
                while (true)
                {
                    Response<AgentFile> fileResponse = await client!.GetFileAsync(uploadedAgentFile.Id);
                    AgentFile file = fileResponse.Value;

                    if (file.Status == FileState.Error)
                    {
                        Console.WriteLine("Failed");
                        Console.WriteLine($"Error processing file: {file.StatusDetails}");
                        Environment.Exit(-1);
                    }
                    else if (file.Status == FileState.Processed)
                    {
                        Console.WriteLine("Done");
                        Console.WriteLine($"File Name: {file.Filename}, Id: {file.Id}, Status: {file.Status}, Purpose: {file.Purpose}");
                        Console.WriteLine();
                        break;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(1));
                    Console.Write(".");
                }

                return uploadedAgentFile.Id;

            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Environment.Exit(-1);
            }

            return string.Empty;
        }

        static async Task DoCreateVectorStore(List<string> fileIds)
        {

            try
            {
                Console.Write("Creating Vector Store ... ");
                string storeName = "vector-store-" + DateTime.Now.ToString("hhmm-ddMMMyy");
                // Create a vector store with the file and wait for it to be processed.
                Response<VectorStore> createVectorStoreResponse = await client!.CreateVectorStoreAsync(
                    fileIds: fileIds,
                    name: storeName);

                VectorStore vectorStore = createVectorStoreResponse.Value;

                // Wait for vector store processing to complete
                while (true)
                {
                    Response<VectorStore> vectorStoreResponse = await client!.GetVectorStoreAsync(vectorStore.Id);
                    vectorStore = vectorStoreResponse.Value;

                    if (vectorStore.Status == VectorStoreStatus.Completed)
                    {
                        Console.WriteLine("Done");
                        Console.WriteLine($"Vector Store Name: {vectorStore.Name}, Id: {vectorStore.Id}, Status: {vectorStore.Status}, FileCount: {vectorStore.FileCounts.Completed}");
                        Console.WriteLine();
                        break;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(1));
                    Console.Write(".");
                }
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Environment.Exit(-1);
            }
        }

    }

}

