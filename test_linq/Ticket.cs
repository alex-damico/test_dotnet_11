namespace test_linq;

public class Ticket
{
    public string Code { get; }
    public string Title { get; }
    public string Description { get; private set; }
    public int Priority { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public bool IsClosed { get; private set; }

    public event EventHandler? Closed;

    public string PriorityLabel => Priority switch
    {
        1 => "Alta",
        2 => "Media",
        3 => "Bassa",
        _ => "Sconosciuta"
    };

    public string TypeName { get; }
    public int TargetResolutionHours { get; }

    public DateTimeOffset DueAt => CreatedAt.AddHours(TargetResolutionHours);

    public Ticket(
        string code,
        string title,
        string description,
        int priority)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "Il codice non può essere vuoto.",
                nameof(code));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "Il titolo non può essere vuoto.",
                nameof(title));
        }

        if (priority < 1 || priority > 3)
        {
            throw new ArgumentOutOfRangeException(
                nameof(priority),
                "La priorità deve essere compresa tra 1 e 3.");
        }

        Code = code.Trim();
        Title = title.Trim();
        Description = description.Trim();
        Priority = priority;
    }

    public void Close()
    {
        if (IsClosed)
        {
            return;
        }

        IsClosed = true;
        Closed?.Invoke(this, EventArgs.Empty);
    }
    

    public string GetSummary()
    {
        return
            $"Codice: {Code}\n" +
            $"Tipo: {TypeName}\n" +
            $"Titolo: {Title}\n" +
            $"Priorità: {PriorityLabel}\n" +
            $"Scadenza: {DueAt:dd/MM/yyyy HH:mm}";
    }

    public override string ToString()
    {
        return $"[{Code}] {TypeName}: {Title}";
    }
}