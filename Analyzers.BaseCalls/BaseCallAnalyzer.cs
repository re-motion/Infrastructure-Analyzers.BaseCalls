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
  private const string DiagnosticIdLoopMessage = "RMBCA0002";

  // The category of the diagnostic (Design, Naming etc.).
  private const string Category = "Usage";

  // Feel free to use raw strings if you don't need localization.
  private static readonly LocalizableString Title = "Base Call missing";
  private static readonly LocalizableString TitleLoopMessage = "Base Call found in a loop";

  // The message that will be displayed to the user.
  private static readonly LocalizableString MessageFormat = "Base Call missing";
  private static readonly LocalizableString MessageFormatLoopMessage = "Base Call found in a loop";

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
      DiagnosticIdLoopMessage,
      TitleLoopMessage,
      MessageFormatLoopMessage,
      Category,
      DiagnosticSeverity.Warning,
      true,
      DescriptionLoopMessage);

  // Keep in mind: you have to list your rules here.
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [DiagnosticDescriptionNoBaseCallFound, DiagnosticDescriptionBaseCallFoundInLoop];

  public override void Initialize (AnalysisContext context)
  {
    context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);

    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

    context.EnableConcurrentExecution();
  }

  private void AnalyzeNode (SyntaxNodeAnalysisContext context)
  {
    var node = (MethodDeclarationSyntax)context.Node;

    if (!BaseCallCheckShouldHappen(context)) return;

    if (node.Body == null) context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptionNoBaseCallFound, node.GetLocation()));

    var childNodes = node.Body!.ChildNodes();
    foreach (var childNode in childNodes)
    {
      if (BaseCallInAllIfsRecursive(context, childNode)) return;
      if (IsBaseCall(context, childNode)) return;
      if (BaseCallInLoopRecursive(context, childNode))
      {
        context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptionBaseCallFoundInLoop, node.GetLocation()));
        return;
      }
    }

    //no basecall found -> Warning
    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptionNoBaseCallFound, node.GetLocation()));
  }

  private bool BaseCallCheckShouldHappen (SyntaxNodeAnalysisContext context)
  {
    var node = (MethodDeclarationSyntax)context.Node;

    if (!node.Modifiers.Any(SyntaxKind.OverrideKeyword)) return false;

    //check for IgnoreBaseCallCheck attribute
    if (HasIgnoreBaseCallCheckAttribute(context)) return false;

    //get overridden method
    var overriddenMethodAsIMethodSymbol = context.SemanticModel.GetDeclaredSymbol(node)?.OverriddenMethod;
    var overriddenMethodAsNode = overriddenMethodAsIMethodSymbol?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as MethodDeclarationSyntax;

    //check base method for attribute if it does not have one, next base method will be checked
    while (overriddenMethodAsNode != null)
    {
      //check overridden method for BaseCallCheck attribute
      var res = CheckForBaseCallCheckAttribute(overriddenMethodAsIMethodSymbol!);

      switch (res)
      {
        case BaseCall.IsOptional:
          return false;
        case BaseCall.IsMandatory:
          return true;
        case BaseCall.Default:
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }

      if (overriddenMethodAsNode.Modifiers.Any(SyntaxKind.AbstractKeyword))
        return false;
      

      //go one generation back
      overriddenMethodAsIMethodSymbol = context.SemanticModel.GetDeclaredSymbol(overriddenMethodAsNode)?.OverriddenMethod;
      overriddenMethodAsNode = overriddenMethodAsIMethodSymbol?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as MethodDeclarationSyntax;
    }

    return ((PredefinedTypeSyntax)node.ReturnType).Keyword.IsKind(SyntaxKind.VoidKeyword);
  }

  private bool IsBaseCall (SyntaxNodeAnalysisContext context, SyntaxNode childNode)
  {
    var node = (MethodDeclarationSyntax)context.Node;
    InvocationExpressionSyntax invocationExpressionNode;
    MemberAccessExpressionSyntax simpleMemberAccessExpressionNode;
    try
    {
      //trying to cast a line of code to an BaseExpressionSyntax, example Syntax tree:
      /*
       MethodDeclaration
        Body: Block
          Expression: InvocationExpression
            Expression: SimpleMemberAccessExpression
              Expression: BaseExpression
      */
      if (childNode is not InvocationExpressionSyntax syntax)
      {
        var expressionStatementNode = childNode as ExpressionStatementSyntax ?? throw new InvalidOperationException();
        invocationExpressionNode = expressionStatementNode.Expression as InvocationExpressionSyntax ?? throw new InvalidOperationException();
      }
      else //special case for use in BaseCallInAllIfsRecursive
      {
        invocationExpressionNode = syntax;
      }

      simpleMemberAccessExpressionNode = invocationExpressionNode.Expression as MemberAccessExpressionSyntax ?? throw new InvalidOperationException();
      _ = simpleMemberAccessExpressionNode.Expression as BaseExpressionSyntax ?? throw new InvalidOperationException();
    }
    catch (InvalidOperationException)
    {
      return false;
    }


    //Method signature
    var methodName = node.Identifier.Text;
    var parameters = node.ParameterList.Parameters;
    var numberOfParameters = parameters.Count;
    var typesOfParameters = parameters.Select(param => param.GetType()).ToArray();

    //Method signature of BaseCall
    var nameOfCalledMethod = simpleMemberAccessExpressionNode.Name.Identifier.Text;
    var arguments = invocationExpressionNode.ArgumentList.Arguments;
    int numberOfArguments = arguments.Count;
    Type[] typesOfArguments = arguments.Select(arg => arg.GetType()).ToArray();


    //check if it's really a basecall
    if (
        nameOfCalledMethod.Equals(methodName)
        && numberOfParameters == numberOfArguments
        && typesOfParameters.Equals(typesOfArguments)
    )
      return true;
    return false;
  }

  private bool BaseCallInLoopRecursive (SyntaxNodeAnalysisContext context, SyntaxNode node)
  {
    if (IsBaseCall(context, node)) return true;
    if (!IsLoop(node)) return false;

    var loopStatement = (node as ForStatementSyntax)?.Statement ??
                        (node as WhileStatementSyntax)?.Statement ??
                        (node as ForEachStatementSyntax)?.Statement ??
                        (node as DoStatementSyntax)?.Statement;


    return loopStatement!.ChildNodes().Any(childNode => BaseCallInLoopRecursive(context, childNode));
  }

  private bool BaseCallInAllIfsRecursive (SyntaxNodeAnalysisContext context, SyntaxNode node)
  {
    var methodNode = (MethodDeclarationSyntax)context.Node;
    if (IsBaseCall(context, node)) return true;
    if (!IsBranch(node)) return false;

    var ifStatement = (node as IfStatementSyntax)?.Statement ??
                      (node as ElseClauseSyntax)?.Statement;

    var baseCallFound = true;
    var baseCallFoundHere = false;
    foreach (var childNode in ifStatement!.ChildNodes())
    {
      if (childNode is BlockSyntax blockSyntax)
      {
        //for if blocks with {}
        foreach (var blockChildNode in blockSyntax.ChildNodes())
        {
          if (BaseCallInAllIfsRecursive(context, blockChildNode))
            baseCallFoundHere = true;
          if (IsLoop(blockChildNode))
          {
            if (BaseCallInLoopRecursive(context, blockChildNode))
            {
              context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptionBaseCallFoundInLoop, methodNode.GetLocation()));
              return true;
            }
          }
        }
      }

      //for if block without {}
      else if (IsBranch(childNode))
      {
        baseCallFound = baseCallFound && BaseCallInAllIfsRecursive(context, childNode);
      }
      else if (IsLoop(childNode))
      {
        if (BaseCallInLoopRecursive(context, childNode))
        {
          context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptionBaseCallFoundInLoop, methodNode.GetLocation()));
          return true;
        }
      }
      else if (IsBaseCall(context, childNode))
      {
        baseCallFoundHere = true;
      }
    }

    return baseCallFound && baseCallFoundHere;
  }

  private readonly Func<SyntaxNode, bool> IsLoop = node => node is ForStatementSyntax or WhileStatementSyntax or ForEachStatementSyntax or DoStatementSyntax;

  private readonly Func<SyntaxNode, bool> IsBranch = node => node is IfStatementSyntax or ElseClauseSyntax;

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
      if (attributeDescription.Equals("Remotion.Infrastructure.Analyzers.BaseCalls.BaseCallCheckAttribute(BaseCall.IsMandatory)"))
        return BaseCall.IsMandatory;
    }

    return BaseCall.Default;
  }
}