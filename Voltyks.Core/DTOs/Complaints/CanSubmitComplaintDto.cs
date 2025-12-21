namespace Voltyks.Core.DTOs.Complaints
{
    public class CanSubmitComplaintDto
    {
        public bool CanSubmit { get; set; }
        public int HoursRemaining { get; set; }
        public int MinutesRemaining { get; set; }
    }
}
