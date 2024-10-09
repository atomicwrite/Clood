using System;
using NUnit.Framework;
using CloodKey;
using CloodKey.Interfaces;

namespace CloodTest
{
    [TestFixture]
    public class CmdKeyCliTests
    {
        private CmdKeyCli _cmdKeyCli;

        [SetUp]
        public void Setup()
        {
            // Use the currently logged-in username
            string currentUsername = Environment.UserName;
            _cmdKeyCli = new CmdKeyCli(currentUsername);
        }

        [Test]
        public void TestAddingKey()
        {
            // Arrange
            string key = "TestKey";
            string value = "TestValue";

            // Act
            string result = _cmdKeyCli.Set(key, value);

            // Assert
            Assert.That(result, Is.EqualTo("Key set successfully."));
        }

        [Test]
        public void TestAddingAndGettingKey()
        {
            // Arrange
            string key = "TestKey2";
            string value = "TestValue2";

            // Act
            _cmdKeyCli.Set(key, value);
            string retrievedValue = _cmdKeyCli.Get(key);

            // Assert
            Assert.That(retrievedValue, Is.EqualTo(value));
        }

        [Test]
        public void TestGettingNonExistentKey()
        {
            // Arrange
            string nonExistentKey = "NonExistentKey";

            // Act & Assert
            Assert.Throws<KeyNotFoundException>(() => _cmdKeyCli.Get(nonExistentKey));
        }
    }
}