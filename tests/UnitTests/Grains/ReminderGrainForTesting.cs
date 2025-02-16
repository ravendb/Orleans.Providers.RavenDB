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

    public async Task AddReminder(string reminderName, TimeSpan dueTime, TimeSpan period)
    {
        await this.RegisterOrUpdateReminder(reminderName, dueTime, period);

        _state.State.ReminderTriggerCounts.TryAdd(reminderName, 0);

        if (_state.State.ReminderTriggerTimes.ContainsKey(reminderName) == false)
        {
            _state.State.ReminderTriggerTimes[reminderName] = DateTime.MinValue;
        }

        await _state.WriteStateAsync();
    }


    public async Task RemoveReminder(string reminderName)
    {
        try
        {
            var r = await this.GetReminder(reminderName) ?? throw new Exception("Reminder not found");
            await this.UnregisterReminder(r);

            if (_state.State.ReminderMaxTriggers.Remove(reminderName))
            {
                // expired reminder
                _state.State.ReminderTriggerCounts.TryGetValue(reminderName, out var count);
                _state.State.ExpiredRemindersCount.Add(reminderName, count);
            }

            _state.State.ReminderTriggerCounts.Remove(reminderName);
            _state.State.ReminderTriggerTimes.Remove(reminderName);
            
            await _state.WriteStateAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

    }

    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        Console.WriteLine($"Reminder triggered: {reminderName} at {status.CurrentTickTime}");

        await _state.ReadStateAsync();

        _state.State.ReminderTriggerCounts.TryAdd(reminderName, 0);

        _state.State.ReminderTriggerCounts[reminderName]++;
        _state.State.ReminderTriggerTimes[reminderName] = status.CurrentTickTime; // Store timestamp of last trigger

        if (_state.State.ReminderMaxTriggers.TryGetValue(reminderName, out int maxTriggers) &&
            _state.State.ReminderTriggerCounts[reminderName] >= maxTriggers)
        {
            Console.WriteLine($"Reminder {reminderName} reached max triggers ({maxTriggers}), unregistering.");
            await RemoveReminder(reminderName);
        }

        await _state.WriteStateAsync();
    }

    public Task<bool> WasReminderTriggered(string reminderName)
    {
        _state.State.ReminderTriggerCounts.TryGetValue(reminderName, out var count);
        return Task.FromResult(count > 0);
    }

    public Task<int> GetReminderTriggerCount(string reminderName)
    {
        if (_state.State.ExpiredRemindersCount.TryGetValue(reminderName, out var count) == false)
            _state.State.ReminderTriggerCounts.TryGetValue(reminderName, out count);
        return Task.FromResult(count);
    }

    public Task ForceDeactivate()
    {
        Console.WriteLine($"Deactivating grain {this.GetPrimaryKey()}.");
        DeactivateOnIdle(); // Request deactivation
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

    public Task<int> GetTotalReminderTriggerCount()
    {
        return Task.FromResult(_state.State.ReminderTriggerCounts.Values.Sum());
    }

    public Task<Dictionary<string, DateTime>> GetReminderTriggerTimes()
    {
        return Task.FromResult(new Dictionary<string, DateTime>(_state.State.ReminderTriggerTimes));
    }

    public async Task AddExpiringReminder(string reminderName, TimeSpan dueTime, TimeSpan period, int maxTriggers)
    {
        await this.RegisterOrUpdateReminder(reminderName, dueTime, period);

        _state.State.ReminderTriggerCounts.TryAdd(reminderName, 0);

        _state.State.ReminderMaxTriggers[reminderName] = maxTriggers;
        await _state.WriteStateAsync();
    }

}

[Serializable]
public class ReminderGrainState
{
    public Dictionary<string, int> ReminderTriggerCounts { get; set; } = new();
    public Dictionary<string, int> ReminderMaxTriggers { get; set; } = new();
    public Dictionary<string, DateTime> ReminderTriggerTimes { get; set; } = new();
    public Dictionary<string, int> ExpiredRemindersCount { get; set; } = new();


}