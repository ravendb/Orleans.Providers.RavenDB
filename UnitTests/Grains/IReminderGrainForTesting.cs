using Orleans;

namespace UnitTests.Grains;

public interface IReminderGrainForTesting : IGrainWithIntegerKey
{
    Task<bool> IsReminderExists(string reminderName);
    
    Task AddReminder(string reminderName);

    Task AddReminder(string reminderName, TimeSpan dueTime, TimeSpan period);

    Task RemoveReminder(string reminderName);

    Task<string> UpsertReminder(ReminderEntry entry);
    
    Task<ReminderTableData> ReadRowsInRange(uint begin, uint end);

    Task<bool> WasReminderTriggered(string reminderName);

    Task<int> GetReminderTriggerCount(string reminderName);

    Task ForceDeactivate();

}