// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier: MIT
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Remotion.Infrastructure.Analyzers.BaseCalls.UnitTests.Utilities;
using Xunit;

namespace Remotion.Infrastructure.Analyzers.BaseCalls.UnitTests;

public class AbstractBaseMethodTest
{
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
          public abstract void x ();
        }
                
        public class DerivedClass : BaseClass
        {

          public override void x ()
          {
            //base.x();
          }
        }";

    var expected = DiagnosticResult.EmptyDiagnosticResults;
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task NoBaseCall_Overriding_Abstract_Method_With_Generics_ReportsNothing ()
  {
    const string text = @"
        using System;
        using System.Runtime.InteropServices.ComTypes;
        using Remotion.Infrastructure.Analyzers.BaseCalls;

                
        namespace ConsoleApp1;
                
        public abstract class BaseClass<T>
        {
          public abstract void x (T a);
        }
                
        public class DerivedClass : BaseClass<string>
        {

          public override void x (string a)
          {
            //base.x();
          }
        }";

    var expected = DiagnosticResult.EmptyDiagnosticResults;
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task NoBaseCall_Overriding_Abstract_Method_With_Complex_Generics_ReportsNothing ()
  {
    const string text = @"
        using System;
        using System.Runtime.InteropServices.ComTypes;
        using Remotion.Infrastructure.Analyzers.BaseCalls;
using System.Collections.Generic;

                
        namespace ConsoleApp1;
                
        public abstract class BaseClass<T>
        {
          public abstract void x (List<T> a);
        }
                
        public class DerivedClass : BaseClass<string>
        {

          public override void x (List<string> a)
          {
            //base.x();
          }
        }";

    var expected = DiagnosticResult.EmptyDiagnosticResults;
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }
}