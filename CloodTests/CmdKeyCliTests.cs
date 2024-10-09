using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CloodKey;
using CloodKey.Interfaces;

namespace CloodTests
{
    [TestClass]
    public class CmdKeyCliTests
    {
        private CmdKeyCli _cmdKeyCli;

        [TestInitialize]
        public void Setup()
        {
            _cmdKeyCli = new CmdKeyCli();
        }

        [TestMethod]
        public void TestAddingKey()
        {
            // Arrange
            string key = "TestKey1";
            string value = "TestValue1";

            // Act
            string result = _cmdKeyCli.Set(key, value);

            // Assert
            Assert.AreEqual("Key set successfully.", result);
        }

        [TestMethod]
        public void TestAddingAndGettingKey()
        {
            // Arrange
            string key = "TestKey2";
            string value = "TestValue2";

            // Act
            _cmdKeyCli.Set(key, value);
            string retrievedValue = _cmdKeyCli.Get(key);

            // Assert
            Assert.AreEqual(value, retrievedValue);
        }

        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void TestGettingNonexistentKey()
        {
            // Arrange
            string nonexistentKey = "NonexistentKey";

            // Act & Assert
            _cmdKeyCli.Get(nonexistentKey); // This should throw a KeyNotFoundException
        }
    }
}
