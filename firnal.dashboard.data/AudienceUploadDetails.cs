namespace firnal.dashboard.data
{
    public class AudienceUploadDetails
    {
        public long Id { get; set; }
        public string? AudienceName { get; set; }
        public int Records { get; set; }
        public DateTime UploadedAt { get; set; }
        public bool IsEnriched { get; set; }
        public string? Status { get; set; }
    }
}
