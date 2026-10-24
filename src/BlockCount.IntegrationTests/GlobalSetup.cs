using NUnit.Framework;

[SetUpFixture]
public class BlockCountGlobalSetup
{
    private GlobalSetupTeardown _setup = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        _setup = new GlobalSetupTeardown();
        _setup.SetUp();
    }

    [OneTimeTearDown]
    public void TearDown() => _setup.TearDown();
}
