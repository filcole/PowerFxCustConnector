using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerFxCustConnector.Models
{
    public class CalcPowerFxRequest
    {
        //[JsonProperty(Required = Required.Always)]
        public string InputJson { get; set; }
        public List<Rule> Rules { get; set; }
    }

    public class Rule
    {
        public string Name { get; set; }
        public string Formula { get; set; }
    }
}
