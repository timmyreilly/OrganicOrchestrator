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

            await Task.WhenAll(content1, content2, content3);


            // Replace "hello" with the name of your Durable Activity Function.
            // outputs.Add(await context.CallActivityAsync<string>("DurableFunctionsOrchestrationCSharp_Hello", "Tokyo"));
            // outputs.Add(await context.CallActivityAsync<string>("DurableFunctionsOrchestrationCSharp_Hello", "Seattle"));
            // outputs.Add(await context.CallActivityAsync<string>("DurableFunctionsOrchestrationCSharp_Hello", "London"));
            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            // outputs.AddRange([fileAReceived, fileBReceived, fileCReceived]);  

            await context.CallActivityAsync("Bundle", prefix);

            return prefix;
        }

        [FunctionName("Bundle")]
        public static string Bundle([ActivityTrigger] string prefix, ILogger log)
        {
            log.LogInformation($"Saying hello to {prefix}.");
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
            [BlobTrigger("dumbdumbcontainerone/{name}", Connection = "dumbdumbstorage_STORAGE")]Stream myBlob,
            [OrchestrationClient]DurableOrchestrationClient starter,
            string name,
            ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
            var prefix = name.Split('-')[0];
            var fileName = name.Split('-')[1];
            log.LogInformation("PREFIX: " + prefix);

            var orchestratorProcessStatus = await starter.GetStatusAsync(prefix);
            if (orchestratorProcessStatus != null)
            {
                log.LogInformation(" THERE IS A THING IN PROGRESS: PROCESS STATUS: " + orchestratorProcessStatus.ToString());
            } else {
                log.LogInformation("THERE IS NOTHING IN PROCESS: THIS IS WHAT WE HAVE FOR PROCESS STATUS: "); 
            }

            // Function input comes from the request content.
            string instanceId = null;
            if (orchestratorProcessStatus is null)
            {
                instanceId = await starter.StartNewAsync("DurableFunctionsOrchestrationCSharp", prefix, prefix);
                log.LogInformation("INSTANCE ID: " + instanceId);
            }
            else
            {
                instanceId = prefix;
            }
            log.LogInformation("INSTANCE ID...: " + instanceId);



            if (name.Contains("OrderHeaderDetail"))
            {
                log.LogInformation("OrderHeaderDetail going to orchestrator: " + name);
                await starter.RaiseEventAsync(instanceId, "OrderHeaderDetail", name);
            }
            else if (name.Contains("OrderLineItems"))
            {
                log.LogInformation("OrderLineItems going to orchestrator: " + name);
                await starter.RaiseEventAsync(instanceId, "OrderLineItems", name);
            }
            else if (name.Contains("ProductInformation"))
            {
                log.LogInformation("ProductInformation going to orchestrator: " + name);
                await starter.RaiseEventAsync(instanceId, "ProductInformation", name);
            }


            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        }
    }
}