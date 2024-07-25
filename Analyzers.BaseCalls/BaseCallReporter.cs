// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier: MIT
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Remotion.Infrastructure.Analyzers.BaseCalls;

public static class BaseCallReporter
{
  public static void ReportAll (SyntaxNodeAnalysisContext context, BaseCallDescriptor[] baseCalls, DiagnosticDescriptor diagnosticDescriptor)
  {
    foreach (var baseCall in baseCalls)
    {
      ReportDiagnostic(context, Diagnostic.Create(diagnosticDescriptor, baseCall.Location));
    }
  }

  public static void ReportAllWrong (SyntaxNodeAnalysisContext context, BaseCallDescriptor[] baseCalls)
  {
    baseCalls = baseCalls.Where(descriptor => !descriptor.CallsBaseMethod).ToArray();
    ReportAll(context, baseCalls, Rules.WrongBaseCall);
  }
  public static void ReportDiagnostic (SyntaxNodeAnalysisContext context, Diagnostic? diagnostic)
  {
    if (diagnostic == null)
    {
      return;
    }

    context.ReportDiagnostic(diagnostic);
  }
}