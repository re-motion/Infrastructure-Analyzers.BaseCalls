// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier:MIT

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Remotion.Infrastructure.Analyzers.BaseCalls;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BaseCallAnalyzer : DiagnosticAnalyzer
{
  // Preferred format of DiagnosticId is Your Prefix + Number, e.g. CA1234.
  private const string DiagnosticId = "RMBCA0001";

  // The category of the diagnostic (Design, Naming etc.).
  private const string Category = "Usage";

  // Feel free to use raw strings if you don't need localization.
  private static readonly LocalizableString Title = "DummyTitle";

  // The message that will be displayed to the user.
  private static readonly LocalizableString MessageFormat = "DummyMessage";

  private static readonly LocalizableString Description = "DummyDescription.";

  // Don't forget to add your rules to the AnalyzerReleases.Shipped/Unshipped.md
  public static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category,
    DiagnosticSeverity.Warning, true, Description);

  // Keep in mind: you have to list your rules here.
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    ImmutableArray.Create(Rule);

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

    context.EnableConcurrentExecution();
  }
}