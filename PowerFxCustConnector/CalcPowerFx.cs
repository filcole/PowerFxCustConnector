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
using PowerFxCustConnector.Models;
using System;

namespace PowerFxCustConnector
{
    public class CalcPowerFx

    {
        private readonly ILogger<CalcPowerFx> _logger;

        public CalcPowerFx(ILogger<CalcPowerFx> log)
        {
            _logger = log;
        }

        [FunctionName(nameof(CalcPowerFx))]
        [OpenApiOperation(operationId: "CalcPowerFx", tags: new[] { "Calculation" })]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(CalcPowerFxRequest), Required = true, Description = "Request parameters and formulas")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string[]), Description = "The available functions exposed by the PowerFx interpreter")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string json = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogDebug("inputjson={json}", json);

            CalcPowerFxRequest request;

            try
            { 
                request = JsonConvert.DeserializeObject<CalcPowerFxRequest>(json);
            }
            catch
            {
                return new BadRequestResult();
            }


            var engine = new RecalcEngine();

            var input = (RecordValue)FormulaValue.FromJson(request.InputJson);

            FormulaValue value;

            var formulaFx = request.Formulas[0].Expression;
            try
            {
                

                value = engine.Eval(formulaFx, input);
            }
            catch (System.InvalidOperationException e)
            {
                _logger.LogCritical($"Error InvalidOperationException: {e.Message} on formula {formulaFx}", e);
                throw;
            }

            return new OkObjectResult(value.ToObject());

            //string name = req.Query["name"];

            //dynamic data = JsonConvert.DeserializeObject(requestBody);
            //name = name ?? data?.name;

            //string responseMessage = string.IsNullOrEmpty(name)
            //    ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
            //    : $"Hello, {name}. This HTTP triggered function executed successfully.";

            //return new OkObjectResult(responseMessage);
        }
    }
}

