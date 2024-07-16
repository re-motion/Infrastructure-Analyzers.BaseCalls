// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier: MIT

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

namespace Remotion.Infrastructure.Analyzers.BaseCalls.UnitTests;

public class CodeFixTest
{
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
            .Diagnostic(BaseCallAnalyzer.NoBaseCall)
            .WithLocation(14, 5)
            .WithArguments("Test"));


    await test.RunAsync();
  }
}