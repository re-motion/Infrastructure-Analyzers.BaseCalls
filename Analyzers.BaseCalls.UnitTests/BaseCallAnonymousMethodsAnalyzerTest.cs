// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier: MIT
namespace Remotion.Infrastructure.Analyzers.BaseCalls.UnitTests;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

public class BaseCallAnonymousMethodsTest
{
  [Fact]
  public async Task AnonymousMethod_WithoutBaseCall_ReportsNothing ()
  {
    const string text = @"
        using System;

        public class TestClass
        {
            public void TestMethod()
            {
                Action action = delegate()
                {
                    Console.WriteLine(""Hello, World!"");
                };
            }
        }";

    await CSharpAnalyzerVerifier<BaseCallAnonymousMethodsAnalyzer>.VerifyAnalyzerAsync(text);
  }

  [Fact]
  public async Task AnonymousMethod_WithBaseCall_ReportsDiagnostic ()
  {
    const string text = @"
        using System;

        public class TestClass
        {
            public void TestMethod()
            {
                Action action = delegate()
                {
                    base.ToString();
                };
            }
        }";

    var expected = CSharpAnalyzerVerifier<BaseCallAnonymousMethodsAnalyzer>
        .Diagnostic(BaseCallAnonymousMethodsAnalyzer.DiagnosticDescriptionBaseCallFoundInAnonymousMethod)
        .WithLocation(8, 33);

    await CSharpAnalyzerVerifier<BaseCallAnonymousMethodsAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task SimpleLambda_WithoutBaseCall_ReportsNothing ()
  {
    const string text = @"
        using System;

        public class TestClass
        {
            public void TestMethod()
            {
                Action<int> action = x => Console.WriteLine(x);
            }
        }";

    await CSharpAnalyzerVerifier<BaseCallAnonymousMethodsAnalyzer>.VerifyAnalyzerAsync(text);
  }

  [Fact]
  public async Task SimpleLambda_WithBaseCall_ReportsDiagnostic ()
  {
    const string text = @"
        using System;

        public class TestClass
        {
            public void TestMethod()
            {
                Action<int> action = x => base.ToString();
            }
        }";

    var expected = CSharpAnalyzerVerifier<BaseCallAnonymousMethodsAnalyzer>
        .Diagnostic(BaseCallAnonymousMethodsAnalyzer.DiagnosticDescriptionBaseCallFoundInAnonymousMethod)
        .WithLocation(8, 38);

    await CSharpAnalyzerVerifier<BaseCallAnonymousMethodsAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task ParenthesizedLambda_WithoutBaseCall_ReportsNothing ()
  {
    const string text = @"
        using System;

        public class TestClass
        {
            public void TestMethod()
            {
                Func<int, int> func = (x) => { return x * 2; };
            }
        }";

    await CSharpAnalyzerVerifier<BaseCallAnonymousMethodsAnalyzer>.VerifyAnalyzerAsync(text);
  }

  [Fact]
  public async Task ParenthesizedLambda_WithBaseCall_ReportsDiagnostic ()
  {
    const string text = @"
        using System;

        public class TestClass
        {
            public void TestMethod()
            {
                Func<int, string> func = (x) => { base.ToString(); return x.ToString(); };
            }
        }";

    var expected = CSharpAnalyzerVerifier<BaseCallAnonymousMethodsAnalyzer>
        .Diagnostic(BaseCallAnonymousMethodsAnalyzer.DiagnosticDescriptionBaseCallFoundInAnonymousMethod)
        .WithLocation(8, 42);

    await CSharpAnalyzerVerifier<BaseCallAnonymousMethodsAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task NestedAnonymousMethods_WithBaseCallInInner_ReportsDiagnostic ()
  {
    const string text = @"
        using System;

        public class TestClass
        {
            public void TestMethod()
            {
                Action outerAction = delegate()
                {
                    Action innerAction = () => base.ToString();
                };
            }
        }";

    var expected = new[]
                   {
                       CSharpAnalyzerVerifier<BaseCallAnonymousMethodsAnalyzer>
                           .Diagnostic(BaseCallAnonymousMethodsAnalyzer.DiagnosticDescriptionBaseCallFoundInAnonymousMethod)
                           .WithLocation(8, 38),

                       CSharpAnalyzerVerifier<BaseCallAnonymousMethodsAnalyzer>
                           .Diagnostic(BaseCallAnonymousMethodsAnalyzer.DiagnosticDescriptionBaseCallFoundInAnonymousMethod)
                           .WithLocation(10, 42)
                   };


    await CSharpAnalyzerVerifier<BaseCallAnonymousMethodsAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task AnonymousMethod_WithBaseCallInConditional_ReportsDiagnostic ()
  {
    const string text = @"
        using System;

        public class TestClass
        {
            public void TestMethod()
            {
                Action action = delegate()
                {
                    if (true)
                    {
                        base.ToString();
                    }
                };
            }
        }";

    var expected = CSharpAnalyzerVerifier<BaseCallAnonymousMethodsAnalyzer>
        .Diagnostic(BaseCallAnonymousMethodsAnalyzer.DiagnosticDescriptionBaseCallFoundInAnonymousMethod)
        .WithLocation(8, 33);

    await CSharpAnalyzerVerifier<BaseCallAnonymousMethodsAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }
}