// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier: MIT

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Remotion.Infrastructure.Analyzers.BaseCalls.UnitTests.Utilities;
using Xunit;

namespace Remotion.Infrastructure.Analyzers.BaseCalls.UnitTests.ControlFlowTest;

public class SwitchTests
{
  [Fact]
  public async Task SwitchStatement_WithBaseCallInEachCase_ReportsNothing ()
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
                        return base.Test(1);
                    default:
                        return base.Test(value * 2);
                }
            }
        }";

    var expected = DiagnosticResult.EmptyDiagnosticResults;
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task SwitchStatement_WithBaseCallInSomeButNotAllCases_ReportsDiagnostic ()
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
                        return value + 1; // Missing base call
                    default:
                        return base.Test(value * 2);
                }
            }
        }";

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.NoBaseCall)
        .WithLocation(14, 17)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task SwitchExpression_WithBaseCallInEachArm_ReportsNothing ()
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
                return value switch
                {
                    0 => base.Test(0),
                    1 => base.Test(1),
                    _ => base.Test(value * 2)
                };
            }
        }";

    var expected = DiagnosticResult.EmptyDiagnosticResults;
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
  public async Task BaseCallInSwitchStatement_ReportsNothing ()
  {
    const string text = @"
            using Remotion.Infrastructure.Analyzers.BaseCalls;
            
            public abstract class BaseClass
            {
                [BaseCallCheck(BaseCall.IsMandatory)]
                public virtual void Test(int value) { }
            }
            
            public class DerivedClass : BaseClass
            {
                public override void Test(int value)
                {
                    switch (value)
                    {
                        case 0:
                            base.Test(0);
                            break;
                        case 1:
                            base.Test(1);
                            break;
                        default:
                            base.Test(value);
                            break;
                    }
                }
            }";

    var expected = DiagnosticResult.EmptyDiagnosticResults;
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task BaseCallInSwitchExpression_ReportsNothing ()
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
                        0 => base.Test(0),
                        1 => base.Test(1),
                        _ => base.Test(value)
                    };
            }";

    var expected = DiagnosticResult.EmptyDiagnosticResults;
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }
}