// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier: MIT
using System;
using System.Threading.Tasks;
using Remotion.Infrastructure.Analyzers.BaseCalls.UnitTests.Utilities;
using Xunit;

namespace Remotion.Infrastructure.Analyzers.BaseCalls.UnitTests.DisallowedBaseCallUsagesTests;

public class AnonymousMethodsTests
{
  [Fact]
  public async Task AnonymousMethod_WithoutBaseCall_ReportsNothing ()
  {
    const string text = @"
using System;
using Remotion.Infrastructure.Analyzers.BaseCalls;
     
namespace ConsoleApp1;
        
public abstract class BaseClass
{
  [BaseCallCheck(BaseCall.IsMandatory)]
  public virtual void Test ()
  {
    return;
  }
}
public class DerivedClass : BaseClass
{
  public override void Test()
  {
    var action = delegate()
    {
        Console.WriteLine(""Hello, World!"");
    };
    base.Test();
  }
}";

    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text);
  }

  [Fact]
  public async Task AnonymousMethod_WithBaseCall_ReportsDiagnostic ()
  {
    const string text = @"
using Remotion.Infrastructure.Analyzers.BaseCalls;
     
namespace ConsoleApp1;
        
public abstract class BaseClass
{
  [BaseCallCheck(BaseCall.IsMandatory)]
  public virtual void Test ()
  {
    return;
  }
}
public class DerivedClass : BaseClass
{
  public override void Test()
  {
    var action = delegate()
    {
      base.Test();
    };
    action();
  }
}";

    var expected = new[]
                   {
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(Rules.InAnonymousMethod)
                           .WithLocation(20, 7), 
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(Rules.NoBaseCall)
                           .WithLocation(16, 24) 
                   };

    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task SimpleLambda_WithoutBaseCall_ReportsDiagnostic ()
  {
    const string text = @"
using Remotion.Infrastructure.Analyzers.BaseCalls;
using System;
     
namespace ConsoleApp1;
        
public abstract class BaseClass
{
  [BaseCallCheck(BaseCall.IsMandatory)]
  public virtual void Test ()
  {
    return;
  }
}
public class DerivedClass : BaseClass
{
  public override void Test()
  {
    Action<int> action = x => Console.WriteLine(x);
  }
}";

    var expected = new[]
                   {
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(Rules.NoBaseCall)
                           .WithLocation(17, 24)
                   };

    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task SimpleLambda_WithBaseCall_ReportsDiagnostic ()
  {
    const string text = @"
using System;
using Remotion.Infrastructure.Analyzers.BaseCalls;
     
namespace ConsoleApp1;
        
public abstract class BaseClass
{
  [BaseCallCheck(BaseCall.IsMandatory)]
  public virtual void Test ()
  {
    return;
  }
}
public class DerivedClass : BaseClass
{
  public override void Test()
  {
    Action<int> action = x => base.Test();
  }
}";


    var expected = new[]
                   {
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(Rules.NoBaseCall)
                           .WithLocation(17, 24),
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(Rules.InAnonymousMethod)
                           .WithLocation(19, 31),
                   };

    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task ParenthesizedLambda_WithoutBaseCall_ReportsNothing ()
  {
    const string text = @"
using Remotion.Infrastructure.Analyzers.BaseCalls;
using System;
     
namespace ConsoleApp1;
        
public abstract class BaseClass
{
  [BaseCallCheck(BaseCall.IsMandatory)]
  public virtual void Test ()
  {
    return;
  }
}
public class DerivedClass : BaseClass
{
  public override void Test()
  {
    Func<int, int> func = (x) => { return x * 2; };
  }
}";

    var expected = new[]
                   {
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(Rules.NoBaseCall)
                           .WithLocation(17, 24)
                   };

    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task ParenthesizedLambda_WithBaseCall_ReportsDiagnostic ()
  {
    const string text = @"
using Remotion.Infrastructure.Analyzers.BaseCalls;
using System;
     
namespace ConsoleApp1;
        
public abstract class BaseClass
{
  [BaseCallCheck(BaseCall.IsMandatory)]
  public virtual int Test ()
  {
    return 5;
  }
}
public class DerivedClass : BaseClass
{
  public override int Test()
  {
    Func<int, int> func = (x) => { base.Test(); return base.Test(); };
    return 5;
  }
}";

    var expected = new[]
                   {
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(Rules.InAnonymousMethod)
                           .WithLocation(19, 36),
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(Rules.InAnonymousMethod)
                           .WithLocation(19, 56),
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(Rules.NoBaseCall)
                           .WithLocation(20, 5) 
                   };

    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task NestedAnonymousMethods_WithBaseCallInInner_ReportsDiagnostic ()
  {
    const string text = @"
using Remotion.Infrastructure.Analyzers.BaseCalls;
     
namespace ConsoleApp1;
        
public abstract class BaseClass
{
  [BaseCallCheck(BaseCall.IsMandatory)]
  public virtual void Test ()
  {
    return;
  }
}
public class DerivedClass : BaseClass
{
  public override void Test()
  {
    var outerAction = delegate()
    {
        var innerAction = () => base.Test();
    };

  }
}";

    var expected = new[]
                   {
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(Rules.InAnonymousMethod)
                           .WithLocation(20, 33),
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(Rules.NoBaseCall)
                           .WithLocation(16, 24) 
                   };


    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }

  [Fact]
  public async Task AnonymousMethod_WithBaseCallInConditional_ReportsDiagnostic ()
  {
    const string text = @"
using Remotion.Infrastructure.Analyzers.BaseCalls;
     
namespace ConsoleApp1;
        
public abstract class BaseClass
{
  [BaseCallCheck(BaseCall.IsMandatory)]
  public virtual void Test ()
  {
    return;
  }
}
public class DerivedClass : BaseClass
{
  public override void Test()
  {
    var action = delegate()
    {
        if (true)
        {
            base.Test();
        }
    };
  }
}";

    var expected = new[]
                   {
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(Rules.InAnonymousMethod)
                           .WithLocation(22, 13), 
                       CSharpAnalyzerVerifier<BaseCallAnalyzer>
                           .Diagnostic(Rules.NoBaseCall)
                           .WithLocation(16, 24) 
                   };

    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }
}