// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier: MIT

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Remotion.Infrastructure.Analyzers.BaseCalls.UnitTests.Utilities;
using Xunit;

namespace Remotion.Infrastructure.Analyzers.BaseCalls.UnitTests.ControlFlowTests;

public class TryCatchTests
{
  [Fact]
  public async Task BaseCallInTryBlock_ReportsTryDiagnostic ()
  {
    const string text = @"
        using Remotion.Infrastructure.Analyzers.BaseCalls;
        using Remotion.Infrastructure.Analyzers.BaseCalls.Attribute;
        public abstract class BaseClass
        {
            [BaseCallCheck(BaseCall.IsMandatory)]
            public virtual void Test() { }
        }
        
        public class DerivedClass : BaseClass
        {
            public override void Test()
            {
                try
                {
                    base.Test(); // Base call in try block
                }
                catch
                {
                    // Empty catch
                }
            }
        }";

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.InTryOrCatch)
        .WithLocation(16, 21)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task BaseCallInCatchBlock_ReportsCatchDiagnostic ()
  {
    const string text = @"
        using Remotion.Infrastructure.Analyzers.BaseCalls;
        using System;
        using Remotion.Infrastructure.Analyzers.BaseCalls.Attribute;
        public abstract class BaseClass
        {
            [BaseCallCheck(BaseCall.IsMandatory)]
            public virtual void Test() { }
        }
        
        public class DerivedClass : BaseClass
        {
            public override void Test()
            {
                try
                {
                    throw new Exception();
                }
                catch
                {
                    base.Test(); // Base call in catch block
                }
            }
        }";

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.InTryOrCatch)
        .WithLocation(21, 21)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task BaseCallInFinallyBlock_ReportsNothing ()
  {
    const string text = @"
        using Remotion.Infrastructure.Analyzers.BaseCalls;
        using Remotion.Infrastructure.Analyzers.BaseCalls.Attribute;
        public abstract class BaseClass
        {
            [BaseCallCheck(BaseCall.IsMandatory)]
            public virtual void Test() { }
        }
        
        public class DerivedClass : BaseClass
        {
            public override void Test()
            {
                try
                {
                    // Empty try
                }
                catch
                {
                    // Empty catch
                }
                finally
                {
                    base.Test(); // Base call in finally block
                }
            }
        }";

    var expected = DiagnosticResult.EmptyDiagnosticResults;
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task BaseCallOutsideTryCatch_ReportsNothing ()
  {
    const string text = @"
        using Remotion.Infrastructure.Analyzers.BaseCalls;
        using Remotion.Infrastructure.Analyzers.BaseCalls.Attribute;
        public abstract class BaseClass
        {
            [BaseCallCheck(BaseCall.IsMandatory)]
            public virtual void Test() { }
        }
        
        public class DerivedClass : BaseClass
        {
            public override void Test()
            {
                try
                {
                    // Empty try
                }
                catch
                {
                    // Empty catch
                }

                base.Test(); // Base call outside try-catch
            }
        }";

    var expected = DiagnosticResult.EmptyDiagnosticResults;
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task NoBaseCall_with_empty_TryCatch_ReportsDiagnostic ()
  {
    const string text = @"
        using Remotion.Infrastructure.Analyzers.BaseCalls;
        using Remotion.Infrastructure.Analyzers.BaseCalls.Attribute;
        public abstract class BaseClass
        {
            [BaseCallCheck(BaseCall.IsMandatory)]
            public virtual void Test() { }
        }
        
        public class DerivedClass : BaseClass
        {
            public override void Test()
            {
                try
                {
                    // Empty try
                }
                catch
                {
                    // Empty catch
                }

            }
        }";

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.NoBaseCall)
        .WithLocation(12, 34)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task BaseCallInNestedTryCatch_ReportsTryDiagnostic ()
  {
    const string text = @"
        using Remotion.Infrastructure.Analyzers.BaseCalls;
        using Remotion.Infrastructure.Analyzers.BaseCalls.Attribute;
        public abstract class BaseClass
        {
            [BaseCallCheck(BaseCall.IsMandatory)]
            public virtual void Test() { }
        }
        
        public class DerivedClass : BaseClass
        {
            public override void Test()
            {
                try
                {
                    try
                    {
                        base.Test(); // Base call in nested try block
                    }
                    catch
                    {
                        // Empty catch
                    }
                }
                catch
                {
                    // Empty catch
                }
            }
        }";

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.InTryOrCatch)
        .WithLocation(18, 25)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }
}