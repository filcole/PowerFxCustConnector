using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerFxCustConnector.Models
{
    public class RequestBody
    {
        public string Context { get; set; }
        public string Yaml { get; set; }
    }
}
