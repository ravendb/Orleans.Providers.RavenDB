using Orleans;

namespace UnitTests.Grains;

public class ReminderGrainForTesting : Grain, IReminderGrainForTesting, IRemindable
{
    private readonly IReminderTable _reminderTable;

    public ReminderGrainForTesting(IReminderTable reminderTable)
    {
        _reminderTable = reminderTable;
    }

    public async Task<bool> IsReminderExists(string reminderName)
    {
        var reminder = await this.GetReminder(reminderName);
        return reminder != null;
    }

    public Task AddReminder(string reminderName)
    {
        try
        {
            return this.RegisterOrUpdateReminder(reminderName, TimeSpan.FromDays(1), TimeSpan.FromDays(1));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task RemoveReminder(string reminderName)
    {
        var r = await this.GetReminder(reminderName) ?? throw new Exception("Reminder not found");
        await this.UnregisterReminder(r);
    }

    public Task ReceiveReminder(string reminderName, Orleans.Runtime.TickStatus status) => throw new NotSupportedException();

    // Testing methods
    public Task<string> UpsertReminder(ReminderEntry entry)
    {
        return _reminderTable.UpsertRow(entry);
    }

    public Task<ReminderTableData> ReadRowsInRange(uint begin, uint end)
    {
        return _reminderTable.ReadRows(begin, end);
    }
}