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
            string adtInstanceUrl = "https://<your-instance-hostname>";

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

            //await UpdateTwinDataAsync(client, "sampleTwin-1-0");

            //
            //await DeleteTwinAsync(client, "sampleTwin-2");
            //
            //await QueryDigitalTwins(client);
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
            twinData.Contents.Add("data", $"Hello World-0706!");

            string prefix = "sampleTwin-1-";
            for (int i = 0; i < 2; i++)
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

        private static async Task DeleteTwinAsync(DigitalTwinsClient client, string twinId)
        {
            await FindAndDeleteOutgoingRelationshipsAsync(client, twinId);
            await FindAndDeleteIncomingRelationshipsAsync(client, twinId);
            try
            {
                await client.DeleteDigitalTwinAsync(twinId);
                Console.WriteLine("Twin deleted successfully");
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"*** Error:{ex.Message}");
            }
        }

        private static async Task FindAndDeleteOutgoingRelationshipsAsync(DigitalTwinsClient client, string dtId)
        {
            // Find the relationships for the twin

            try
            {
                // GetRelationshipsAsync will throw an error if a problem occurs
                AsyncPageable<BasicRelationship> rels = client.GetRelationshipsAsync<BasicRelationship>(dtId);

                await foreach (BasicRelationship rel in rels)
                {
                    await client.DeleteRelationshipAsync(dtId, rel.Id).ConfigureAwait(false);
                    Console.WriteLine($"Deleted relationship {rel.Id} from {dtId}");
                }
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"*** Error {ex.Status}/{ex.ErrorCode} retrieving or deleting relationships for {dtId} due to {ex.Message}");
            }
        }

        private static async Task FindAndDeleteIncomingRelationshipsAsync(DigitalTwinsClient client, string dtId)
        {
            // Find the relationships for the twin

            try
            {
                // GetRelationshipsAsync will throw an error if a problem occurs
                AsyncPageable<IncomingRelationship> incomingRels = client.GetIncomingRelationshipsAsync(dtId);

                await foreach (IncomingRelationship incomingRel in incomingRels)
                {
                    await client.DeleteRelationshipAsync(incomingRel.SourceId, incomingRel.RelationshipId).ConfigureAwait(false);
                    Console.WriteLine($"Deleted incoming relationship {incomingRel.RelationshipId} from {dtId}");
                }
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"*** Error {ex.Status}/{ex.ErrorCode} retrieving or deleting incoming relationships for {dtId} due to {ex.Message}");
            }
        }

        public static async Task UpdateTwinDataAsync(DigitalTwinsClient client, string dtId)
        {
            var updateTwinData = new JsonPatchDocument();
            updateTwinData.AppendAdd("/data", "hello 2021-07-06");
            await client.UpdateDigitalTwinAsync(dtId, updateTwinData);
        }
    }
}
