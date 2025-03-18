using Microsoft.AspNetCore.Mvc;

namespace Laboration2MVC.Models
{
    public class TransactionModel
    {

        //fix unnecessary public access modifiers
        public int TransactionID { get; set; }
        public string BookingDate { get; set; }
        public string TransactionDate { get; set; }
        public string Reference { get; set; }
        public double Amount { get; set; }
        public double Balance { get; set; }

        public bool IsUserReferenceChanged { get; set; } = false; //creates a 


    }
}
