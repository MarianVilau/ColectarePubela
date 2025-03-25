namespace MMsWebApp.Models
{
    public class PubelaCetatean
    {

        public int Id { get; set; } // Auto-incremented primary key
        public int PubelaId { get; set; }
        public int CetateanId { get; set; }
        public required string Adresa { get; set; }
    }
}
