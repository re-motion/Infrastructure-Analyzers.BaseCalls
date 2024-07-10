// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier: MIT

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

namespace Remotion.Infrastructure.Analyzers.BaseCalls.UnitTests;

public class BaseCallTest
{
  [Fact]
  public async Task BaseCall_ReportsNothing ()
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

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionNoBaseCallFound)
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
    
    var expected = new DiagnosticResult[]
                   {
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionNoBaseCallFound)
                           .WithLocation(18, 5)
                           .WithArguments("Test"),

                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionWrongBaseCall)
                           .WithLocation(21, 9)
                           .WithArguments("Test")
                   };
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


    var expected = new DiagnosticResult[]
                   {
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionNoBaseCallFound)
                           .WithLocation(18, 5)
                           .WithArguments("Test"),

                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionWrongBaseCall)
                           .WithLocation(21, 9)
                           .WithArguments("Test")
                   };
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task BaseCall_with_correct_param_datatypes_ReportsNothing ()
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
                string b = "";
                base.Test(b); // not a baseCall
            }
        }

        """;

    var expected = DiagnosticResult.EmptyDiagnosticResults;
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
    
    var expected = new DiagnosticResult[]
                   {
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionNoBaseCallFound)
                           .WithLocation(18, 5)
                           .WithArguments("Test"),

                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionWrongBaseCall)
                           .WithLocation(21, 9)
                           .WithArguments("Test")
                   };
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
  public async Task NoBaseCall_WithBaseCallOptional_ReportsNothing ()
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
  public async Task NoBaseCall_WithBaseCallOptional_Multi_Gen_Inheritance_ReportsNothing ()
  {
    const string text =
        """
        using Remotion.Infrastructure.Analyzers.BaseCalls;

        public abstract class BaseClass
        {
            [BaseCallCheck(BaseCall.IsOptional)]
            public virtual void Test()
            {
                int a = 5;
            }
        }
        public class DerivedClass : BaseClass
        {
            public override void Test()
            {
                int b = 2;
            }
        }

        public class SecondDerivedClass : DerivedClass
        {
            public override void Test()
            {
                int c = 7;
                //base.Test();
            }
        }
        """;
    var expected = DiagnosticResult.EmptyDiagnosticResults;
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task NoBaseCall_WithBaseCall_Mandatory_Multi_Gen_Inheritance_ReportsDiagnostic ()
  {
    const string text =
        """
        using Remotion.Infrastructure.Analyzers.BaseCalls;

        public abstract class BaseClass
        {
            [BaseCallCheck(BaseCall.IsMandatory)]
            public virtual void Test()
            {
                int a = 5;
            }
        }
        public class DerivedClass : BaseClass
        {
            public override void Test()
            {
                int b = 2;
                base.Test();
            }
        }

        public class SecondDerivedClass : DerivedClass
        {
            public override void Test()
            {
                int c = 7;
                //base.Test();
            }
        }
        """;
    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionNoBaseCallFound)
        .WithLocation(22, 5)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task NoBaseCall_with_IgnoreBaseCall_ReportsNothing ()
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

  [Fact]
  public async Task BaseCall_In_If_ReportsNothing ()
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
                if (b == 6)
                  if (b==9)
                  {
                    base.Test();
                  }
                  else
                    base.Test();
                else
                  base.Test();
            }
        }
        """;

    var expected = DiagnosticResult.EmptyDiagnosticResults;
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task BaseCall_and_If_ReportsNothing ()
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
                if (b == 6)
                  _ = "";
                base.Test();
            }
        }
        """;

    var expected = DiagnosticResult.EmptyDiagnosticResults;
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task NoBaseCall_In_If_ReportsDiagnostic ()
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
                if (b == 6)
                  if (b==9)
                    base.Test();
                  else
                    _ = "";
                else
                  base.Test();
            }
        }
        """;

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionNoBaseCallFound)
        .WithLocation(14, 5)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task BaseCall_In_loop_ReportsLoopDiagnostic ()
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
              for (int i = 0; i < 10; i++) {
                  for (int j = 0; j < 10; j++) {
                    while(true){
                      base.Test();
                    }
                  }
              }
            }
        }
        """;

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionBaseCallFoundInLoop)
        .WithLocation(14, 5)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task NoBaseCall_In_loop_ReportsDiagnostic ()
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
              for (int i = 0; i < 10; i++) {
                  for (int j = 0; j < 10; j++) {
                    while(true){
                      //base.Test();
                    }
                  }
              }
            }
        }
        """;

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionNoBaseCallFound)
        .WithLocation(14, 5)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task BaseCall_In_Loop_In_If_ReportsLoopDiagnostic ()
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
                if (b == 6)
                  if (b==9){
                    while(true)
                    {
                      base.Test();
                    }
                  }
                  else
                    base.Test();
                else
                  base.Test();
            }
        }
        """;

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionBaseCallFoundInLoop)
        .WithLocation(14, 5)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task MultipleBaseCalls_ReportsDiagnostic ()
  {
    const string text = @"
                class BaseClass
                {
                    public virtual void Method() { }
                }

                class DerivedClass : BaseClass
                {
                    public override void {|#0:Method|}()
                    {
                        base.Method();
                        base.Method();

                    }
                }";

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionMultipleBaseCalls)
        .WithLocation(9, 21)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task MultipleBaseCalls_InDifferentBranches_ReportsDiagnostic ()
  {
    const string text = @"
                class BaseClass
                {
                    public virtual void Method() { }
                }

                class DerivedClass : BaseClass
                {
                    public override void {|#0:Method|}()
                    {
                        if (true)
                        {
                            base.Method();
                        }
                        else
                        {
                            base.Method();
                        }
                        base.Method();
                    }
                }";

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionMultipleBaseCalls)
        .WithLocation(9, 21)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact(Skip = "test does not work")]
  //[Obsolete("Obsolete")]
  public async Task YourCodeFix_FixesIssue ()
  {
    var test = new CSharpCodeFixTest<BaseCallAnalyzer, BaseCallCodeFixProvider, XUnitVerifier>
               {
                   TestCode = @"
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
}",
                   FixedCode = @"
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
    [IgnoreBaseCallCheck]
    public override void Test ()
    {
        int b = 7;
        //base.Test();
    }
}",
               };

    test.ExpectedDiagnostics.Add(
        CSharpCodeFixVerifier<BaseCallAnalyzer, BaseCallCodeFixProvider, XUnitVerifier>
            .Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionNoBaseCallFound)
            .WithLocation(14, 5)
            .WithArguments("Test"));


    await test.RunAsync();
  }
}