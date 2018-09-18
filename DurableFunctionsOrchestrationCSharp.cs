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

            var content1 = context.WaitForExternalEvent<string>("ReceivedPO");
            var content2 = context.WaitForExternalEvent<string>("ReceivedHeader");
            var content3 = context.WaitForExternalEvent<string>("ReceivedOrderDetails");

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
            if(orchestratorProcessStatus is null) {
                instanceId = await starter.StartNewAsync("DurableFunctionsOrchestrationCSharp", prefix);
            } else {
                instanceId = prefix; 
            }

            await starter.RaiseEventAsync(instanceId, "ReceivedHeader", prefix);
            await starter.RaiseEventAsync(instanceId, "ReceivedOrderDetails", prefix);
            await starter.RaiseEventAsync(instanceId, "ReceivedPO", prefix); 


            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}