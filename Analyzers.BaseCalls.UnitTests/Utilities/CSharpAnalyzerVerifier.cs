// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Remotion.Infrastructure.Analyzers.BaseCalls.Attribute;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Remotion.Infrastructure.Analyzers.BaseCalls.UnitTests.Utilities;

public static class CSharpAnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
  public static DiagnosticResult Diagnostic (DiagnosticDescriptor desc)
  {
    return CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(desc);
  }

  public static Task VerifyAnalyzerAsync (string source, params DiagnosticResult[] expected)
  {
    var contextAssemblyLocation = typeof(BaseCallCheckAttribute).Assembly.Location;
    ImmutableArray<PackageIdentity> packages = [new PackageIdentity("Remotion.Mixins", "6.0.0")];

    var test = new Test
               {
                   TestCode = source,
                   ReferenceAssemblies = GetReferenceAssemblies(typeof(BaseCallCheckAttribute).Assembly).AddPackages(packages)
                       .WithNuGetConfigFilePath(@"..\..\..\..\..\Infrastructure-Analyzers.BaseCalls\NuGet.config"),
                   SolutionTransforms =
                   {
                       (solution, id) =>
                       {
                         var project = solution.GetProject(id);
                         project = project!
                             .AddMetadataReference(MetadataReference.CreateFromFile(contextAssemblyLocation));
                         return project.Solution;
                       }
                   }
               };
    test.ExpectedDiagnostics.AddRange(expected);

    return test.RunAsync();
  }

  private static ReferenceAssemblies GetReferenceAssemblies (Assembly assembly)
  {
    return new ReferenceAssemblies(
        "net8.0-windows7.0",
        new PackageIdentity("Microsoft.NETCore.App.Ref", "8.0.0"),
        Path.Combine("ref", "net8.0"));
  }

  private class Test : CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>;
}