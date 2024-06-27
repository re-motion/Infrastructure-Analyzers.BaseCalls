using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Remotion.Infrastructure.Analyzers.BaseCalls.UnitTests;

public class SampleSemanticAnalyzerTests
{
  [Fact]
  public async Task SetSpeedHugeSpeedSpecified_AlertDiagnostic()
  {
    const string text =
      """
      public class Program
      {
          public void Main()
          {
              var spaceship = new Spaceship();
              spaceship.SetSpeed(300000000);
          }
      }

      public class Spaceship
      {
          public void SetSpeed(long speed) {}
      }

      """;

    var expected = CSharpAnalyzerVerifier<SampleSemanticAnalyzer, DefaultVerifier>.Diagnostic()
      .WithLocation(7, 28)
      .WithArguments("300000000");
    await CSharpAnalyzerVerifier<SampleSemanticAnalyzer, DefaultVerifier>.VerifyAnalyzerAsync(text, expected);
  }
}