using NUnit.Framework;
using System.IO;
using System.Collections.Generic;
using Clood;

namespace CloodTest;

[TestFixture]
public class CSharpSymbolTreeAnalyzerTests
{
    private CSharpSymbolTreeAnalyzer _analyzer;
    private string _testFilePath;

    [SetUp]
    public void Setup()
    {
        _analyzer = new CSharpSymbolTreeAnalyzer();
        _testFilePath = Path.GetTempFileName();
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }

    [Test]
    public void AnalyzeFile_ComplexClassStructure_ReturnsCorrectSymbolTree()
    {
        // Arrange
        var code = @"
                public class OuterClass
                {
                    public string OuterProperty { get; set; }

                    public void OuterMethod()
                    {
                        var outerVar = 42;

                        void LocalMethod1()
                        {
                            var localMethod1Var = ""test"";

                            void NestedLocalMethod()
                            {
                                var nestedVar = true;
                            }
                        }

                        var anotherOuterVar = 10;

                        void LocalMethod2()
                        {
                            var localMethod2Var = 3.14;
                        }
                    }

                    public static void StaticMethod()
                    {
                        var staticMethodVar = 100;

                        void StaticLocalMethod()
                        {
                            var staticLocalVar = ""static local"";
                        }
                    }

                    private class InnerClass
                    {
                        public int InnerProperty { get; set; }

                        public void InnerMethod()
                        {
                            var innerVar = 1000;

                            void InnerLocalMethod()
                            {
                                var innerLocalVar = ""inner local"";
                            }
                        }
                    }
                }";
        File.WriteAllText(_testFilePath, code);

        // Act
        var symbols = _analyzer.AnalyzeFile(_testFilePath);
        var treeStrings = symbols.ToTreeString();

        // Assert
        var expectedStrings = new List<string>
        {
            "OuterClass",
            "OuterClass>OuterProperty",
            "OuterClass>OuterMethod",
            "OuterClass>OuterMethod>outerVar",
            "OuterClass>OuterMethod>LocalMethod1",
            "OuterClass>OuterMethod>LocalMethod1>localMethod1Var",
            "OuterClass>OuterMethod>NestedLocalMethod",
            "OuterClass>OuterMethod>NestedLocalMethod>nestedVar",
            "OuterClass>OuterMethod>anotherOuterVar",
            "OuterClass>OuterMethod>LocalMethod2",
            "OuterClass>OuterMethod>LocalMethod2>localMethod2Var",
            "OuterClass>StaticMethod",
            "OuterClass>StaticMethod>staticMethodVar",
            "OuterClass>StaticMethod>StaticLocalMethod",
            "OuterClass>StaticMethod>StaticLocalMethod>staticLocalVar",
            "InnerClass",
            "InnerClass>InnerProperty",
            "InnerClass>InnerMethod",
            "InnerClass>InnerMethod>innerVar",
            "InnerClass>InnerMethod>InnerLocalMethod",
            "InnerClass>InnerMethod>InnerLocalMethod>innerLocalVar",

        };
   
        CollectionAssert.AreEqual(expectedStrings, treeStrings);
    }

    [Test]
    public void AnalyzeFile_MethodWithMultipleNestedFunctions_ReturnsCorrectSymbolTree()
    {
        // Arrange
        var code = @"
                public class TestClass
                {
                    public void ComplexMethod()
                    {
                        var topLevelVar = 1;

                        void FirstLevel()
                        {
                            var firstLevelVar = 2;

                            void SecondLevelA()
                            {
                                var secondLevelVarA = 3;
                            }

                            void SecondLevelB()
                            {
                                var secondLevelVarB = 4;

                                void ThirdLevel()
                                {
                                    var thirdLevelVar = 5;
                                }
                            }
                        }

                        void AnotherFirstLevel()
                        {
                            var anotherFirstLevelVar = 6;
                        }
                    }
                }";
        File.WriteAllText(_testFilePath, code);

        // Act
        var symbols = _analyzer.AnalyzeFile(_testFilePath);
        var treeStrings = symbols.ToTreeString();

        // Assert
        var expectedStrings = new List<string>
        {
            "TestClass",
            "TestClass>ComplexMethod",
            "TestClass>ComplexMethod>topLevelVar",
            "TestClass>ComplexMethod>FirstLevel",
            "TestClass>ComplexMethod>FirstLevel>firstLevelVar",
            "TestClass>ComplexMethod>SecondLevelA",
            "TestClass>ComplexMethod>SecondLevelA>secondLevelVarA",
            "TestClass>ComplexMethod>SecondLevelB",
            "TestClass>ComplexMethod>SecondLevelB>secondLevelVarB",
            "TestClass>ComplexMethod>ThirdLevel",
            "TestClass>ComplexMethod>ThirdLevel>thirdLevelVar",
            "TestClass>ComplexMethod>AnotherFirstLevel",
            "TestClass>ComplexMethod>AnotherFirstLevel>anotherFirstLevelVar"


        };

        CollectionAssert.AreEqual(expectedStrings, treeStrings);
    }

    [Test]
    public void AnalyzeFile_ClassWithMultipleMethodsAndProperties_ReturnsCorrectSymbolTree()
    {
        // Arrange
        var code = @"
                public class MultiMemberClass
                {
                    public int Property1 { get; set; }
                    private string Property2 { get; set; }

                    public void Method1()
                    {
                        var method1Var = 1;
                    }

                    private void Method2()
                    {
                        var method2Var = 2;
                    }

                    public static void StaticMethod()
                    {
                        var staticVar = 3;
                    }
                }";
        File.WriteAllText(_testFilePath, code);

        // Act
        var symbols = _analyzer.AnalyzeFile(_testFilePath);
        var treeStrings = symbols.ToTreeString();

        // Assert
        var expectedStrings = new List<string>
        {
            "MultiMemberClass",
            "MultiMemberClass>Property1",
            "MultiMemberClass>Property2",
            "MultiMemberClass>Method1",
            "MultiMemberClass>Method1>method1Var",
            "MultiMemberClass>Method2",
            "MultiMemberClass>Method2>method2Var",
            "MultiMemberClass>StaticMethod",
            "MultiMemberClass>StaticMethod>staticVar"
        };

        CollectionAssert.AreEqual(expectedStrings, treeStrings);
    }
}