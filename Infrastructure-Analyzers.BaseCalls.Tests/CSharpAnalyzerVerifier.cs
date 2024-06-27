using System;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing.XUnit;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Infrastructure_Analyzers.BaseCalls.Tests;

public static class CSharpAnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    private class Test : CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>;

    private static readonly Lazy<ReferenceAssemblies> s_net80 =
        new(() => new ReferenceAssemblies("net8.0", new PackageIdentity("Microsoft.NETCore.App.Ref", "8.0.0"),
            Path.Combine("ref", "net8.0")));

    public static DiagnosticResult Diagnostic(DiagnosticDescriptor desc) =>
        AnalyzerVerifier<TAnalyzer>.Diagnostic(desc);

    public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        var contextAssemblyLocation = typeof(BaseCallCheckAttribute).Assembly.Location;

        var test = new Test
        {
            TestCode = source,
            ReferenceAssemblies = GetReferenceAssemblies(typeof(BaseCallCheckAttribute).Assembly),
            SolutionTransforms =
            {
                (solution, id) =>
                {
                    var project = solution.GetProject(id);
                    project = project
                        .AddMetadataReference(MetadataReference.CreateFromFile(contextAssemblyLocation));
                    return project.Solution;
                }
            }
        };
        test.ExpectedDiagnostics.AddRange(expected);

        return test.RunAsync();
    }

    private static ReferenceAssemblies GetReferenceAssemblies(Assembly assembly)
    {
        return assembly.GetCustomAttribute<TargetFrameworkAttribute>()!.FrameworkName switch
        {
            ".NETCoreApp,Version=v8.0" => s_net80.Value,
            ".NETStandard,Version=v2.0" => ReferenceAssemblies.NetStandard.NetStandard20,
            var frameworkName => throw new NotSupportedException($"'{frameworkName}' is not supported.")
        };
    }
}