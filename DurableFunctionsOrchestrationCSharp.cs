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
            List<string> bundle = new List<string>(); 
            bundle.AddRange(something); 
            bundle.Add(prefix); 

            await context.CallActivityAsync("Bundle", bundle);

            return prefix;
        }

        [FunctionName("Bundle")]
        public static string Bundle([ActivityTrigger] string[] fileContent, ILogger log)
        {
            log.LogInformation($"*** \n \n *** Our Three File: ");
            var ohd = fileContent[0]; 
            var oli = fileContent[1]; 
            var pi = fileContent[2];
            var prefix = fileContent[3];  

            log.LogInformation("ohd : " + ohd);  
            log.LogInformation("oli : " + oli);  
            log.LogInformation("pi : " + pi);  
            log.LogInformation("bundle prefix: " + prefix); 


            

            // Get all the files with this prefix: 
            // {prefix}-OrderHeaderDetails.csv
            // {prefix}-OrderLineItems.csv
            // {prefix}-ProductInformation.csv 

            // This function is being called because we have confirmed receipt of all files in the blob for a given prefix. Now grab all files with that prefix.
            Environment.GetEnvironmentVariable("BlobAccountName", EnvironmentVariableTarget.Process);

            var storageCredentials = new StorageCredentials("myAccountName", "myAccountKey");
            
            return $"Hello {ohd}!";
        }

        [FunctionName("BlobTriggerAgain")]
        public static void Run(
            [BlobTrigger("dumbdumbcontainerone/{name}", Connection = "dumbdumbstorage_STORAGE")]TextReader myBlob,
            [OrchestrationClient]DurableOrchestrationClient starter,
            string name,
            ILogger log)
        {
            if(name.Contains("OrderHeaderDetails")){

                // log.LogInformation(myBlob.ReadToEnd()); 
                var csv = new CsvReader(myBlob);
                var records = csv.GetRecords<OrderHeaderDetailModel>();
                

                // log.LogInformation("\n ********RECORD: " + records.ToString()); 
                foreach(var r in records) {
                    log.LogInformation("wee");
                    csv.Read(); 
                    log.LogInformation(r.ponumber.ToString()); 
                }


            }



        }
        [Disable]
        [FunctionName("BlobTriggerCSharp")]
        public static async void Rune(

            [BlobTrigger("dumbdumbcontainerone/{name}", Connection = "dumbdumbstorage_STORAGE")]string myBlob,
            [OrchestrationClient]DurableOrchestrationClient starter,
            string name,
            ILogger log)
        {

            // receive a file: 
            var prefixAndInstanceId = name.Split('-')[0]; 

            var orchestrationFunctionProcessStatus = await starter.GetStatusAsync(prefixAndInstanceId);

            var resultOfStarter = ""; 
            if(orchestrationFunctionProcessStatus is null) {
                log.LogInformation("***** \nCreating a new Durable Orchestration Function with InstanceId equal to: " + prefixAndInstanceId); 
                resultOfStarter = await starter.StartNewAsync("DurableFunctionsOrchestrationCSharp", prefixAndInstanceId, prefixAndInstanceId);
                orchestrationFunctionProcessStatus = await starter.GetStatusAsync(prefixAndInstanceId);
            }


            // if(orchestrationFunctionProcessStatus.InstanceId == prefixAndInstanceId) {
            //     log.LogInformation("knocked out of the park \n prefixAndInstanceId: " + prefixAndInstanceId + " orchestrationFunctionProcessStatus.InstanceId: " + orchestrationFunctionProcessStatus.InstanceId);
            // }


            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob} Bytes");
            // log.LogInformation("INSTANCE ID...: " + prefixAndInstanceId);

            var stuffInBlob = myBlob; 

            if (name.Contains("OrderHeaderDetails"))
            {
//                 await starter.RaiseEventAsync(prefixAndInstanceId, "OrderHeaderDetails", name);

                // List<OrderHeaderDetailModel> details = File.ReadAllLines(myBlob).Skip(1).Select(v => OrderHeaderDetailModel.FromCsv(v)).ToList(); 
                log.LogInformation("OrderHeaderDetails going to orchestrator: " + name);
                await starter.RaiseEventAsync(prefixAndInstanceId, "OrderHeaderDetails", myBlob);
            }
            else if (name.Contains("OrderLineItems"))
            {
                log.LogInformation("OrderLineItems going to orchestrator: " + name);
                await starter.RaiseEventAsync(prefixAndInstanceId, "OrderLineItems", myBlob);
            }
            else if (name.Contains("ProductInformation"))
            {
                log.LogInformation("ProductInformation going to orchestrator: " + name);
                await starter.RaiseEventAsync(prefixAndInstanceId, "ProductInformation", myBlob);
            }


            // log.LogInformation($"Started orchestration with ID = '{prefixAndInstanceId}'.");
        }

        
    }
}