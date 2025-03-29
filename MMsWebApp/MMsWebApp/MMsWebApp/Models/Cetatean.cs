namespace MMsWebApp.Models
{
    public class Cetatean
    {
        public int Id { get; set; } // Auto-incremented primary key
        public required string Nume { get; set; }
        public required string Prenume { get; set; }
        public required string Email { get; set; }
        public required string CNP { get; set; }
        public ICollection<PubelaCetatean> PubeleCetateni { get; set; } = new List<PubelaCetatean>();
    }
}
