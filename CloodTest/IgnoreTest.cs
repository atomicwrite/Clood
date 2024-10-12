namespace CloodTest;

using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;

[TestFixture]
public class GitIgnoreCompilerTests
{
    [Test]
    public void TestCompileAndAccepts()
    {
        const string content = """
                               
                                           # This is a comment
                                           *.txt
                                           !important.txt
                                           /node_modules
                                           temp/
                                       
                               """;

        var compiler = GitIgnoreCompiler.Compile(content);

        Assert.Multiple(() =>
        {
            Assert.That(compiler.Accepts("important.txt"), Is.True);
            Assert.That(compiler.Accepts("subfolder/important.txt"), Is.True);
            Assert.That(compiler.Accepts("document.txt"), Is.False);
            Assert.That(compiler.Accepts("node_modules/package.json"), Is.False);
            Assert.That(compiler.Accepts("temp/file.tmp"), Is.False);
        });
    }

    [Test]
    public void TestCompileAndDenies()
    {
        const string content = """
                               
                                           *.log
                                           !debug.log
                                           /build
                                       
                               """;

        var compiler = GitIgnoreCompiler.Compile(content);

        Assert.Multiple(() =>
        {
            Assert.That(compiler.Denies("error.log"), Is.True);
            Assert.That(compiler.Denies("build/output.dll"), Is.True);
            Assert.That(compiler.Denies("debug.log"), Is.False);
            Assert.That(compiler.Denies("src/main.cs"), Is.False);
        });
    }

    [Test]
    public void TestCompileAndMaybe()
    {
        const string content = """
                               
                                           docs/**/*.md
                                           **/temp/
                                       
                               """;

        var compiler = GitIgnoreCompiler.Compile(content);

        Assert.Multiple(() =>
        {
            Assert.That(compiler.Accepts("docs/readme.md"), Is.True);
            Assert.That(compiler.Denies("docs/api/index.md"), Is.True);
            Assert.That(compiler.Denies("project/temp/cache.tmp"), Is.True);
            Assert.That(compiler.Accepts("license.txt"), Is.True);
        });
    }

    [Test]
    public void TestCompileWithComplexPatterns()
    {
        const string content = """
                               
                                           # Ignore all .log files
                                           *.log
                                           # But not important.log
                                           !important.log
                                           # Ignore all files in any directory named temp
                                           **/temp/**
                                           # Ignore all .txt files in the docs directory and its subdirectories
                                           docs/**/*.txt
                                       
                               """;

        var compiler = GitIgnoreCompiler.Compile(content);

        Assert.Multiple(() =>
        {
            Assert.That(compiler.Denies("error.log"), Is.True);
            Assert.That(compiler.Denies("important.log"), Is.False);
            Assert.That(compiler.Denies("somedir/temp/cache.tmp"), Is.True);
            Assert.That(compiler.Accepts("docs/manual.txt"), Is.True);
            Assert.That(compiler.Denies("docs/chapter1/notes.txt"), Is.True);
            Assert.That(compiler.Denies("docs/readme.md"), Is.False);
        });
    }
}