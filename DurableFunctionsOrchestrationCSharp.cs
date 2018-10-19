using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CsvHelper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Auth;
using Newtonsoft.Json;
using Microsoft.Azure.Documents;


namespace Company.Function
{
    public static class DurableFunctionsOrchestrationCSharp
    {
        [FunctionName("DurableFunctionsOrchestrationCSharp")]
        public static async Task<string> RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext context)
        {
            string prefix = context.GetInput<string>();

            var content1 = context.WaitForExternalEvent<string>("OrderHeaderDetails");
            var content2 = context.WaitForExternalEvent<string>("OrderLineItems");
            var content3 = context.WaitForExternalEvent<string>("ProductInformation");

            var something = await Task.WhenAll(content1, content2, content3);

            var bundle = something.ToList();
            bundle.Add(prefix);

            await context.CallActivityAsync("Bundle", bundle);

            return prefix;
        }

        [FunctionName("Bundle")]
        public static async Task<string> Bundle([ActivityTrigger] List<string> fileContent,   
        [CosmosDB(
            databaseName: "Challenge7",
            collectionName: "OrderDetails",
            ConnectionStringSetting = "CosmosDBConnection",
            CreateIfNotExists = true
            
            )]
            IAsyncCollector<CosmosEntry> documentsToStore,
            ILogger log)
        {
            log.LogInformation($"*** \n \n *** Our Three File: ");

            List<OrderHeaderDetailModel> ohd = JsonConvert.DeserializeObject<List<OrderHeaderDetailModel>>(fileContent[0]);
            List<OrderLineItemModel> oli = JsonConvert.DeserializeObject<List<OrderLineItemModel>>(fileContent[1]);
            List<ProductInformationModel> pi = JsonConvert.DeserializeObject<List<ProductInformationModel>>(fileContent[2]);

            var prefix = fileContent[3];

            foreach (var entry in ohd)
            {
                CosmosEntry cosmosEntry = new CosmosEntry();
                cosmosEntry.prefix = prefix;
                cosmosEntry.ponumber = entry.ponumber;
                cosmosEntry.locationid = entry.locationid;
                cosmosEntry.locationname = entry.locationname;
                cosmosEntry.locationaddress = entry.locationaddress;
                cosmosEntry.locationpostcode = entry.locationpostcode;
                cosmosEntry.totalcost = entry.totalcost;
                cosmosEntry.totaltax = entry.totaltax;

                cosmosEntry.orderitemlist = new List<OrderItem>();
                var lineItem = oli.Where(x => x.ponumber == cosmosEntry.ponumber);
                foreach (var l in lineItem)
                {
                    var details = pi.Where(x => x.productid == l.productid).First();

                    var item = new OrderItem();
                    item.ponumber = l.ponumber; 
                    item.productid = l.productid;
                    item.productname = details.productname;
                    item.quantity = l.quantity;
                    item.unitcost = l.unitcost;
                    item.totalcost = l.totalcost;
                    item.totaltax = l.totaltax;
                    item.productdescription = details.productdescription;

                    cosmosEntry.orderitemlist.Add(item);
                }
                log.LogInformation(cosmosEntry.ToString());
                await documentsToStore.AddAsync(cosmosEntry); 
                // add cosmosEntry to cosmos

            }



            // Get all the files with this prefix: 
            // {prefix}-OrderHeaderDetails.csv
            // {prefix}-OrderLineItems.csv
            // {prefix}-ProductInformation.csv 

            // This function is being called because we have confirmed receipt of all files in the blob for a given prefix. Now grab all files with that prefix.
            // Environment.GetEnvironmentVariable("BlobAccountName", EnvironmentVariableTarget.Process);

            // var storageCredentials = new StorageCredentials("myAccountName", "myAccountKey");

            return $"Hello {fileContent[3]}!";
        }

        [Disable]
        [FunctionName("BlobTriggerAgain")]
        public static void Run(
            [BlobTrigger("dumbdumbcontainerone/{name}", Connection = "dumbdumbstorage_STORAGE")]TextReader myBlob,
            [OrchestrationClient]DurableOrchestrationClient starter,
            string name,
            ILogger log)
        {
            var csv = new CsvReader(myBlob);
            if (name.Contains("OrderHeaderDetails"))
            {
                var ohd = csv.GetRecords<OrderHeaderDetailModel>();

                foreach (var r in ohd)
                {
                    csv.Read();
                    log.LogInformation(r.ponumber.ToString());
                }
            }
            else if (name.Contains("OrderLineItems"))
            {
                var oli = csv.GetRecords<OrderLineItemModel>();

                foreach (var r in oli)
                {
                    csv.Read();
                    log.LogInformation(r.ponumber.ToString());

                }
            }
            else if (name.Contains("ProductInformation"))
            {
                var pi = csv.GetRecords<ProductInformationModel>();

                foreach (var r in pi)
                {
                    csv.Read();
                    log.LogInformation(r.productid.ToString());
                }
            }



        }

        [FunctionName("BlobTriggerCSharp")]
        public static async void Rune(

            [BlobTrigger("dumbdumbcontainerone/{name}", Connection = "dumbdumbstorage_STORAGE")]TextReader myBlob,
            [OrchestrationClient]DurableOrchestrationClient starter,
            string name,
            ILogger log)
        {

            // receive a file: 
            var prefixAndInstanceId = name.Split('-')[0];

            var orchestrationFunctionProcessStatus = await starter.GetStatusAsync(prefixAndInstanceId);

            var resultOfStarter = "";
            if (orchestrationFunctionProcessStatus is null)
            {
                log.LogInformation("***** \nCreating a new Durable Orchestration Function with InstanceId equal to: " + prefixAndInstanceId);
                resultOfStarter = await starter.StartNewAsync("DurableFunctionsOrchestrationCSharp", prefixAndInstanceId, prefixAndInstanceId);
                orchestrationFunctionProcessStatus = await starter.GetStatusAsync(prefixAndInstanceId);
            }


            // if(orchestrationFunctionProcessStatus.InstanceId == prefixAndInstanceId) {
            //     log.LogInformation("knocked out of the park \n prefixAndInstanceId: " + prefixAndInstanceId + " orchestrationFunctionProcessStatus.InstanceId: " + orchestrationFunctionProcessStatus.InstanceId);
            // }

            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n ");
            // log.LogInformation("INSTANCE ID...: " + prefixAndInstanceId);

            var csv = new CsvReader(myBlob);

            if (name.Contains("OrderHeaderDetails"))
            {
                var ohd = csv.GetRecords<OrderHeaderDetailModel>();
                string json = JsonConvert.SerializeObject(ohd);
                log.LogInformation("OrderHeaderDetails going to orchestrator: " + name);
                await starter.RaiseEventAsync(prefixAndInstanceId, "OrderHeaderDetails", json);
            }
            else if (name.Contains("OrderLineItems"))
            {
                var oli = csv.GetRecords<OrderLineItemModel>();
                string json = JsonConvert.SerializeObject(oli);
                log.LogInformation("OrderLineItems going to orchestrator: " + name);
                await starter.RaiseEventAsync(prefixAndInstanceId, "OrderLineItems", json);
            }
            else if (name.Contains("ProductInformation"))
            {
                var pi = csv.GetRecords<ProductInformationModel>();
                string json = JsonConvert.SerializeObject(pi);
                log.LogInformation("ProductInformation going to orchestrator: " + name);
                await starter.RaiseEventAsync(prefixAndInstanceId, "ProductInformation", json);
            }


            // log.LogInformation($"Started orchestration with ID = '{prefixAndInstanceId}'.");
        }


    }
}