namespace Orleans.Providers.RavenDb.Reminders
{
    /// <summary>
    /// Represents a reminder document stored in RavenDB for Orleans reminders.
    /// </summary>
    public class RavenDbReminderDocument
    {
        /// <summary>
        /// The unique document identifier in RavenDB.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The serialized grain ID associated with the reminder.
        /// </summary>
        public string GrainId { get; set; } = string.Empty;

        /// <summary>
        /// The name of the reminder.
        /// </summary>
        public string ReminderName { get; set; } = string.Empty;

        /// <summary>
        /// The time at which the reminder was first scheduled to fire.
        /// </summary>
        public DateTime StartAt { get; set; }

        /// <summary>
        /// The interval between reminder firings.
        /// </summary>
        public TimeSpan Period { get; set; }

        /// <summary>
        /// The last time the reminder was updated.
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// The hash code used for consistent hashing of the reminder.
        /// </summary>
        public uint HashCode { get; set; }
        
    }
}
