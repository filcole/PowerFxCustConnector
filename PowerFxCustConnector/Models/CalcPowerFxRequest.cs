﻿using Newtonsoft.Json;
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
        public List<Formula> Formulas { get; set; }
    }

    public class Formula
    {
        public string Name { get; set; }
        public string Expression { get; set; }
    }
}
