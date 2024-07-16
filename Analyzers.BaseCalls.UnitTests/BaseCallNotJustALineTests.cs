// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier: MIT

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

namespace Remotion.Infrastructure.Analyzers.BaseCalls.UnitTests;

public class BaseCallNotJustALineTests
{
  [Fact]
  public async Task BaseCallInTernaryOperator_ReportsNothing ()
  {
    const string text = @"
        using Remotion.Infrastructure.Analyzers.BaseCalls;
        
        public abstract class BaseClass
        {
            [BaseCallCheck(BaseCall.IsMandatory)]
            public virtual int Test(bool condition) { return 0; }
        }
        
        public class DerivedClass : BaseClass
        {
            public override int Test(bool condition)
            {
                int result = condition ? base.Test(true) : base.Test(false);
                return result;
            }
        }";

    var expected = DiagnosticResult.EmptyDiagnosticResults;
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task BaseCallInAssignmentWithOperator_ReportsNothing ()
  {
    const string text = @"
        using Remotion.Infrastructure.Analyzers.BaseCalls;
        
        public abstract class BaseClass
        {
            [BaseCallCheck(BaseCall.IsMandatory)]
            public virtual int Test() { return 0; }
        }
        
        public class DerivedClass : BaseClass
        {
            public override int Test()
            {
                int result = 0;
                result += base.Test() * 2;
                return result;
            }
        }";

    var expected = DiagnosticResult.EmptyDiagnosticResults;
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task BaseCallInComplexMathExpression_ReportsNothing ()
  {
    const string text = @"
        using Remotion.Infrastructure.Analyzers.BaseCalls;
        using System;
        
        public abstract class BaseClass
        {
            [BaseCallCheck(BaseCall.IsMandatory)]
            public virtual int Test(int value) { return value; }
        }
        
        public class DerivedClass : BaseClass
        {
            public override int Test(int value)
            {
                int result = Math.Max(base.Test(value), base.Test(value + 1)) + ((value > 0) ? base.Test(value - 1) : 0);
                return result;
            }
        }";

    var expected = DiagnosticResult.EmptyDiagnosticResults;
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task BaseCallInStringInterpolation_ReportsNothing ()
  {
    const string text = @"
        using Remotion.Infrastructure.Analyzers.BaseCalls;
        
        public abstract class BaseClass
        {
            [BaseCallCheck(BaseCall.IsMandatory)]
            public virtual string Test(int value) { return value.ToString(); }
        }
        
        public class DerivedClass : BaseClass
        {
            public override string Test(int value)
            {
                string result = $""The base value is {base.Test(value)}, and doubled is {base.Test(value * 2)}"";
                return result;
            }
        }";

    var expected = DiagnosticResult.EmptyDiagnosticResults;
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task BaseCallInReturn_ReportsNothing ()
  {
    const string text = @"
        using Remotion.Infrastructure.Analyzers.BaseCalls;
        
        public abstract class BaseClass
        {
            [BaseCallCheck(BaseCall.IsMandatory)]
            public virtual int Test(bool condition) { return 0; }
        }
        
        public class DerivedClass : BaseClass
        {
            public override int Test(bool condition)
            {
                return base.Test(true);
            }
        }";

    var expected = DiagnosticResult.EmptyDiagnosticResults;
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task BaseCallInReturn2_ReportsNothing ()
  {
    const string text = @"
        using Remotion.Infrastructure.Analyzers.BaseCalls;
        using System;
        
        public abstract class BaseClass
        {
            [BaseCallCheck(BaseCall.IsMandatory)]
            public virtual int Test(int value) { return value; }
        }
        
        public class DerivedClass : BaseClass
        {
            public override int Test(int value)
            {
                return Math.Max(base.Test(value), base.Test(value + 1)) + ((value > 0) ? base.Test(value - 1) : 0);
            }
        }";

    var expected = DiagnosticResult.EmptyDiagnosticResults;
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task Multiple_BaseCallsInReturn_ReportsDiagnostic ()
  {
    const string text = @"
        using Remotion.Infrastructure.Analyzers.BaseCalls;
        
        public abstract class BaseClass
        {
            [BaseCallCheck(BaseCall.IsMandatory)]
            public virtual int Test(bool condition) { return 0; }
        }
        
        public class DerivedClass : BaseClass
        {
            public override int Test(bool condition)
            {
                base.Test(false);
                return base.Test(true);
            }
        }";

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionMultipleBaseCalls)
        .WithLocation(12, 33)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }
}