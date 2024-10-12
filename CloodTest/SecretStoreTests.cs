using System;
using NUnit.Framework;
using CloodKey;
using CloodKey.Interfaces;
using NUnit.Framework;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;

namespace CloodTest
{
}
namespace CloodTest
{
    [TestFixture]
    public class SecretStoreTests
    {
        private SecretStore _secretStore;

        [SetUp]
        public void Setup()
        {
            // Use the currently logged-in username
            string currentUsername = Environment.UserName;
            _secretStore = new SecretStore(currentUsername);
        }

        [Test]
        public async Task TestAddingKey()
        {
            // Arrange
            string key = "TestKey";
            string value = "TestValue";

            // Act
            _secretStore.Set(key, value);

            // Assert
           Assert.Pass();
        }

        [Test]
        public async Task TestAddingAndGettingKey()
        {
            // Arrange
            string key = "TestKey2";
            string value = "TestValue2";

            // Act
            _secretStore.Set(key, value);
            string retrievedValue = await _secretStore.Get(key);

            // Assert
            Assert.That(retrievedValue, Is.EqualTo(value));
        }

        [Test]
        public void TestGettingNonExistentKey()
        {
            // Arrange
            string nonExistentKey = "NonExistentKey";

            // Act & Assert
            Assert.Throws<KeyNotFoundException>(async () => await _secretStore.Get(nonExistentKey));
        }
    }
}