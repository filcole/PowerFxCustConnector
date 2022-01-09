using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerFxCustConnector.Models
{
    public class CalcPowerFxRequestYaml
    {
        //[JsonProperty(Required = Required.Always)]
        public string InputJson { get; set; }
        public string Yaml { get; set; }
    }
}
