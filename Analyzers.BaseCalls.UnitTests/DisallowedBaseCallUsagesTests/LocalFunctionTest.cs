// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier: MIT
using System;
using System.Threading.Tasks;
using Remotion.Infrastructure.Analyzers.BaseCalls.UnitTests.Utilities;
using Xunit;

namespace Remotion.Infrastructure.Analyzers.BaseCalls.UnitTests.DisallowedBaseCallUsagesTests;

public class LocalFunctionTest
{
  [Fact]
  public async Task LocalFunction_WithoutBaseCall_ReportsNothing ()
  {
    const string text = @"
            public class TestClass
            {
                public void TestMethod()
                {
                    void LocalFunction()
                    {
                        System.Console.WriteLine(""Hello, World!"");
                    }
                    LocalFunction();
                }
            }";

    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text);
  }

  [Fact]
  public async Task LocalFunction_WithBaseCall_ReportsDiagnostic ()
  {
    const string text = @"
            public class TestClass
            {
                public void TestMethod()
                {
                    void LocalFunction()
                    {
                        base.ToString();
                    }
                    LocalFunction();
                }
            }";

    var expected = new[]
                   {
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(Rules.InLocalFunction)
                           .WithLocation(8, 25),
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(Rules.InNonOverridingMethod)
                           .WithLocation(8, 25)
                           .WithArguments("Test"),
                   };

    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task LocalFunction_WithBaseCallInConditional_ReportsDiagnostic ()
  {
    const string text = @"
            public class TestClass
            {
                public void TestMethod()
                {
                    void LocalFunction()
                    {
                        if (true)
                        {
                            base.ToString();
                        }
                    }
                    LocalFunction();
                }
            }";


    var expected = new[]
                   {
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(Rules.InLocalFunction)
                           .WithLocation(10, 29),
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(Rules.InNonOverridingMethod)
                           .WithLocation(10, 29)
                           .WithArguments("Test"),
                   };

    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task NestedLocalFunctions_WithBaseCallInInner_ReportsTwoDiagnostics ()
  {
    const string text = @"
        public class TestClass
        {
            public void TestMethod()
            {
                void OuterFunction()
                {
                    void InnerFunction()
                    {
                        base.ToString();
                    }
                    InnerFunction();
                }
                OuterFunction();
            }
        }";

    var expected = new[]
                   {
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(Rules.InLocalFunction)
                           .WithLocation(10, 25),
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(Rules.InLocalFunction)
                           .WithLocation(10, 25),
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(Rules.InNonOverridingMethod)
                           .WithLocation(10, 25),
                   };

    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task LocalFunction_WithBaseCallInLoop_ReportsDiagnostic ()
  {
    const string text = @"
            public class TestClass
            {
                public void TestMethod()
                {
                    void LocalFunction()
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            base.ToString();
                        }
                    }
                    LocalFunction();
                }
            }";

    var expected = new[]
                   {
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(Rules.InLocalFunction)
                           .WithLocation(10, 29),
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(Rules.InNonOverridingMethod)
                           .WithLocation(10, 29)
                           .WithArguments("Test"),
                   };

    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task MultipleLocalFunctions_OnlyOneWithBaseCall_ReportsOneDiagnostic ()
  {
    const string text = @"
            public class TestClass
            {
                public void TestMethod()
                {
                    void LocalFunction1()
                    {
                        System.Console.WriteLine(""No base call here"");
                    }

                    void LocalFunction2()
                    {
                        base.ToString();
                    }

                    LocalFunction1();
                    LocalFunction2();
                }
            }";

    var expected = new[]
                   {
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(Rules.InLocalFunction)
                           .WithLocation(13, 25),
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(Rules.InNonOverridingMethod)
                           .WithLocation(13, 25)
                           .WithArguments("Test"),
                   };

    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }
}