using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QA.API.Models
{
    public class FaqNETAnswer
    {
        public string Question { get; set; }
        public string Answer { get; set; }

        public string Domain { get; set; }

        public string UserName { get; set; }     
        public string Time { get; set; }
        public string AnswerID { get; set; }
    }
}