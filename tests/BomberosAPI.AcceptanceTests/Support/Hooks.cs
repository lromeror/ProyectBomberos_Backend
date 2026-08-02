using Reqnroll;

namespace BomberosAPI.AcceptanceTests.Support;

[Binding]
public class Hooks
{
    private readonly TestContext _context;

    public Hooks(TestContext context)
    {
        _context = context;
    }

    [BeforeScenario]
    public void BeforeScenario()
    {
        _context.ClearAuthentication();
    }

    [AfterScenario]
    public void AfterScenario()
    {
        _context.ClearAuthentication();
    }
}
