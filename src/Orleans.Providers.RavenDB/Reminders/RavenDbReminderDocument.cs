namespace Orleans.Providers.RavenDb.Reminders
{
    public class RavenDbReminderDocument
    {
        public string Id { get; set; }               // Unique key, e.g., "<GrainId>_<ReminderName>"
        public string GrainId { get; set; }           // Grain ID
        public string ReminderName { get; set; }      // Reminder Name
        public DateTime StartAt { get; set; }         // Initial due time for the reminder
        public TimeSpan Period { get; set; }          // Interval at which the reminder should fire
        public DateTime LastUpdated { get; set; }     // Last time the reminder was updated
        public uint HashCode { get; set; }
        
    }
}
