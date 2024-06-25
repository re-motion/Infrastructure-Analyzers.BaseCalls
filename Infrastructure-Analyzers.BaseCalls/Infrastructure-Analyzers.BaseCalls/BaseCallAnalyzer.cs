using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Infrastructure_Analyzers.BaseCalls;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BaseCallAnalyzer : DiagnosticAnalyzer
{
    // Preferred format of DiagnosticId is Your Prefix + Number, e.g. CA1234.
    private const string DiagnosticId = "DY0001";

    // Feel free to use raw strings if you don't need localization.
    private static readonly LocalizableString Title = "DummyTitle";

    // The message that will be displayed to the user.
    private static readonly LocalizableString MessageFormat = "DummyMessage";

    private static readonly LocalizableString Description = "DummyDescription.";

    // The category of the diagnostic (Design, Naming etc.).
    private const string Category = "Usage";

    // Don't forget to add your rules to the AnalyzerReleases.Shipped/Unshipped.md
    private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category,
        DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

    // Keep in mind: you have to list your rules here.
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.EnableConcurrentExecution();
    }
}