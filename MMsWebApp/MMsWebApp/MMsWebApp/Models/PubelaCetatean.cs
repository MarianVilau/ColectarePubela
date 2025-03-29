namespace MMsWebApp.Models
{
    public class PubelaCetatean
    {
        public int Id { get; set; } // Auto-incremented primary key
        public required string PubelaId { get; set; }
        public required int CetateanId { get; set; }
        public required string Adresa { get; set; }

        // Navigation properties
        public Pubela? Pubela { get; set; }
        public Cetatean? Cetatean { get; set; }
    }
}
