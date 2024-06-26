using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Infrastructure_Analyzers.BaseCalls.Tests;

public class BaseCallTest
{
    [Fact]
    public async void BasicBaseCallTest()
    {
        const string text = @"
using Infrastructure_Analyzers.BaseCalls;

public abstract class BaseClass
{
    [BaseCallCheck(BaseCall.IsMandatory)]
    public virtual void Test ()
    {
        int a = 5;
    }
}

public class DerivedClass : BaseClass
{
    public override void Test ()
    {
        int b = 7;
        //base.Test();
    }
}";
        /*
         var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer, DefaultVerifier>.Diagnostic()
            .WithLocation(14, 26)
            .WithArguments("Test");
         */
        var expected = DiagnosticResult.EmptyDiagnosticResults;
        await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
    }
}