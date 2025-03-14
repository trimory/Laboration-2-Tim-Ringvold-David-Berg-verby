using Microsoft.AspNetCore.Mvc;

namespace Laboration2MVC.Models
{
    public class APIModel
    {
        public int TransactionID { get; set; }
        public string BookingDate { get; set; }
        public string TransactionDate { get; set; }
        public string reference { get; set; }
        public double Amount { get; set; }
        public double Balance { get; set; }

    }
}
