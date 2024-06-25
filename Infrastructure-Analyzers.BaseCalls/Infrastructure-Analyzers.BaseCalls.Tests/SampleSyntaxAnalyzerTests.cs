using System.Threading.Tasks;
using Xunit;
using Verifier =
    Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<
        Infrastructure_Analyzers.BaseCalls.SampleSyntaxAnalyzer>;

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

        var expected = Verifier.Diagnostic()
            .WithLocation(2, 14)
            .WithArguments("SuperstitiousClass");
        await Verifier.VerifyAnalyzerAsync(text, expected).ConfigureAwait(false);
    }
}