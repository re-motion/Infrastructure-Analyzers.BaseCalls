// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier: MIT

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Remotion.Infrastructure.Analyzers.BaseCalls.UnitTests.Utilities;
using Xunit;

namespace Remotion.Infrastructure.Analyzers.BaseCalls.UnitTests.ControlFlowTests;

public class ThrowTest
{
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
                throw new Exception();
              }
              else
              {
                base.x(); 
                throw new Exception();
              }
            }
            else
            {
              if (condition3)
              {
                base.x();
                throw new Exception();
              }
              else if (condition4)
              {
                
              }
              else if (condition1)
              {
                base.x();
                throw new Exception();
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
                throw new Exception();
              }
              else
              {
                base.x(); 
                throw new Exception();
              }
            }
            else
            {
              if (condition3)
              {
                base.x();
                throw new Exception();
              }
              else if (condition4)
              {
                base.x(); //one too much
              }
              else if (condition1)
              {
                base.x();
                throw new Exception();
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
                throw new Exception();
              }
              else
              {
                base.x(); 
                throw new Exception();
              }
            }
            else
            {
              if (condition3)
              {
                base.x();
                throw new Exception();
              }
              else if (condition4)
              {
                //base Call Missing
              }
              else if (condition1)
              {
                base.x();
                throw new Exception();
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
                throw new Exception();
              }
              else
              {
                base.x(); 
                throw new Exception();
              }
            }
            else
            {
              if (condition3)
              {
                //base.x; //base call missing
                throw new Exception();
              }
              else if (condition4)
              {
              }
              else if (condition1)
              {
                base.x();
                throw new Exception();
              }
              base.x();
            }
          }
        }
        """;

    var expected = DiagnosticResult.EmptyDiagnosticResults;
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }
}