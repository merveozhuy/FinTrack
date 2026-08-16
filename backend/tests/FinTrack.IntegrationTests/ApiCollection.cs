namespace FinTrack.IntegrationTests;

/// <summary>
/// Shares a single API + database container across all test classes in the collection,
/// so the container starts once per test run instead of once per class.
/// </summary>
[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
