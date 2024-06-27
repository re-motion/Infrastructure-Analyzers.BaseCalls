// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier:MIT

using System;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Remotion.Infrastructure.Analyzers.BaseCalls.UnitTests;

public static class CSharpAnalyzerVerifier<TAnalyzer>
  where TAnalyzer : DiagnosticAnalyzer, new()
{
  public static DiagnosticResult Diagnostic(DiagnosticDescriptor desc)
  {
    return CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(desc);
  }

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
      ".NETCoreApp,Version=v8.0" => ReferenceAssemblies.Net.Net80,
      ".NETStandard,Version=v2.0" => ReferenceAssemblies.NetStandard.NetStandard20,
      var frameworkName => throw new NotSupportedException($"'{frameworkName}' is not supported.")
    };
  }

  private class Test : CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>;
}