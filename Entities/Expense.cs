namespace Backend.Entities;

public class Expense
{
    public int ExpenseId { get; set; }

    public int BuildingId { get; set; }

    public string ExpenseName { get; set; }

    public decimal Amount { get; set; }

    public DateTime ExpenseDate { get; set; }

    public string? Category { get; set; }

    public string? Note { get; set; }

    public Building Building { get; set; }
}