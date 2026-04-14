namespace TheShed.Shared.Models.Entities
{
    public class Country
    {
        public int Id { get; set; }
        public string IsoCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? FlagUrl { get; set; }  // URL de la bandera
    }
}