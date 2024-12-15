using Orleans;
using Orleans.Runtime;
using UnitTests.Grains;
using UnitTests.Infrastructure;
using Xunit;

namespace UnitTests;

public class BasicReminderTableTests : IClassFixture<RavenDbReminderFixture>
{
    private readonly RavenDbReminderFixture _fixture;

    public BasicReminderTableTests(RavenDbReminderFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Test_ReminderRegistration()
    {
        var testGrainId = 1;
        var reminderName = "test-reminder";

        var grain = _fixture.Client.GetGrain<IReminderGrainForTesting>(testGrainId);
        await grain.AddReminder(reminderName);

        var exists = await grain.IsReminderExists(reminderName);
        Assert.True(exists);
    }

    [Fact]
    public async Task Test_ReadRows_RangeBoundaries()
    {
        var grainId1 = GrainId.Create("test", "grain1");
        var grainId2 = GrainId.Create("test", "grain2");

        var reminder1 = new ReminderEntry
        {
            GrainId = grainId1,
            ReminderName = "test1",
            StartAt = DateTime.UtcNow,
            Period = TimeSpan.FromMinutes(1)
        };

        var reminder2 = new ReminderEntry
        {
            GrainId = grainId2,
            ReminderName = "test2",
            StartAt = DateTime.UtcNow,
            Period = TimeSpan.FromMinutes(2)
        };

        var reminderTableGrain = _fixture.Client.GetGrain<IReminderGrainForTesting>(1);

        await reminderTableGrain.UpsertReminder(reminder1);
        await reminderTableGrain.UpsertReminder(reminder2);

        var result = await reminderTableGrain.ReadRowsInRange(0, uint.MaxValue);

        Assert.Contains(result.Reminders, r => r.GrainId == grainId1);
        Assert.Contains(result.Reminders, r => r.GrainId == grainId2);
    }

    [Fact]
    public async Task Test_ReminderDeletion()
    {
        var testGrainId = 2;
        var reminderName = "test-reminder-delete";

        var grain = _fixture.Client.GetGrain<IReminderGrainForTesting>(testGrainId);

        // Add a reminder
        await grain.AddReminder(reminderName);

        // Verify the reminder exists
        var existsBeforeDeletion = await grain.IsReminderExists(reminderName);
        Assert.True(existsBeforeDeletion, "Reminder should exist before deletion.");

        // Remove the reminder
        await grain.RemoveReminder(reminderName);

        // Verify the reminder no longer exists
        var existsAfterDeletion = await grain.IsReminderExists(reminderName);
        Assert.False(existsAfterDeletion, "Reminder should not exist after deletion.");
    }

    [Fact]
    public async Task Test_ReminderOverwrite()
    {
        var testGrainId = 3;
        var reminderName = "test-reminder-overwrite";

        var grain = _fixture.Client.GetGrain<IReminderGrainForTesting>(testGrainId);

        // Add a reminder
        await grain.AddReminder(reminderName);

        // Verify the reminder exists
        var existsBeforeOverwrite = await grain.IsReminderExists(reminderName);
        Assert.True(existsBeforeOverwrite, "Reminder should exist before overwrite.");

        // Register the same reminder again (overwrite)
        await grain.AddReminder(reminderName);

        // Verify the reminder still exists after overwriting
        var existsAfterOverwrite = await grain.IsReminderExists(reminderName);
        Assert.True(existsAfterOverwrite, "Reminder should still exist after overwrite.");
    }

    [Fact]
    public async Task Test_NonExistentReminder()
    {
        var testGrainId = 4;
        var reminderName = "non-existent-reminder";

        var grain = _fixture.Client.GetGrain<IReminderGrainForTesting>(testGrainId);

        // Check for a reminder that hasn’t been created
        var exists = await grain.IsReminderExists(reminderName);

        // Verify the reminder does not exist
        Assert.False(exists, "Non-existent reminder should not be found.");
    }

    [Fact]
    public async Task Test_DuplicateReminderRegistration()
    {
        var testGrainId = 10;
        var reminderName = "duplicate-reminder";

        var grain = _fixture.Client.GetGrain<IReminderGrainForTesting>(testGrainId);

        // Register the reminder for the first time
        await grain.AddReminder(reminderName, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20));

        // Register the same reminder again with a different period
        await grain.AddReminder(reminderName, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));

        // Wait for the reminder to trigger
        await Task.Delay(TimeSpan.FromSeconds(40));

        // Verify the reminder triggered correctly
        var triggerCount = await grain.GetReminderTriggerCount(reminderName);
        Assert.True(triggerCount > 0, $"Reminder '{reminderName}' should have triggered at least once after being overwritten.");
    }


    [Fact]
    public async Task Test_MultipleReminders()
    {
        var testGrainId = 5;
        var reminderName1 = "test-reminder-1";
        var reminderName2 = "test-reminder-2";

        var grain = _fixture.Client.GetGrain<IReminderGrainForTesting>(testGrainId);

        // Add multiple reminders
        await grain.AddReminder(reminderName1);
        await grain.AddReminder(reminderName2);

        // Verify both reminders exist
        var exists1 = await grain.IsReminderExists(reminderName1);
        var exists2 = await grain.IsReminderExists(reminderName2);

        Assert.True(exists1, "First reminder should exist.");
        Assert.True(exists2, "Second reminder should exist.");

        // Remove one reminder
        await grain.RemoveReminder(reminderName1);

        // Verify that the first reminder is gone and the second still exists
        var existsAfterRemoval1 = await grain.IsReminderExists(reminderName1);
        var existsAfterRemoval2 = await grain.IsReminderExists(reminderName2);

        Assert.False(existsAfterRemoval1, "First reminder should no longer exist.");
        Assert.True(existsAfterRemoval2, "Second reminder should still exist.");
    }

    [Fact]
    public async Task Test_RemindersAcrossMultipleGrains()
    {
        var grain1Id = 12;
        var grain2Id = 13;
        var reminder1Name = "reminder-grain1";
        var reminder2Name = "reminder-grain2";

        var grain1 = _fixture.Client.GetGrain<IReminderGrainForTesting>(grain1Id);
        var grain2 = _fixture.Client.GetGrain<IReminderGrainForTesting>(grain2Id);

        // Register reminders for both grains
        await grain1.AddReminder(reminder1Name, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20));
        await grain2.AddReminder(reminder2Name, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20));

        // Wait for reminders to trigger
        await Task.Delay(TimeSpan.FromSeconds(40));

        // Verify each grain's reminders were triggered independently
        var grain1TriggerCount = await grain1.GetReminderTriggerCount(reminder1Name);
        var grain2TriggerCount = await grain2.GetReminderTriggerCount(reminder2Name);

        Assert.True(grain1TriggerCount > 0, "Reminder for Grain 1 should have triggered.");
        Assert.True(grain2TriggerCount > 0, "Reminder for Grain 2 should have triggered.");
    }

    [Fact]
    public async Task Test_ReceiveReminder_Triggered()
    {
        var testGrainId = 6;
        var reminderName = "test-receive-reminder";

        var grain = _fixture.Client.GetGrain<IReminderGrainForTesting>(testGrainId);

        // Add a reminder with a short period
        await grain.AddReminder(reminderName, dueTime: TimeSpan.FromSeconds(5), period: TimeSpan.FromMinutes(1));

        // Wait for the reminder to trigger (simulate a short wait time)
        await Task.Delay(TimeSpan.FromSeconds(15));

        // Verify the reminder was triggered
        var triggered = await grain.WasReminderTriggered(reminderName);
        Assert.True(triggered, $"Reminder '{reminderName}' should have triggered.");
    }

    [Fact]
    public async Task Test_ReceiveReminder_MultipleTriggers()
    {
        var testGrainId = 7;
        var reminderName = "test-multiple-triggers";

        var grain = _fixture.Client.GetGrain<IReminderGrainForTesting>(testGrainId);

        // Add a reminder with a short interval
        await grain.AddReminder(reminderName, dueTime: TimeSpan.FromSeconds(10), period: TimeSpan.FromSeconds(5));

        // Wait for multiple reminder triggers
        await Task.Delay(TimeSpan.FromSeconds(40));

        // Verify the reminder was triggered multiple times
        var triggerCount = await grain.GetReminderTriggerCount(reminderName);
        Assert.True(triggerCount >= 2, $"Reminder '{reminderName}' should have triggered at least twice. triggerCount = {triggerCount}");
    }

    [Fact]
    public async Task Test_ReminderStatePersistence()
    {
        var testGrainId = 14;
        var reminderName = "persistent-reminder";

        var grain = _fixture.Client.GetGrain<IReminderGrainForTesting>(testGrainId);

        // Register a reminder
        await grain.AddReminder(reminderName, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5));

        // Wait long enough for at least one reminder to trigger
        await Task.Delay(TimeSpan.FromSeconds(40)); // Allow the first reminder to trigger

        // Capture the trigger count before deactivation
        var triggerCountBeforeDeactivation = await grain.GetReminderTriggerCount(reminderName);
        Assert.True(triggerCountBeforeDeactivation > 0, "Reminder should have triggered at least once before deactivation.");

        // Deactivate the grain explicitly
        await grain.ForceDeactivate();

        // Reactivate the grain by accessing it again
        grain = _fixture.Client.GetGrain<IReminderGrainForTesting>(testGrainId);

        // Wait for reminders to trigger post-reactivation
        await Task.Delay(TimeSpan.FromSeconds(30)); // Allow time for multiple reminders after reactivation

        // Verify the reminder is still active and has triggered additional times
        var triggerCountAfterReactivation = await grain.GetReminderTriggerCount(reminderName);
        Assert.True(triggerCountAfterReactivation > triggerCountBeforeDeactivation,
            $"Reminder should have triggered additional times after reactivation. Before: {triggerCountBeforeDeactivation}, After: {triggerCountAfterReactivation}");
    }


    [Fact]
    public async Task Test_LargeNumberOfReminders()
    {
        var testGrainId = 15;
        var grain = _fixture.Client.GetGrain<IReminderGrainForTesting>(testGrainId);

        const int reminderCount = 100;
        var reminderNames = Enumerable.Range(0, reminderCount).Select(i => $"reminder-{i}").ToList();

        // Register multiple reminders
        foreach (var reminderName in reminderNames)
        {
            await grain.AddReminder(reminderName, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20));
        }

        // Wait for reminders to trigger
        await Task.Delay(TimeSpan.FromSeconds(40));

        // Verify all reminders triggered at least once
        foreach (var reminderName in reminderNames)
        {
            var triggerCount = await grain.GetReminderTriggerCount(reminderName);
            Assert.True(triggerCount > 0, $"Reminder '{reminderName}' should have triggered.");
        }
    }

}