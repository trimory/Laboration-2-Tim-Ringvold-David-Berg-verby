using System.Xml.Serialization;
using System;
using System.Collections.Generic;
namespace Laboration2MVC.Models
{
   
        [XmlRoot("TransactionReport")]
        public class ReportViewModel
        {
            public decimal TotalIncome { get; set; }
            public decimal TotalExpense { get; set; }
            public decimal Balance { get; set; }

            [XmlElement("CategorySummaries")]
            public List<CategorySummary> CategorySummaries { get; set; } = new List<CategorySummary>();
        }

        public class CategorySummary
        {
            [XmlAttribute("Name")]
            public string CategoryName { get; set; }

            public decimal Income { get; set; }
            public decimal Expense { get; set; }
            public decimal Balance { get; set; }
            public int TransactionCount { get; set; }
        }
    }

