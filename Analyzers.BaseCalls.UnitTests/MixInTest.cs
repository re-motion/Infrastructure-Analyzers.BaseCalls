// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier: MIT

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Remotion.Infrastructure.Analyzers.BaseCalls.UnitTests.Utilities;
using Xunit;

namespace Remotion.Infrastructure.Analyzers.BaseCalls.UnitTests;

public class MixInTests
{
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

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.NoBaseCall)
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

    var expected = CSharpAnalyzerVerifier<BaseCallAnalyzer>.Diagnostic(BaseCallAnalyzer.WrongBaseCall)
        .WithLocation(25, 5)
        .WithArguments("NonOverridingMethod");
    await CSharpAnalyzerVerifier<BaseCallAnalyzer>.VerifyAnalyzerAsync(text, expected);
  }
}