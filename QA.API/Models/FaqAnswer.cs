using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QA.API.Models
{
    public class FaqAnswer
    {
        public string Question { get; set; }
        public string Answer { get; set; }

        public string Domain { get; set; }

        public object Extra { get; set; }
    }
}