using Microsoft.AspNetCore.Mvc;

namespace Laboration2MVC.Models
{
    public class TransactionModel
    {

        //fix unnecessary public access modifiers
        public int TransactionID { get; set; } 
        public string BookingDate { get; set; } = string.Empty;
        public string TransactionDate { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public double Amount { get; set; }
        public double Balance { get; set; }

        public string Category { get; set; } = "Övrigt";

        public bool UserOverwrittenCategory { get; set; } = false;


    }
}
