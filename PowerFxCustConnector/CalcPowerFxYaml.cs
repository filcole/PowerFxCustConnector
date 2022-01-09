using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Public.Values;
using Newtonsoft.Json;
using PowerFxCustConnector.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace PowerFxCustConnector
{
    internal class Formula
    {
        internal string Name { get; set; }
        internal string Expression { get; set; }
    }

    public class CalcPowerFxYaml
    {
        private readonly ILogger<CalcPowerFxYaml> _logger;

        public CalcPowerFxYaml(ILogger<CalcPowerFxYaml> log)
        {
            _logger = log;
        }

        [FunctionName(nameof(CalcPowerFxYaml))]
        [OpenApiOperation(operationId: "CalcPowerFxYaml", tags: new[] { "Calculation" }, Description = "Calculate one or more PowerFx expressions with an optionally provided context using the Microsoft.PowerFx library", Summary = "Calculate PowerFx formulae")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(RequestBody), Required = true, Description = "Request parameters and yaml formulas")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "Json serialised results", Summary = "Evaluated formulae")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req)
        {
            string body = await new StreamReader(req.Body).ReadToEndAsync();

            RequestBody request;

            try
            {
                request = JsonConvert.DeserializeObject<RequestBody>(body);
            }
            catch (Exception ex)
            {
                _logger.LogError("Could not deserialize request: {0}", ex.Message);
                return new BadRequestObjectResult($"Could not deserialise request");
            }

            // Instantiate (Fire up) the PowerFx engine!!
            var engine = new RecalcEngine();

            // We may be passed a JSON context, but if it's not passed then create an empty object.
            var input = (RecordValue)FormulaValue.FromJson(request.Context ?? "{}");

            // Try and get the list of formuale from the passed Yaml
            List<Formula> formulae;
            try
            {
                // Read the Yaml and parse into a list of variables and expressions
                formulae = GetFormulae(request.Yaml);
            }
            catch (YamlException ex)
            {
                var errmsg = $"Exception {ex.Message} extracting formula from YAML. Inner exception: {ex.InnerException}";
                _logger.LogWarning(errmsg);
                return new BadRequestObjectResult(errmsg);
            }

            _logger.LogInformation($"Processing {formulae.Count} formulae with context: {request.Context}");

            // Evaulate each formula in turn, store the result of each formula back in the PowerFx engine
            // so that it can be used by later formulas.
            foreach (var f in formulae)
            {
                try
                {
                    engine.UpdateVariable(f.Name, engine.Eval(f.Expression, input));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Exception: {0} on formula '{1}'", ex.Message, f.Expression);
                    return new BadRequestObjectResult($"PowerFx error on forumla '{f.Expression}': {ex.Message}");
                }
            }

            // Note: Integers serialise as decimal numbers, but the Parse Json step in Power Automate will
            // happily converts them back to integers within Power Automate.
            var output = new Dictionary<string, Object>();
            foreach (var f in formulae)
            {
                // Yaml expression may contain a variable multiple times,
                // but it only needs to be returned once.
                if (!output.ContainsKey(f.Name))
                {
                    output[f.Name] = engine.GetValue(f.Name).ToObject();
                }
            }

            string json = JsonConvert.SerializeObject(output);
            _logger.LogInformation("Successful response: {output}", json);

            return new OkObjectResult(json);
        }

        // Build a list of forumlae from the Yaml that's passed to the function
        private List<Formula> GetFormulae(string formulaYaml)
        {
            // Read and parse the Yaml
            var yaml = new YamlStream();
            yaml.Load(new StringReader(formulaYaml));

            var formulae = new List<Formula>();

            // Fetch all nodes, it's simpler than trying to navigate the tree structure.
            // There's room to improve this!
            foreach (var node in yaml.Documents[0].AllNodes)
            {
                // We're only interested in the mapping nodes, but these may be top-level, or at the bottom
                // of the tree structure that is Yaml SequenceNodes/MappingNodes.
                if (node is YamlMappingNode mapping)
                {
                    foreach (var entry in mapping.Children)
                    {
                        if (entry.Value is YamlScalarNode val)
                        {
                            var expression = RemoveComments(val.Value).Trim();

                            _logger.LogInformation("expression: {expression}", expression);

                            if (expression.StartsWith("="))
                            {
                                var name = ((YamlScalarNode)entry.Key).Value;
                                formulae.Add(new Formula
                                {
                                    Name = name,
                                    // Remove the first character (=)
                                    Expression = expression[1..],
                                });
                            }
                        }
                    }
                }
            }
            return formulae;
        }

        // Remove single line comments '//' and multi-line comments /* xxx */
        // Thank you https://stackoverflow.com/a/3524689
        private static string RemoveComments(string input)
        {
            var blockComments = @"/\*(.*?)\*/";
            var lineComments = @"//(.*?)\r?\n";
            var strings = @"""((\\[^\n]|[^""\n])*)""";
            var verbatimStrings = @"@(""[^""]*"")+";

            return Regex.Replace(input,
                blockComments + "|" + lineComments + "|" + strings + "|" + verbatimStrings,
                me =>
                {
                    if (me.Value.StartsWith("/*") || me.Value.StartsWith("//"))
                    {
                        return me.Value.StartsWith("//") ? Environment.NewLine : "";
                    }
                    // Keep the literal strings
                    return me.Value;
                },
                RegexOptions.Singleline
            );
        }
    }
}