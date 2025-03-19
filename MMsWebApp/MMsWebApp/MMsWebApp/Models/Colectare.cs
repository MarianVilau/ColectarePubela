namespace MMsWebApp.Models
{
    public class Colectare
    {
        public int Id { get; set; } // This will be auto-incremented by the database
        public required string IdPubela { get; set; }
        public DateTime CollectedAt { get; set; }
    }
}
