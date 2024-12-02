using Orleans;
using Orleans.Runtime;

namespace UnitTests.Grains;

public class ReminderGrainForTesting : Grain, IReminderGrainForTesting, IRemindable
{
    private readonly IPersistentState<ReminderGrainState> _state;

    private readonly IReminderTable _reminderTable;

    public ReminderGrainForTesting(IReminderTable reminderTable, [PersistentState("reminderState", "Default")] IPersistentState<ReminderGrainState> state)
    {
        _reminderTable = reminderTable;
        _state = state;
    }

    public async Task<bool> IsReminderExists(string reminderName)
    {
        var reminder = await this.GetReminder(reminderName);
        return reminder != null;
    }

    public Task AddReminder(string reminderName) =>
        AddReminder(reminderName, dueTime: TimeSpan.FromDays(1), period: TimeSpan.FromDays(1));

    public Task AddReminder(string reminderName, TimeSpan dueTime, TimeSpan period)
    {
        return this.RegisterOrUpdateReminder(reminderName, dueTime, period);
    }

    public async Task RemoveReminder(string reminderName)
    {
        var r = await this.GetReminder(reminderName) ?? throw new Exception("Reminder not found");
        await this.UnregisterReminder(r);

        if (_state.State.ReminderTriggerCounts.ContainsKey(reminderName))
        {
            _state.State.ReminderTriggerCounts.Remove(reminderName);
            await _state.WriteStateAsync(); // Persist state after removal
        }
    }

    public Task ReceiveReminder(string reminderName, Orleans.Runtime.TickStatus status)
    {
        Console.WriteLine($"Reminder triggered: {reminderName} at {status.CurrentTickTime}");


        // Increment the trigger count for this reminder
        _state.State.ReminderTriggerCounts.TryAdd(reminderName, 0);
        _state.State.ReminderTriggerCounts[reminderName]++;

        return _state.WriteStateAsync(); // Persist state after incrementing
    }

    public Task<bool> WasReminderTriggered(string reminderName)
    {
        _state.State.ReminderTriggerCounts.TryGetValue(reminderName, out var count);
        return Task.FromResult(count > 0);
    }

    public Task<int> GetReminderTriggerCount(string reminderName)
    {
        _state.State.ReminderTriggerCounts.TryGetValue(reminderName, out var count);
        return Task.FromResult(count);
    }

    public Task ForceDeactivate()
    {
        Console.WriteLine($"Deactivating grain {this.GetPrimaryKey()}.");
        this.DeactivateOnIdle(); // Request deactivation
        return Task.CompletedTask;
    }


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

[Serializable]
public class ReminderGrainState
{
    public Dictionary<string, int> ReminderTriggerCounts { get; set; } = new();
}