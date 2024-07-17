// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier: MIT
using Remotion.Infrastructure.Analyzers.BaseCalls.UnitTests.Utilities;

namespace Remotion.Infrastructure.Analyzers.BaseCalls.UnitTests;

using System.Threading.Tasks;
using Xunit;

public class AnonymousMethodsTests
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

    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text);
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

    var expected = new[]
                   {
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(BaseCallAnalyzer.InNonOverridingMethod)
                           .WithLocation(6, 25)
                           .WithArguments("Test"),
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(BaseCallAnalyzer.InAnonymousMethod)
                           .WithLocation(8, 33)
                   };

    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
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

    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text);
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


    var expected = new[]
                   {
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(BaseCallAnalyzer.InNonOverridingMethod)
                           .WithLocation(6, 25)
                           .WithArguments("Test"),
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(BaseCallAnalyzer.InAnonymousMethod)
                           .WithLocation(8, 38)
                   };

    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
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

    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text);
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

    var expected = new[]
                   {
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(BaseCallAnalyzer.InNonOverridingMethod)
                           .WithLocation(6, 25)
                           .WithArguments("Test"),
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(BaseCallAnalyzer.InAnonymousMethod)
                           .WithLocation(8, 42)
                   };

    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
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
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(BaseCallAnalyzer.InNonOverridingMethod)
                           .WithLocation(6, 25),

                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(BaseCallAnalyzer.InAnonymousMethod)
                           .WithLocation(8, 38),

                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(BaseCallAnalyzer.InAnonymousMethod)
                           .WithLocation(10, 42),
                   };


    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
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

    var expected = new[]
                   {
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(BaseCallAnalyzer.InNonOverridingMethod)
                           .WithLocation(6, 25)
                           .WithArguments("Test"),
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(BaseCallAnalyzer.InAnonymousMethod)
                           .WithLocation(8, 33)
                   };

    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }
}