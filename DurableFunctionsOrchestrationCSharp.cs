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

            var content1 = context.WaitForExternalEvent<TextReader>("OrderHeaderDetails");
            var content2 = context.WaitForExternalEvent<TextReader>("OrderLineItems");
            var content3 = context.WaitForExternalEvent<TextReader>("ProductInformation");

            var something = await Task.WhenAll(content1, content2, content3);

            
            await context.CallActivityAsync("Bundle", something);

            return prefix;
        }

        [FunctionName("Bundle")]
        public static string Bundle([ActivityTrigger] TextReader[] fileContent, ILogger log)
        {
            log.LogInformation($"*** \n \n *** Our Three File: ");
            foreach(var r  in fileContent) {
                log.LogInformation(r.ToString()); 
            }



            var ohdcsv = new CsvReader(fileContent[0]);
            var ohd = ohdcsv.GetRecords<OrderHeaderDetailModel>(); 
            foreach(var r in ohd) {
                log.LogInformation(r.ponumber); 
            }

            // var ohd = fileContent[0];
            // var oli = fileContent[1];
            // var pi = fileContent[2];
            // // var prefix = fileContent[3];
            
            

            // log.LogInformation("ohd : " + ohd);
            // log.LogInformation("oli : " + oli);
            // log.LogInformation("pi : " + pi);
            // log.LogInformation("bundle prefix: " + prefix);




            // Get all the files with this prefix: 
            // {prefix}-OrderHeaderDetails.csv
            // {prefix}-OrderLineItems.csv
            // {prefix}-ProductInformation.csv 

            // This function is being called because we have confirmed receipt of all files in the blob for a given prefix. Now grab all files with that prefix.
            Environment.GetEnvironmentVariable("BlobAccountName", EnvironmentVariableTarget.Process);

            var storageCredentials = new StorageCredentials("myAccountName", "myAccountKey");

            return $"Hello {ohd}!";
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


            if (name.Contains("OrderHeaderDetails"))
            {
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