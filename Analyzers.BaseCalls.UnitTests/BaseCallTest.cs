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
  public async Task BaseCall_In_Non_Overriding_Method_ReportsDiagnostic ()
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
            public void Test2 ()
            {
                int b = 7;
                base.Test();
            }
        }
        """;

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionInInNonOverridingMethod)
        .WithLocation(14, 17)
        .WithArguments("Test");
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
        .WithLocation(14, 26)
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

    var expected = new[]
                   {
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


    var expected = new[]
                   {
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

    var expected = new[]
                   {
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
        .WithLocation(22, 26)
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
        .WithLocation(14, 26)
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
        .WithLocation(14, 26)
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
        .WithLocation(14, 26)
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
        .WithLocation(14, 26)
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
        .WithLocation(9, 42)
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
        .WithLocation(9, 42)
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

  [Fact]
  public async Task IfsAndReturns_ReportsNothing ()
  {
    const string text =
        """
        using System;
        using System.Runtime.InteropServices.ComTypes;
        using Remotion.Infrastructure.Analyzers.BaseCalls;
                
        namespace ConsoleApp1;
                
        public abstract class BaseClass
        {
          [BaseCallCheck(BaseCall.IsMandatory)]
          public virtual void x()
          {
            int a = 5;
          }
        }
                
        public class DerivedClass : BaseClass
        {
          private bool condition1 = true;
          private bool condition2 = false;
          private bool condition3 = true;
          private bool condition4 = false;
                
          public override void x ()
          {
            if (condition1)
            {
              if (condition2)
              {
                base.x();
                return;
              }
              else
              {
                base.x(); 
                return;
              }
            }
            else
            {
              if (condition3)
              {
                base.x();
                return;
              }
              else if (condition4)
              {
                
              }
              else if (condition1)
              {
                base.x();
                return;
              }
                
              base.x();
            }
          }
        }
        """;

    var expected = DiagnosticResult.EmptyDiagnosticResults;
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task IfsAndReturns_ReportsMultipleDiagnostic ()
  {
    const string text =
        """
        using System;
        using System.Runtime.InteropServices.ComTypes;
        using Remotion.Infrastructure.Analyzers.BaseCalls;
                
        namespace ConsoleApp1;
                
        public abstract class BaseClass
        {
          [BaseCallCheck(BaseCall.IsMandatory)]
          public virtual void x()
          {
            int a = 5;
          }
        }
                
        public class DerivedClass : BaseClass
        {
          private bool condition1 = true;
          private bool condition2 = false;
          private bool condition3 = true;
          private bool condition4 = false;
                
          public override void x ()
          {
            if (condition1)
            {
              if (condition2)
              {
                base.x();
                return;
              }
              else
              {
                base.x(); 
                return;
              }
            }
            else
            {
              if (condition3)
              {
                base.x();
                return;
              }
              else if (condition4)
              {
                base.x(); //one too much
              }
              else if (condition1)
              {
                base.x();
                return;
              }
                
              base.x();
            }
          }
        }
        """;

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionMultipleBaseCalls)
        .WithLocation(23, 24)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task IfsAndReturns_ReportsMissingDiagnostic ()
  {
    const string text =
        """
        using System;
        using System.Runtime.InteropServices.ComTypes;
        using Remotion.Infrastructure.Analyzers.BaseCalls;
                
        namespace ConsoleApp1;
                
        public abstract class BaseClass
        {
          [BaseCallCheck(BaseCall.IsMandatory)]
          public virtual void x()
          {
            int a = 5;
          }
        }
                
        public class DerivedClass : BaseClass
        {
          private bool condition1 = true;
          private bool condition2 = false;
          private bool condition3 = true;
          private bool condition4 = false;
                
          public override void x ()
          {
            if (condition1)
            {
              if (condition2)
              {
                base.x();
                return;
              }
              else
              {
                base.x(); 
                return;
              }
            }
            else
            {
              if (condition3)
              {
                base.x();
                return;
              }
              else if (condition4)
              {
                //base Call Missing
              }
              else if (condition1)
              {
                base.x();
                return;
              }
            }
          }
        }
        """;

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionNoBaseCallFound)
        .WithLocation(23, 24)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task IfsAndReturns2_ReportsMissingDiagnostic ()
  {
    const string text =
        """
        using System;
        using System.Runtime.InteropServices.ComTypes;
        using Remotion.Infrastructure.Analyzers.BaseCalls;
                
        namespace ConsoleApp1;
                
        public abstract class BaseClass
        {
          [BaseCallCheck(BaseCall.IsMandatory)]
          public virtual void x()
          {
            int a = 5;
          }
        }
                
        public class DerivedClass : BaseClass
        {
          private bool condition1 = true;
          private bool condition2 = false;
          private bool condition3 = true;
          private bool condition4 = false;
                
          public override void x ()
          {
            if (condition1)
            {
              if (condition2)
              {
                base.x();
                return;
              }
              else
              {
                base.x(); 
                return;
              }
            }
            else
            {
              if (condition3)
              {
                //base.x; //base call missing
                return;
              }
              else if (condition4)
              {
              }
              else if (condition1)
              {
                base.x();
                return;
              }
              base.x();
            }
          }
        }
        """;

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionNoBaseCallFound)
        .WithLocation(23, 24)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task IfsAndReturns3_ReportsMissingNothing ()
  {
    const string text = @"
        using System;
using System.Runtime.InteropServices.ComTypes;
using Remotion.Infrastructure.Analyzers.BaseCalls;

                
namespace ConsoleApp1;
public abstract class BaseClass
{
  [BaseCallCheck(BaseCall.IsMandatory)]
  protected virtual int x ()
  {
    return 5;
  }
}
                
public class DerivedClass : BaseClass
{
  private const bool condition1 = false;
  private const bool condition2 = true;
  private const bool condition3 = false;

  protected override int x ()
  {
    Random random = new();
    
    if (random.Next() > 0.5)
    {
      base.x();
      return 2;
    }
    else if (random.Next() > 0.4)
    {
      base.x();
      return 0;
    }
    base.x();
    

    return 5;
  }
}";

    var expected = DiagnosticResult.EmptyDiagnosticResults;
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task NestedIfElseWithoutBaseCall_ReportsMissingDiagnostic ()
  {
    const string text = @"
        using Remotion.Infrastructure.Analyzers.BaseCalls;
        
        public abstract class BaseClass
        {
            [BaseCallCheck(BaseCall.IsMandatory)]
            public virtual void Test() { }
        }
        
        public class DerivedClass : BaseClass
        {
            private bool condition1 = true;
            private bool condition2 = false;
            
            public override void Test()
            {
                if (condition1)
                {
                    if (condition2)
                    {
                        // Missing base call
                    }
                    else
                    {
                        base.Test();
                    }
                }
                else
                {
                    base.Test();
                }
            }
        }";

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionNoBaseCallFound)
        .WithLocation(15, 34)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task MultipleBaseCallsInSameBranch_ReportsMultipleDiagnostic ()
  {
    const string text = @"
        using Remotion.Infrastructure.Analyzers.BaseCalls;
        
        public abstract class BaseClass
        {
            [BaseCallCheck(BaseCall.IsMandatory)]
            public virtual void Test() { }
        }
        
        public class DerivedClass : BaseClass
        {
            private bool condition = true;
            
            public override void Test()
            {
                if (condition)
                {
                    base.Test();
                    base.Test(); // Duplicate base call
                }
                else
                {
                    base.Test();
                }
            }
        }";

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionMultipleBaseCalls)
        .WithLocation(14, 34)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task BaseCallInLoop_ReportsLoopDiagnostic ()
  {
    const string text = @"
        using Remotion.Infrastructure.Analyzers.BaseCalls;
        
        public abstract class BaseClass
        {
            [BaseCallCheck(BaseCall.IsMandatory)]
            public virtual void Test() { }
        }
        
        public class DerivedClass : BaseClass
        {
            public override void Test()
            {
                for (int i = 0; i < 5; i++)
                {
                    base.Test(); // Base call in loop
                }
            }
        }";

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionBaseCallFoundInLoop)
        .WithLocation(12, 34)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task ComplexNestedStructure_ReportsNothing ()
  {
    const string text = @"
        using Remotion.Infrastructure.Analyzers.BaseCalls;
        
        public abstract class BaseClass
        {
            [BaseCallCheck(BaseCall.IsMandatory)]
            public virtual void Test() { }
        }
        
        public class DerivedClass : BaseClass
        {
            private bool condition1 = true;
            private bool condition2 = false;
            private bool condition3 = true;
            
            public override void Test()
            {
                if (condition1)
                {
                    if (condition2)
                    {
                        base.Test();
                    }
                    else if (condition3)
                    {
                        if (condition1 && condition2)
                        {
                            base.Test();
                        }
                        else
                        {
                            base.Test();
                        }
                    }
                    else
                    {
                        base.Test();
                    }
                }
                else
                {
                    base.Test();
                }
            }
        }";

    var expected = DiagnosticResult.EmptyDiagnosticResults;
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task NestedLoopsWithConditionalBaseCall_ReportsDiagnostic ()
  {
    const string text = @"
        using Remotion.Infrastructure.Analyzers.BaseCalls;
        
        public abstract class BaseClass
        {
            [BaseCallCheck(BaseCall.IsMandatory)]
            public virtual void Test() { }
        }
        
        public class DerivedClass : BaseClass
        {
            private bool condition = true;
            
            public override void Test()
            {
                for (int i = 0; i < 5; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        if (i == 2 && j == 1)
                        {
                            base.Test();
                            return;
                        }
                    }
                }
                
                if (!condition)
                {
                    base.Test();
                }
                else
                {
                    base.Test();
                }
            }
        }";

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionBaseCallFoundInLoop)
        .WithLocation(14, 34)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task RecursiveMethodWithBaseCall_ReportsDiagnostic ()
  {
    const string text = @"
        using Remotion.Infrastructure.Analyzers.BaseCalls;
        
        public abstract class BaseClass
        {
            [BaseCallCheck(BaseCall.IsMandatory)]
            public virtual void Test(int depth) { }
        }
        
        public class DerivedClass : BaseClass
        {
            public override void Test(int depth)
            {
                if (depth > 0)
                {
                    Test(depth - 1);
                }
                else
                {
                    // Base call missing
                }
            }
        }";

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionNoBaseCallFound)
        .WithLocation(12, 34)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task BaseCallInTryBlock_ReportsTryDiagnostic ()
  {
    const string text = @"
        using Remotion.Infrastructure.Analyzers.BaseCalls;
        
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

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionInTryOrCatch)
        .WithLocation(12, 34)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task BaseCallInCatchBlock_ReportsCatchDiagnostic ()
  {
    const string text = @"
        using Remotion.Infrastructure.Analyzers.BaseCalls;
        using System;
        
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

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionInTryOrCatch)
        .WithLocation(13, 34)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task BaseCallInFinallyBlock_ReportsNothing ()
  {
    const string text = @"
        using Remotion.Infrastructure.Analyzers.BaseCalls;
        
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

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionNoBaseCallFound)
        .WithLocation(12, 34)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task BaseCallInNestedTryCatch_ReportsTryDiagnostic ()
  {
    const string text = @"
        using Remotion.Infrastructure.Analyzers.BaseCalls;
        
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

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionInTryOrCatch)
        .WithLocation(12, 34)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

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

  [Fact]
  public async Task NoBaseCall_Overriding_Abstract_Method_ReportsNothing ()
  {
    const string text = @"
        using System;
        using System.Runtime.InteropServices.ComTypes;
        using Remotion.Infrastructure.Analyzers.BaseCalls;

                
        namespace ConsoleApp1;
                
        public abstract class BaseClass
        {
          public abstract int x ();
        }
                
        public class DerivedClass : BaseClass
        {

          public override int x ()
          {

            //base.x();
            return 5;
          }
        }";

    var expected = DiagnosticResult.EmptyDiagnosticResults;
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task IfBranch_WithNoElse_ReportsDiagnostic ()
  {
    const string text = @"
        using System;
using System.Runtime.InteropServices.ComTypes;
using Remotion.Infrastructure.Analyzers.BaseCalls;


namespace ConsoleApp1;

public abstract class BaseClass
{
  [BaseCallCheck(BaseCall.IsMandatory)]
  protected virtual int x ()
  {
    return 5;
  }
}

public class DerivedClass : BaseClass
{
  private const bool condition1 = false;
  private const bool condition2 = true;
  private const bool condition3 = false;

  protected override int x ()
  {
    Random random = new();

    if (random.Next() > 0)
    {
      base.x();
    }
    else if (random.Next() > 0)
    {
      base.x();
    }

    //base.x();

    return 5;
  }
}";

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionNoBaseCallFound)
        .WithLocation(24, 26)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

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

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionNoBaseCallFound)
        .WithLocation(12, 33)
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

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionInInNonOverridingMethod)
        .WithLocation(11, 29)
        .WithArguments("NonOverridingMethod");
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

  [Fact]
  public async Task MixinNormalNextCall ()
  {
    const string text = @"
using System;
using Remotion.Mixins;

[assembly:Mix(typeof(MixTarget), typeof(DerivedClass))]

public interface IParent
{
  void OverridableMethod ();
}


public class DerivedClass : Mixin<System.Object, IParent> {
  [OverrideTarget]
  public void OverridableMethod ()
  {
    Console.WriteLine(""test1"");
    Next.OverridableMethod();
  }
}

public class MixTarget : System.Object
{
  public void CallMethod ()
  {
    OverridableMethod();
  }
  protected virtual void OverridableMethod ()
  {
    Console.WriteLine(""test2"");
  }
}
";

    var expected = DiagnosticResult.EmptyDiagnosticResults;
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task MixinNoNextCall ()
  {
    const string text = @"
using System;
using Remotion.Mixins;

[assembly:Mix(typeof(MixTarget), typeof(DerivedClass))]

public interface IParent
{
  void OverridableMethod ();
}


public class DerivedClass : Mixin<System.Object, IParent> {
  [OverrideTarget]
  public void OverridableMethod ()
  {
    Console.WriteLine(""test1"");
    //Next.OverridableMethod();
  }
}

public class MixTarget : System.Object
{
  public void CallMethod ()
  {
    OverridableMethod();
  }
  protected virtual void OverridableMethod ()
  {
    Console.WriteLine(""test2"");
  }
}
";

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionNoBaseCallFound)
        .WithLocation(15, 15)
        .WithArguments("NonOverridingMethod");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task MixinWrongNextCall ()
  {
    const string text = @"
using System;
using ConsoleApp1;
using Remotion.Mixins;

[assembly:Mix(typeof(MixTarget), typeof(DerivedClass))]

namespace ConsoleApp1;


public interface IParent
{
  void OverridableMethod ();
  void OverridableMethod2 ();
}


public class DerivedClass : Mixin<System.Object, IParent>
{
  
  [OverrideTarget]
  public void OverridableMethod ()
  {
    Console.WriteLine(""test1"");
    Next.OverridableMethod2();
  }
}

public class MixTarget : System.Object
{
  public void CallMethod ()
  {
    OverridableMethod();
  }
  protected virtual void OverridableMethod2 ()
  {
    Console.WriteLine(""test2"");
  }
  protected virtual void OverridableMethod ()
  {
    Console.WriteLine(""test3"");
  }
}

";

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.DiagnosticDescriptionWrongBaseCall)
        .WithLocation(25, 5)
        .WithArguments("NonOverridingMethod");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }
}