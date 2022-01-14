using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx;
using System.Linq;
using System.Net;

namespace PowerFxCustConnector
{
    public class ListFunctions

    {
        [FunctionName(nameof(ListFunctions))]
        [OpenApiOperation(operationId: "ListFunctions", tags: new[] { "Info" }, Description = "List functions", Summary = "List subset of available Canvas/PowerFx functions exposed by Microsoft.PowerFx"),]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string[]), Description = "The available functions exposed by the PowerFx interpreter")]
        public IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
        {
            // To remove variable not used warning;
            _ = req;

            var engine = new RecalcEngine();

            return new OkObjectResult(engine.GetAllFunctionNames().Distinct().OrderBy(x => x));
        }
    }
}