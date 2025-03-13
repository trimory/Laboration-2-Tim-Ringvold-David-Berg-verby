using System;
using System.Text.Json.Serialization;

public class Transaction
{
    [JsonPropertyName("TransactionID")] // Matchar API:ets fältnamn
    public int TransactionID { get; set; }

    [JsonPropertyName("BookingDate")]
    public DateTime BookingDate { get; set; }

    [JsonPropertyName("TransactionDate")]
    public DateTime TransactionDate { get; set; }

    [JsonPropertyName("Reference")]
    public string Reference { get; set; }

    [JsonPropertyName("Amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("Balance")]
    public decimal Balance { get; set; }

    public string Category { get; set; } = "Övrigt";
}

