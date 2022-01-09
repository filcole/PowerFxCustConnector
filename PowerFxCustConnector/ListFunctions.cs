using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Public.Values;

namespace PowerFxCustConnector
{
    public class ListFunctions

    {
        [FunctionName(nameof(ListFunctions))]
        [OpenApiOperation(operationId: "ListFunctions", tags: new[] { "Info" }, Summary = "List available functions")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string[]), Description = "The available functions exposed by the PowerFx interpreter")]
        public IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
        {
            var engine = new RecalcEngine();

            return new OkObjectResult(engine.GetAllFunctionNames());
        }
    }
}