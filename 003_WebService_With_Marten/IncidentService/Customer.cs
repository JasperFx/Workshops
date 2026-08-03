public class Customer
{
    public Guid Id { get; set; }

    public Dictionary<IncidentCategory, IncidentPriority> Priorities { get; set; }
        = new();
    
    public string Name { get; set; }
    public string Region { get; set; }
    public string Class { get; set; }
}