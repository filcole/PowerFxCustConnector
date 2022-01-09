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
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Dynamic;
using YamlDotNet.Core;

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
            // FIXME: Improve logging per youtube vid
            _logger.LogInformation("Triggered CalcPowerFxYaml");

            string body = await new StreamReader(req.Body).ReadToEndAsync();

            RequestBody request;

            try
            {
                request = JsonConvert.DeserializeObject<RequestBody>(body);
            }
            catch
            {
                return new BadRequestResult();
            }

            var engine = new RecalcEngine();

            var input = (RecordValue)FormulaValue.FromJson(request.Context ?? "{}");

            List<Formula> formulae;
            try
            {
                // Read the Yaml and parse into a list of variables and expressions
                formulae = GetFormulae(request.Yaml);
            }
            catch (YamlException ex)
            {
                var errmsg = $"Exception {ex.Message} extracting formula from YAML. Inner exception: {ex.InnerException}";
                _logger.LogCritical(errmsg);
                return new BadRequestObjectResult(errmsg);
            }

            // Evaulate each formula in turn, store the result of each formula back in the engine so that it can be used by later formulas
            foreach (var f in formulae)
            {
                try
                {
                    //engine.UpdateVariable(formula.Name, engine.Eval(formula.Expression, input));
                    ////engine.SetFormula(formula.Name, formula.Expression, null);

                    var val = engine.Eval(f.Expression, input);
                    
                    engine.UpdateVariable(f.Name, val);
                }
                catch (System.InvalidOperationException ex)
                {
                    _logger.LogCritical("Exception InvalidOperationException: {0} on formula '{1}'", ex.Message, f.Expression);
                    return new BadRequestObjectResult($"PowerFx error on forumla '{f.Expression}': {ex.Message}");
                }
                catch (Exception ex)
                {
                    _logger.LogCritical("Exception: {0} on formula '{1}'", ex.Message, f.Expression);
                    return new BadRequestObjectResult($"PowerFx error on forumla '{f.Expression}': {ex.Message}");
                }
            }

            var output = new Dictionary<string, Object>();
            foreach (var f in formulae) {
                // Yaml expression may contain a variable multiple times,
                // but it only needs to be returned once.
                if (!output.ContainsKey(f.Name))
                {
                    output[f.Name] = engine.GetValue(f.Name).ToObject();
                }
            }


            string json = JsonConvert.SerializeObject(output);

            _logger.LogInformation("Output={output}", json);

            return new OkObjectResult(json);
        }

        private List<Formula> GetFormulae(string formulaYaml)
        {
            var formulae = new List<Formula>();

            var input = new StringReader(formulaYaml);

            // Load the stream
            var yaml = new YamlStream();
            yaml.Load(input);

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

                            _logger.LogDebug("expression: {expression}", expression);

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

