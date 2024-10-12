using CloodKey;

namespace CloodTest;

[TestFixture]
public class SecretStoreTests
{
    private SecretStore _secretStore;

    [SetUp]
    public void Setup()
    {
        // Use the currently logged-in username
        var currentUsername = Environment.UserName;
        _secretStore = new SecretStore(currentUsername);
    }

    [Test]
    public async Task TestAddingKey()
    {
        // Arrange
        const string key = "TestKey";
        const string value = "TestValue";

        // Act
        await _secretStore.Set(key, value);

        // Assert
        Assert.Pass();
    }

    [Test]
    public async Task TestAddingAndGettingKey()
    {
        // Arrange
        var key = "TestKey2";
        var value = "TestValue2";

        // Act
        await _secretStore.Set(key, value);
        var retrievedValue = await _secretStore.Get(key);

        // Assert
        Assert.That(retrievedValue, Is.EqualTo(value));
    }

    [Test]
    public void TestGettingNonExistentKey()
    {
        // Arrange
        const string nonExistentKey = "NonExistentKey";

        // Act & Assert
        async void Code() => await _secretStore.Get(nonExistentKey);

        Assert.Throws<KeyNotFoundException>(Code);
    }
}