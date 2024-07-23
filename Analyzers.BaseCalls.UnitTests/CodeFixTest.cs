// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier: MIT

using System;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;
using Verifier =
    Remotion.Infrastructure.Analyzers.BaseCalls.UnitTests.Utilities.CSharpCodeFixVerifier<Remotion.Infrastructure.Analyzers.BaseCalls.BaseCallAnalyzer,
        Remotion.Infrastructure.Analyzers.BaseCalls.BaseCallCodeFixProvider>;

namespace Remotion.Infrastructure.Analyzers.BaseCalls.UnitTests;

public class CodeFixTest
{
  [Fact]
  public async Task AddsIgnoreCheckAttribute ()
  {
    const string TestCode = @"
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
}";
    const string FixedCode = @"
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
}";


    var expected = Verifier.Diagnostic(BaseCallAnalyzer.NoBaseCall).WithLocation(15, 26);
    await Verifier.VerifyCodeFixAsync(TestCode, expected, FixedCode);
  }
  
  [Fact]
  public async Task AddsIgnoreCheckAttribute_And_UsingDirective ()
  {
    const string TestCode = @"


public abstract class BaseClass
{

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
}";
    const string FixedCode = @"using Remotion.Infrastructure.Analyzers.BaseCalls;

public abstract class BaseClass
{

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
}";


    var expected = Verifier.Diagnostic(BaseCallAnalyzer.NoBaseCall).WithLocation(15, 26);
    await Verifier.VerifyCodeFixAsync(TestCode, expected, FixedCode);
  }
}