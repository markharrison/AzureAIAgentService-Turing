# AzureAIAgentService-Turing

Example of using Azure AI Agent Service .

Questions & Answers using uploaded documents. 

Some document files about Alan Turing are included.

## Projects included:

### FileUploader

Upload data files 
Create vector store with uploaded data

### StoreExplorer

List all the vectors stores 
Optionally delete the vectors stores.

### Chat Agent

Q&A - ask about the uploaded data.

(remember to configure the Vector Store Id in appsettings.config)

### Common Library

App Settings functionality used by all the projects.

## Configuration

| Key                      | Value     |  
|--------------------------|-----------| 
| AIProjectConnectionString | Project connection string from AI Foundry | 
| VectorStoreId | Vector Store id that contains the data file(s) | 


 