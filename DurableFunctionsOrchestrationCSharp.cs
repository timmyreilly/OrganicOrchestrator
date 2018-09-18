using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
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


            // Replace "hello" with the name of your Durable Activity Function.
            // outputs.Add(await context.CallActivityAsync<string>("DurableFunctionsOrchestrationCSharp_Hello", "Tokyo"));
            // outputs.Add(await context.CallActivityAsync<string>("DurableFunctionsOrchestrationCSharp_Hello", "Seattle"));
            // outputs.Add(await context.CallActivityAsync<string>("DurableFunctionsOrchestrationCSharp_Hello", "London"));
            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            // outputs.AddRange([fileAReceived, fileBReceived, fileCReceived]);  

            await context.CallActivityAsync("Bundle", something);

            return prefix;
        }

        [FunctionName("Bundle")]
        public static string Bundle([ActivityTrigger] string prefix, ILogger log)
        {
            log.LogInformation($"*** \n \n *** Saying hello to {prefix}.");
            return $"Hello {prefix}!";
        }

        [FunctionName("DurableFunctionsOrchestrationCSharp_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClient starter,
            ILogger log)
        {
            // dynamic data = JsonConvert.DeserializeObject(req);

            var queryValues = req.RequestUri.ParseQueryString();
            var prefix = queryValues["prefix"];
            log.LogInformation(prefix);


            var orchestratorProcessStatus = await starter.GetStatusAsync(prefix);
            // Function input comes from the request content.
            string instanceId = null;
            if (orchestratorProcessStatus is null)
            {
                instanceId = await starter.StartNewAsync("DurableFunctionsOrchestrationCSharp", prefix);
            }
            else
            {
                instanceId = prefix;
            }

            await starter.RaiseEventAsync(instanceId, "ReceivedHeader", prefix);
            await starter.RaiseEventAsync(instanceId, "ReceivedOrderDetails", prefix);
            await starter.RaiseEventAsync(instanceId, "ReceivedPO", prefix);


            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("BlobTriggerCSharp")]
        public static async void Run(
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


            if(orchestrationFunctionProcessStatus.InstanceId == prefixAndInstanceId) {
                log.LogInformation("knocked out of the park \n prefixAndInstanceId: " + prefixAndInstanceId + " orchestrationFunctionProcessStatus.InstanceId: " + orchestrationFunctionProcessStatus.InstanceId);
            }


            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob} Bytes");
            log.LogInformation("INSTANCE ID...: " + prefixAndInstanceId);

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