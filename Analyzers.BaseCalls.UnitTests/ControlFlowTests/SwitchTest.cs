// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier: MIT

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Remotion.Infrastructure.Analyzers.BaseCalls.UnitTests.Utilities;
using Xunit;

namespace Remotion.Infrastructure.Analyzers.BaseCalls.UnitTests.ControlFlowTests;

public class SwitchTests
{
  [Fact]
  public async Task SwitchStatement_WithBaseCall_ReportsDiagnostic ()
  {
    const string text = @"
        using Remotion.Infrastructure.Analyzers.BaseCalls;
            
        public abstract class BaseClass
        {
            [BaseCallCheck(BaseCall.IsMandatory)]
            public virtual int Test(int value) { return value; }
        }
        
        public class DerivedClass : BaseClass
        {
            public override int Test(int value)
            {
                switch (value)
                {
                    case 0:
                        return base.Test(0);
                    case 1:
                        return 5;
                    default:
                        return 8;
                }
            }
        }";

    var expected = new[]
                   {
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(Rules.NoBaseCall)
                           .WithLocation(12, 33)
                           .WithArguments("Test"),
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(Rules.InSwitch)
                           .WithLocation(17, 32)
                           .WithArguments("Test")
                   };
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task SwitchStatement_WithBaseCallOutsideSwitch_ReportsNothing ()
  {
    const string text = @"
        using Remotion.Infrastructure.Analyzers.BaseCalls;
            
        public abstract class BaseClass
        {
            [BaseCallCheck(BaseCall.IsMandatory)]
            public virtual int Test(int value) { return value; }
        }
        
        public class DerivedClass : BaseClass
        {
            public override int Test(int value)
            {
                int result = base.Test(value);
                switch (value)
                {
                    case 0:
                        result *= 2;
                        break;
                    case 1:
                        result += 1;
                        break;
                    default:
                        result -= 1;
                        break;
                }
                return result;
            }
        }";

    var expected = DiagnosticResult.EmptyDiagnosticResults;
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task BaseCallInSwitchExpression_ReportsDiagnostic ()
  {
    const string text = @"
            using Remotion.Infrastructure.Analyzers.BaseCalls;
                
            public abstract class BaseClass
            {
                [BaseCallCheck(BaseCall.IsMandatory)]
                public virtual int Test(int value) => value;
            }
            
            public class DerivedClass : BaseClass
            {
                public override int Test(int value) =>
                    value switch
                    {
                        0 => 2,
                        1 => base.Test(1),
                        _ => 3
                    };
            }";

    var expected = new[]
                   {
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(Rules.NoBaseCall)
                           .WithLocation(12, 37)
                           .WithArguments("Test"),
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(Rules.InSwitch)
                           .WithLocation(16, 30)
                           .WithArguments("Test"),
                   };
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }
}