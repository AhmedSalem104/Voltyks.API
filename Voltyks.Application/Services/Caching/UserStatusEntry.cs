namespace Voltyks.Application.Services.Caching
{
    public class UserStatusEntry
    {
        public bool Exists { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsBanned { get; set; }
    }
}
