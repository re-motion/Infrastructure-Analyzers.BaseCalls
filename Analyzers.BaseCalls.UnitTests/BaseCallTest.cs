﻿// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier: MIT

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Remotion.Infrastructure.Analyzers.BaseCalls.UnitTests;

public class BaseCallTest
{
  [Fact]
  public async Task Normal_BaseCall_ReportsNothing ()
  {
    const string text =
        """
        using Remotion.Infrastructure.Analyzers.BaseCalls;

        public abstract class BaseClass
        {
            //[BaseCallCheck(BaseCall.IsMandatory)]
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
                base.Test();
            }
        }
        """;

    var expected = DiagnosticResult.EmptyDiagnosticResults;
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }
  [Fact]
  public async Task NoBaseCall_WithBaseCallMandatory_ReportsDiagnostic ()
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
        .WithLocation(14, 5)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task BaseCall_with_wrong_name_ReportsDiagnostic ()
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
            public void WrongTest()
            {
              return;
            }
        }

        public class DerivedClass : BaseClass
        {
            public override void Test ()
            {
                int b = 7;
                base.WrongTest();
            }
        }

        """;

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.Rule)
        .WithLocation(18, 5)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }
  
  [Fact]
  public async Task BaseCall_with_wrong_param_datatypes_ReportsDiagnostic ()
  {
    const string text =
        """
        using Remotion.Infrastructure.Analyzers.BaseCalls;

        public abstract class BaseClass
        {
            [BaseCallCheck(BaseCall.IsMandatory)]
            public virtual void Test (string c)
            {
                int a = 5;
            }
            public void Test(int a)
            {
              return;
            }
        }

        public class DerivedClass : BaseClass
        {
            public override void Test (string c)
            {
                int b = 7;
                base.Test(b); // not a baseCall
            }
        }

        """;

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.Rule)
        .WithLocation(18, 5)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task BaseCall_with_wrong_number_of_params_ReportsDiagnostic ()
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
            public void Test(int a)
            {
              return;
            }
        }

        public class DerivedClass : BaseClass
        {
            public override void Test ()
            {
                int b = 7;
                base.Test(b);
            }
        }

        """;

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.Rule)
        .WithLocation(18, 5)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

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
  public async Task NoBaseCall_WithBaseCallOptional_ReportsNothing()
  {
    const string text =
      """
      using Remotion.Infrastructure.Analyzers.BaseCalls;

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
  
  [Fact]
  public async Task NoBaseCall_with_IgnoreBaseCall_ReportsNothing()
  {
    const string text =
        """
        using Remotion.Infrastructure.Analyzers.BaseCalls;

        public abstract class BaseClass
        {
            //[BaseCallCheck(BaseCall.IsOptional)]
            public virtual void Test ()
            {
                int a = 5;
            }
        }

        public class DerivedClass : BaseClass
        {
            [IgnoreBaseCallCheck]
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