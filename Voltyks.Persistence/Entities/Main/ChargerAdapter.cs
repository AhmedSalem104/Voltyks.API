namespace Voltyks.Persistence.Entities.Main
{
    public class ChargerAdapter
    {
        public int ChargerId { get; set; }
        public Charger Charger { get; set; }

        public int ProtocolId { get; set; }
        public Protocol Protocol { get; set; }
    }
}
