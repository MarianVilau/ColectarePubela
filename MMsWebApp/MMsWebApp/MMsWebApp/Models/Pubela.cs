namespace MMsWebApp.Models
{
    public class Pubela
    {
        public required string Id { get; set; }
        public required string Tip { get; set; }
        
        // Navigation properties
        public ICollection<Colectare> Colectari { get; set; } = new List<Colectare>();
        public ICollection<PubelaCetatean> PubeleCetateni { get; set; } = new List<PubelaCetatean>();
    }
}
