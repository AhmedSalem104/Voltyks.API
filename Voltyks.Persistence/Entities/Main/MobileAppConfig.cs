namespace Voltyks.Persistence.Entities.Main
{
    public class MobileAppConfig : BaseEntity<int>
    {
        public bool MobileAppEnabled { get; set; } = true;
    }
}
