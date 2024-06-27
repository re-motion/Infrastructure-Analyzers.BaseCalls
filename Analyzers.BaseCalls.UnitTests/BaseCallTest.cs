using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Infrastructure_Analyzers.BaseCalls.Tests;

public class BaseCallTest
{
  [Fact]
  public async Task NoBaseCall_WithBaseCallMandatory_ReportsDiagnostic()
  {
    const string text =
      """
      using Remotion.Infrastructure.Analyzers.BaseCalls;

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
      }
      """;

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.Rule)
        .WithLocation(14, 26)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task NoBaseCall_WithBaseCallOptional_ReportsNothing()
  {
    const string text =
      """
      using Infrastructure_Analyzers.BaseCalls;

      public abstract class BaseClass
      {
          [BaseCallCheck(BaseCall.IsOptional)]
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
      }
      """;
    var expected = DiagnosticResult.EmptyDiagnosticResults;
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }
}