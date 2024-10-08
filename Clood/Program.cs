using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.Extensions.Configuration;
using Claudia;
using Markdig;
using Markdig.Syntax;
using CommandLine;

namespace Clood;

internal class Program
{
    private static async Task Main(string[] args)
    {
        await Parser.Default.ParseArguments<CliOptions>(args)
            .WithParsedAsync(async (opts) => await Clood.RunWithOptions(opts));
    }
}