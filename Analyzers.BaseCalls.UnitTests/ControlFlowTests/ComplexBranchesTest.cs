// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier: MIT

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Remotion.Infrastructure.Analyzers.BaseCalls.UnitTests.Utilities;
using Xunit;

namespace Remotion.Infrastructure.Analyzers.BaseCalls.UnitTests.ControlFlowTests;

public class ComplexBranchesTest
{
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

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(Rules.NoBaseCall)
        .WithLocation(14, 26)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task MultipleBaseCalls_InDifferentBranches_ReportsDiagnostic ()
  {
    const string text = @"
                class BaseClass
                {
                    public virtual void Method() { return; }
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

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(Rules.MultipleBaseCalls)
        .WithLocation(19, 25)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
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

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(Rules.MultipleBaseCalls)
        .WithLocation(55, 7)
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

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(Rules.NoBaseCall)
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

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(Rules.NoBaseCall)
        .WithLocation(43, 9)
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
            public virtual void Test() { return; }
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

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(Rules.NoBaseCall)
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
            public virtual void Test() { return; }
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

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(Rules.MultipleBaseCalls)
        .WithLocation(19, 21)
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
  public async Task RecursiveMethodWithBaseCall_ReportsDiagnostic ()
  {
    const string text = @"
        using Remotion.Infrastructure.Analyzers.BaseCalls;
           
        public abstract class BaseClass
        {
            [BaseCallCheck(BaseCall.IsMandatory)]
            public virtual void Test(int depth) { return; }
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

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(Rules.NoBaseCall)
        .WithLocation(12, 34)
        .WithArguments("Test");
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

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(Rules.NoBaseCall)
        .WithLocation(39, 5)
        .WithArguments("Test");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }
}