using System;
using Azure.DigitalTwins.Core;
using Azure.Identity;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using Azure;
using System.Text.Json;

namespace DigitalTwinsCodeTutorial
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string adtInstanceUrl = "https://danielshdt.api.sea.digitaltwins.azure.net";

            var credential = new DefaultAzureCredential();
            var client = new DigitalTwinsClient(new Uri(adtInstanceUrl), credential);
            Console.WriteLine($"Service client created – ready to go");


            //
            //await CreateModelsAsync(client);

            //
            //await CreateDigitalTwins(client);

            //
            //await CreateRelationshipAsync(client, "sampleTwin-0", "sampleTwin-1");
            //await CreateRelationshipAsync(client, "sampleTwin-0", "sampleTwin-2");

            //List the relationships
            //await ListRelationshipsAsync(client, "sampleTwin-1");

            //
            await QueryDigitalTwins(client);
        }


        public async static Task CreateModelsAsync(DigitalTwinsClient client)
        {
            Console.WriteLine($"Upload a model");
            string dtdl = File.ReadAllText("./models/SampleModel.json");
            var models = new List<string> { dtdl };
            // Upload the model to the service
            try
            {
                await client.CreateModelsAsync(models);
                Console.WriteLine("Models uploaded to the instance:");
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine($"Upload model error: {e.Status}: {e.Message}");
            }

            // Read a list of models back from the service
            AsyncPageable<DigitalTwinsModelData> modelDataList = client.GetModelsAsync();
            await foreach (DigitalTwinsModelData md in modelDataList)
            {
                Console.WriteLine($"Model: {md.Id}");
            }
        }

        //create digital twins
        public async static Task CreateDigitalTwins(DigitalTwinsClient client)
        {
            var twinData = new BasicDigitalTwin();
            twinData.Metadata.ModelId = "dtmi:example:SampleModel;1";
            twinData.Contents.Add("data", $"Hello World!");

            string prefix = "sampleTwin-";
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    twinData.Id = $"{prefix}{i}";
                    await client.CreateOrReplaceDigitalTwinAsync<BasicDigitalTwin>(twinData.Id, twinData);
                    Console.WriteLine($"Created twin: {twinData.Id}");
                }
                catch (RequestFailedException e)
                {
                    Console.WriteLine($"Create twin error: {e.Status}: {e.Message}");
                }
            }
        }

        public async static Task CreateRelationshipAsync(DigitalTwinsClient client, string srcId, string targetId)
        {
            var relationship = new BasicRelationship
            {
                TargetId = targetId,
                Name = "contains"
            };

            try
            {
                string relId = $"{srcId}-contains->{targetId}";
                await client.CreateOrReplaceRelationshipAsync(srcId, relId, relationship);
                Console.WriteLine("Created relationship successfully");
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine($"Create relationship error: {e.Status}: {e.Message}");
            }
        }

        public async static Task ListRelationshipsAsync(DigitalTwinsClient client, string srcId)
        {
            try
            {
                AsyncPageable<BasicRelationship> results = client.GetRelationshipsAsync<BasicRelationship>(srcId);
                Console.WriteLine($"Twin {srcId} is connected to:");
                await foreach (BasicRelationship rel in results)
                {
                    Console.WriteLine($" -{rel.Name}->{rel.TargetId}");
                }
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine($"Relationship retrieval error: {e.Status}: {e.Message}");
            }
        }

        public async static Task QueryDigitalTwins(DigitalTwinsClient client)
        {
            // Run a query for all twins
            string query = "SELECT * FROM digitaltwins";
            AsyncPageable<BasicDigitalTwin> queryResult = client.QueryAsync<BasicDigitalTwin>(query);

            await foreach (BasicDigitalTwin twin in queryResult)
            {
                Console.WriteLine(JsonSerializer.Serialize(twin));
                Console.WriteLine("---------------");
            }
        }
    }
}
