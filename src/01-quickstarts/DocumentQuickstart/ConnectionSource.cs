namespace DocumentQuickstart;

public static class ConnectionSource
{
    // Matches docker-compose.yml at the root of this repository.
    public const string ConnectionString =
        "Host=localhost;Port=5440;Database=workshop;Username=postgres;password=postgres";
}
