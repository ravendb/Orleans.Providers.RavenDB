namespace Orleans.Providers.RavenDb.Reminders
{
    /// <summary>
    /// Represents a reminder document stored in RavenDB for Orleans reminders.
    /// </summary>
    public class RavenDbReminderDocument
    {
        public string Id { get; set; }    
        public string GrainId { get; set; } 
        public string ReminderName { get; set; }
        public DateTime StartAt { get; set; }
        public TimeSpan Period { get; set; }
        public DateTime LastUpdated { get; set; }
        public uint HashCode { get; set; }
        
    }
}
