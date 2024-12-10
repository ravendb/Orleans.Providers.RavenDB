namespace Orleans.Providers.RavenDB.Configuration
{
    public class RavenDbReminderOptions : RavenDbOptions
    {
        public bool WaitForIndexesAfterSaveChanges { get; set; }
    }
}
