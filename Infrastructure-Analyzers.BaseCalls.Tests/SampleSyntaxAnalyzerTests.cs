using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Infrastructure_Analyzers.BaseCalls.Tests;

public class SampleSyntaxAnalyzerTests
{
    [Fact]
    public async Task SuperstitiousClass_AlertsDiagnostic()
    {
        const string text = @"
public class SuperstitiousClass
{
}
";

        var expected = CSharpAnalyzerVerifier<SampleSyntaxAnalyzer, DefaultVerifier>.Diagnostic()
            .WithLocation(2, 14)
            .WithArguments("SuperstitiousClass");
        await CSharpAnalyzerVerifier<SampleSyntaxAnalyzer, DefaultVerifier>.VerifyAnalyzerAsync(text, expected);
    }
}