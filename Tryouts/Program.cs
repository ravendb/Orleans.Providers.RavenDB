using UnitTests;

namespace Tryouts;

public class Tryouts
{
    public static async Task Main(string[] args)
    {
        var test = new ReminderTableTests();
        await test.RegisterAndRetrieveReminderTest();
    }
}