using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QA.API.Models
{
    public class ExistAnswer
    {
        public bool result { get; set; }
        public List<string> AnswerType { get; set; }
        public string error { get; set; }
        public double TimeSpan { get; set; }
    }
}