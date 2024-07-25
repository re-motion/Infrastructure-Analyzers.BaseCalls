// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier: MIT

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Remotion.Infrastructure.Analyzers.BaseCalls.UnitTests.Utilities;
using Xunit;

namespace Remotion.Infrastructure.Analyzers.BaseCalls.UnitTests.DisallowedBaseCallUsagesTests;

public class BaseCallInNonOverridingMethodTest
{
  [Fact]
  public async Task NoOverrideMethod_ReportsNothing ()
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
            public void Test2 ()
            {
                int b = 7;
                //base.Test();
            }
        }
        """;

    var expected = DiagnosticResult.EmptyDiagnosticResults;
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task BaseCallInNonOverridingMethod_ReportsDiagnostic ()
  {
    const string text = @"
            using Remotion.Infrastructure.Analyzers.BaseCalls;
            
            public abstract class BaseClass
            {
                public virtual void Test() { }
            }
            
            public class DerivedClass : BaseClass
            {
                public void NonOverridingMethod()
                {
                    base.Test();
                }
            }";

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(Rules.InNonOverridingMethod)
        .WithLocation(13, 21)
        .WithArguments("NonOverridingMethod");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }
}