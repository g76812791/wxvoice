using System.Collections.Generic;
using CBase.CMA.CModel;
using System.Text;
using CBase.DB;
using QA.Domain.CMeta;
using CBase;
using QA.Domain;


namespace QA.API.Models
{
    public class AnswerResultSG
    {
        public Domain.Intent intent { get; set; }        
        public QueryDataContainer SortData { get; set; }
        public double timeSpan { get; set; }       
    }

}