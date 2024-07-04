// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Linq;

namespace Remotion.Infrastructure.Analyzers.BaseCalls;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BaseCallAnalyzer : DiagnosticAnalyzer
{
  // Preferred format of DiagnosticId is Your Prefix + Number, e.g. CA1234.
  private const string DiagnosticId = "RMBCA0001";

  // The category of the diagnostic (Design, Naming etc.).
  private const string Category = "Usage";

  // Feel free to use raw strings if you don't need localization.
  private static readonly LocalizableString Title = "Base Call missing";
  private static readonly LocalizableString TitleLoopMessage = "Base Call is not allowed in a loop";

  // The message that will be displayed to the user.
  private static readonly LocalizableString MessageFormat = "Base Call missing";
  private static readonly LocalizableString MessageFormatLoopMessage = "Base Call found in a loop, not allowed here";

  private static readonly LocalizableString Description = "Base Call is missing.";
  private static readonly LocalizableString DescriptionLoopMessage = "Base Call found in a loop, not allowed here.";

  // Don't forget to add your rules to the AnalyzerReleases.Shipped/Unshipped.md
  public static readonly DiagnosticDescriptor DiagnosticDescriptionNoBaseCallFound = new(
      DiagnosticId,
      Title,
      MessageFormat,
      Category,
      DiagnosticSeverity.Warning,
      true,
      Description);

  public static readonly DiagnosticDescriptor DiagnosticDescriptionBaseCallFoundInLoop = new(
      DiagnosticId,
      TitleLoopMessage,
      MessageFormatLoopMessage,
      Category,
      DiagnosticSeverity.Warning,
      true,
      DescriptionLoopMessage);

  // Keep in mind: you have to list your rules here.
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [DiagnosticDescriptionNoBaseCallFound];

  public override void Initialize (AnalysisContext context)
  {
    context.RegisterSyntaxNodeAction(
        AnalyzeNode,
        SyntaxKind.MethodDeclaration);

    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

    context.EnableConcurrentExecution();
  }

  private void AnalyzeNode (SyntaxNodeAnalysisContext context)
  {
    var node = (MethodDeclarationSyntax)context.Node;

    if (!node.Modifiers.Any(SyntaxKind.OverrideKeyword)) return;

    if (AttributePreventingBaseCallCheck(context)) return;

    //body is empty -> diagnostic
    if (node.Body == null) context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptionNoBaseCallFound, node.GetLocation()));


    //searching for basecalls by going through every line and checking if it's a basecall, example syntax tree:
    /*
     MethodDeclaration
      Body: Block
        Expression: InvocationExpression
          Expression: SimpleMemberAccessExpression
            Expression: BaseExpression
    */
    var childNodes = node.Body!.ChildNodes();
    foreach (var childNode in childNodes)
    {
      InvocationExpressionSyntax invocationExpressionNode;
      MemberAccessExpressionSyntax simpleMemberAccessExpressionNode;
      try
      {
        //trying to cast a line of code to an BaseExpressionSyntax
        var expressionStatementNode = childNode as ExpressionStatementSyntax ?? throw new InvalidOperationException();
        invocationExpressionNode = expressionStatementNode.Expression as InvocationExpressionSyntax ?? throw new InvalidOperationException();
        simpleMemberAccessExpressionNode = invocationExpressionNode.Expression as MemberAccessExpressionSyntax ?? throw new InvalidOperationException();
        _ = simpleMemberAccessExpressionNode.Expression as BaseExpressionSyntax ?? throw new InvalidOperationException();
      }
      catch (InvalidOperationException)
      {
        continue;
      }


      //Method signature
      var methodName = node.Identifier.Text;
      var parameters = node.ParameterList.Parameters;
      var numberOfParameters = parameters.Count;
      var typesOfParameters = parameters.Select(param => param.GetType()).ToArray();
      //int arity = node.Arity; TODO: implement for Generics

      //Method signature of BaseCall
      var nameOfCalledMethod = simpleMemberAccessExpressionNode.Name.Identifier.Text;
      var arguments = invocationExpressionNode.ArgumentList.Arguments;
      int numberOfArguments = arguments.Count;
      Type[] typesOfArguments = arguments.Select(arg => arg.GetType()).ToArray();


      //check if its really a basecall
      if (
          nameOfCalledMethod.Equals(methodName)
          && numberOfParameters == numberOfArguments
          && typesOfParameters.Equals(typesOfArguments)
      )
        return;
    }


    //no basecall found -> Warning
    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptionNoBaseCallFound, node.GetLocation()));
  }

  private bool AttributePreventingBaseCallCheck (SyntaxNodeAnalysisContext context)
  {
    var node = (MethodDeclarationSyntax)context.Node;
    //check for IgnoreBaseCallCheck attribute
    if (HasIgnoreBaseCallCheckAttribute(context)) return true;

    //get overridden method
    var methodSymbol = context.SemanticModel.GetDeclaredSymbol(node);
    var overriddenMethod = methodSymbol?.OverriddenMethod;
    if (overriddenMethod == null) return true;

    //check overridden method for BaseCallCheck attribute
    var res = CheckForBaseCallCheckAttribute(overriddenMethod);
    if (res == BaseCall.IsOptional)
      return true;

    return false;
  }

  private bool HasIgnoreBaseCallCheckAttribute (SyntaxNodeAnalysisContext context)
  {
    var node = (MethodDeclarationSyntax)context.Node;
    foreach (var attributeListSyntax in node.AttributeLists)
    {
      foreach (var attribute in attributeListSyntax.Attributes)
      {
        var imSymbol = context.SemanticModel.GetSymbolInfo(attribute).Symbol;

        if (imSymbol == null) continue;
        var fullNameOfNamespace = imSymbol.ToString();

        if (fullNameOfNamespace.Equals("Remotion.Infrastructure.Analyzers.BaseCalls.IgnoreBaseCallCheckAttribute.IgnoreBaseCallCheckAttribute()"))
          return true;
      }
    }

    return false;
  }

  private BaseCall CheckForBaseCallCheckAttribute (IMethodSymbol overriddenMethod)
  {
    //mostly ChatGPT
    var attributes = overriddenMethod.GetAttributes();
    var attributeDescriptions = attributes.Select(
        attr =>
        {
          var attributeClass = attr.AttributeClass;
          var attributeNamespace = attributeClass?.ContainingNamespace.ToDisplayString();
          var valueOfEnumArgument = attr.ConstructorArguments[0].Value;
          if (valueOfEnumArgument != null)
            return $"{attributeNamespace}.{attributeClass?.Name}(BaseCall.{Enum.GetName(typeof(BaseCall), valueOfEnumArgument)})";
          return $"{attributeNamespace}.{attributeClass?.Name}";
        }).ToList();


    foreach (var attributeDescription in attributeDescriptions)
    {
      if (attributeDescription.Equals("Remotion.Infrastructure.Analyzers.BaseCalls.BaseCallCheckAttribute(BaseCall.IsOptional)"))
        return BaseCall.IsOptional;
    }

    return BaseCall.IsMandatory;
  }
}