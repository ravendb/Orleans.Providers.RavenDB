using Orleans;

namespace UnitTests.Grains;

public interface IReminderGrainForTesting : IGrainWithIntegerKey
{
    Task<bool> IsReminderExists(string reminderName);
    Task AddReminder(string reminderName);
    Task RemoveReminder(string reminderName);

    // New methods for testing purposes
    Task<string> UpsertReminder(ReminderEntry entry);
    Task<ReminderTableData> ReadRowsInRange(uint begin, uint end);
}