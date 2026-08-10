using Marten.Schema;

namespace Projections.TeleHealth;

// Document
public class Specialty
{
    [Identity]
    public string Code { get; set; }
    public string Description { get; set; }
}




