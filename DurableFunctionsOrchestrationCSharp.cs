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
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var outputs = new List<string>();

            outputs.Add(await context.WaitForExternalEvent<string>("ReceivedPO"));
            outputs.Add(await context.WaitForExternalEvent<string>("ReceivedHeader"));
            outputs.Add(await context.WaitForExternalEvent<string>("ReceivedOrderDetails"));

            // Replace "hello" with the name of your Durable Activity Function.
            // outputs.Add(await context.CallActivityAsync<string>("DurableFunctionsOrchestrationCSharp_Hello", "Tokyo"));
            // outputs.Add(await context.CallActivityAsync<string>("DurableFunctionsOrchestrationCSharp_Hello", "Seattle"));
            // outputs.Add(await context.CallActivityAsync<string>("DurableFunctionsOrchestrationCSharp_Hello", "London"));
            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            // outputs.AddRange([fileAReceived, fileBReceived, fileCReceived]);  

            await context.CallActivityAsync("Bundle", outputs);

            return outputs;
        }

        [FunctionName("Bundle")]
        public static string Bundle([ActivityTrigger] List<string> names, ILogger log)
        {
            log.LogInformation($"Saying hello to {names}.");
            return $"Hello {names}!";
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


            var bob = await starter.GetStatusAsync("123");
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("DurableFunctionsOrchestrationCSharp", "123");


            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}