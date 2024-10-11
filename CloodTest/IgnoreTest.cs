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
        var content = @"
            # This is a comment
            *.txt
            !important.txt
            /node_modules
            temp/
        ";

        var compiler = GitIgnoreCompiler.Compile(content);

        Assert.IsTrue(compiler.Accepts("important.txt"));
        Assert.IsTrue(compiler.Accepts("subfolder/important.txt"));
        Assert.IsFalse(compiler.Accepts("document.txt"));
        Assert.IsFalse(compiler.Accepts("node_modules/package.json"));
        Assert.IsFalse(compiler.Accepts("temp/file.tmp"));
    }

    [Test]
    public void TestCompileAndDenies()
    {
        var content = @"
            *.log
            !debug.log
            /build
        ";

        var compiler = GitIgnoreCompiler.Compile(content);

        Assert.IsTrue(compiler.Denies("error.log"));
        Assert.IsTrue(compiler.Denies("build/output.dll"));
        Assert.IsFalse(compiler.Denies("debug.log"));
        Assert.IsFalse(compiler.Denies("src/main.cs"));
    }

    [Test]
    public void TestCompileAndMaybe()
    {
        var content = @"
            docs/**/*.md
            **/temp/
        ";

        var compiler = GitIgnoreCompiler.Compile(content);

        Assert.IsTrue(compiler.Accepts("docs/readme.md"));
        Assert.IsTrue(compiler.Denies("docs/api/index.md"));
        Assert.IsTrue(compiler.Denies("project/temp/cache.tmp"));
        Assert.IsTrue(compiler.Accepts("license.txt"));
    }

    [Test]
    public void TestCompileWithComplexPatterns()
    {
        var content = @"
            # Ignore all .log files
            *.log
            # But not important.log
            !important.log
            # Ignore all files in any directory named temp
            **/temp/**
            # Ignore all .txt files in the docs directory and its subdirectories
            docs/**/*.txt
        ";

        var compiler = GitIgnoreCompiler.Compile(content);

        Assert.IsTrue(compiler.Denies("error.log"));
        Assert.IsFalse(compiler.Denies("important.log"));
        Assert.IsTrue(compiler.Denies("somedir/temp/cache.tmp"));
        Assert.IsTrue(compiler.Accepts("docs/manual.txt"));
        Assert.IsTrue(compiler.Denies("docs/chapter1/notes.txt"));
        Assert.IsFalse(compiler.Denies("docs/readme.md"));
    }
}