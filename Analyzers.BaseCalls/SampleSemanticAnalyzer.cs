using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Infrastructure_Analyzers.BaseCalls;

/// <summary>
///   A sample analyzer that reports invalid values being used for the 'speed' parameter of the 'SetSpeed' function.
///   To make sure that we analyze the method of the specific class, we use semantic analysis instead of the syntax tree,
///   so this analyzer will not work if the project is not compilable.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SampleSemanticAnalyzer : DiagnosticAnalyzer
{
  private const string CommonApiClassName = "Spaceship";
  private const string CommonApiMethodName = "SetSpeed";

  // Preferred format of DiagnosticId is Your Prefix + Number, e.g. CA1234.
  private const string DiagnosticId = "BCA0002";

  // The category of the diagnostic (Design, Naming etc.).
  private const string Category = "Usage";

  // Feel free to use raw strings if you don't need localization.
  private static readonly LocalizableString Title = "Spaceship is going faster than the speed of light";

  // The message that will be displayed to the user.
  private static readonly LocalizableString MessageFormat =
    "Cannot set speed of spaceship to '{0}' as this would be faster than the speed of light";

  private static readonly LocalizableString Description =
    "A spaceship should not be able to go faster than the speed of light.";

  private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category,
    DiagnosticSeverity.Warning, true, Description);

  // Keep in mind: you have to list your rules here.
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    ImmutableArray.Create(Rule);

  public override void Initialize(AnalysisContext context)
  {
    // You must call this method to avoid analyzing generated code.
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

    // You must call this method to enable the Concurrent Execution.
    context.EnableConcurrentExecution();

    // Subscribe to semantic (compile time) action invocation, e.g. method invocation.
    context.RegisterOperationAction(AnalyzeInvocationOperation, OperationKind.Invocation);
  }

  /// <summary>
  ///   Executed on the completion of the semantic analysis associated with the Invocation operation as called above.
  /// </summary>
  /// <param name="context">Operation context.</param>
  private void AnalyzeInvocationOperation(OperationAnalysisContext context)
  {
    // The Roslyn architecture is based on inheritance.
    // To get the required metadata, we should match the 'Operation' and 'Syntax' objects to the particular types,
    // which are based on the 'OperationKind' parameter specified in the 'Register...' method.
    if (context.Operation is not IInvocationOperation invocationOperation ||
        context.Operation.Syntax is not InvocationExpressionSyntax invocationSyntax)
      return;

    var methodSymbol = invocationOperation.TargetMethod;

    // Check whether the method name is 'SetSpeed' and it is a member of the 'Spaceship' class.
    if (methodSymbol.MethodKind != MethodKind.Ordinary ||
        methodSymbol.ReceiverType?.Name != CommonApiClassName ||
        methodSymbol.Name != CommonApiMethodName
       )
      return;

    // Count validation is enough in most cases. Keep analyzers as simple as possible.
    if (invocationSyntax.ArgumentList.Arguments.Count != 1)
      return;

    // Traverse through the syntax tree, starting with the particular 'InvocationSyntax' to the desired node.
    var argumentSyntax = invocationSyntax.ArgumentList.Arguments.Single().Expression;

    // The 'ToString' method of 'Syntax' classes returns the corresponding part of the source code.
    var argument = argumentSyntax.ToString();

    if (!int.TryParse(argument, out var actualSpeed))
      return;

    if (actualSpeed <= 299_792_458)
      return;

    var diagnostic = Diagnostic.Create(Rule,
      // The highlighted area in the analyzed source code. Keep it as specific as possible.
      argumentSyntax.GetLocation(),
      // The value is passed to the 'MessageFormat' argument of your rule.
      actualSpeed);

    // Reporting a diagnostic is the primary outcome of analyzers.
    context.ReportDiagnostic(diagnostic);
  }
}