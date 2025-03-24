using Laboration2MVC.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Xml.Serialization;
using System.Diagnostics;
using System.ComponentModel.Design;
using System;

public class ReportController : Controller
{
    private readonly DatabaseModel dbModel;

    public ReportController(DatabaseModel importDbModel)
    {
        dbModel = importDbModel;
    }

    public async Task<IActionResult> Report()
    {
        try
        {
            var transactions = await dbModel.GetTransactions();

            var report = GenerateReport(transactions);

            ViewData["reportResult"] = report;
            return View(report);
        }
        catch (Exception)
        {
            ViewData["CatchError"] = "Error accessing database";
            return View(new ReportViewModel());
        }
    }

    private ReportViewModel GenerateReport(List<TransactionModel> transactions)
    {
        try
        {
            var report = new ReportViewModel();

            report.TotalIncome = transactions.Where(t => t.Amount > 0).Sum(t => (decimal)t.Amount);
            report.TotalExpense = transactions.Where(t => t.Amount < 0).Sum(t => (decimal)t.Amount);
            report.Balance = report.TotalIncome + report.TotalExpense;

            var categoryGroups = transactions.GroupBy(t => t.Category);

            foreach (var group in categoryGroups)
            {
                var categorySummary = new CategorySummary
                {
                    CategoryName = group.Key,
                    Income = group.Where(t => t.Amount > 0).Sum(t => (decimal)t.Amount),
                    Expense = group.Where(t => t.Amount < 0).Sum(t => (decimal)t.Amount),
                    TransactionCount = group.Count(),
                    Balance = group.Sum(t => (decimal)t.Amount)
                };

                report.CategorySummaries.Add(categorySummary);
            }

            return report;
        }
        catch (Exception)
        {
            ViewData["CatchError"] = "Error accessing database";
            return new ReportViewModel();
        }
    }
    public async Task<IActionResult> DownloadReportXml()
    {
        try
        {
            var transactions = await (dbModel.GetTransactions());

            var report = GenerateReport(transactions);

            // Serialize to XML
            var serializer = new XmlSerializer(typeof(ReportViewModel));
            using var memoryStream = new MemoryStream();
            using var streamWriter = new StreamWriter(memoryStream, Encoding.UTF8);

            serializer.Serialize(streamWriter, report);

            memoryStream.Position = 0;

            // Create file name with date
            var fileName = $"TransaktionsRapport_{DateTime.Now:yyyy-MM-dd}.xml";

            // Return XML file for download 
            return File(memoryStream.ToArray(), "application/xml", fileName);
        }
        catch (Exception)
        {
            ViewData["CatchError"] = "Error creating file";
            return View(new ReportViewModel());
        }
    }
}
