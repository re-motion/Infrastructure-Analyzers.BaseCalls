// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Remotion.Infrastructure.Analyzers.BaseCalls;

public enum BaseCallType
{
  Normal,
  None,
  InLoop,
  Multiple
}

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BaseCallAnalyzer : DiagnosticAnalyzer
{
  //rules
  private const string DiagnosticId = "RMBCA0001";
  private const string Category = "Usage";
  private static readonly LocalizableString Title = "Base Call missing";
  private static readonly LocalizableString MessageFormat = "Base Call missing";
  private static readonly LocalizableString Description = "Base Call is missing.";

  public static readonly DiagnosticDescriptor DiagnosticDescriptionNoBaseCallFound = new(
      DiagnosticId,
      Title,
      MessageFormat,
      Category,
      DiagnosticSeverity.Warning,
      true,
      Description);


  private const string DiagnosticIdLoopMessage = "RMBCA0002";
  private static readonly LocalizableString TitleLoopMessage = "Base Call found in a loop";
  private static readonly LocalizableString MessageFormatLoopMessage = "Base Call found in a loop";
  private static readonly LocalizableString DescriptionLoopMessage = "Base Call found in a loop, not allowed here.";

  public static readonly DiagnosticDescriptor DiagnosticDescriptionBaseCallFoundInLoop = new(
      DiagnosticIdLoopMessage,
      TitleLoopMessage,
      MessageFormatLoopMessage,
      Category,
      DiagnosticSeverity.Warning,
      true,
      DescriptionLoopMessage);

  private const string DiagnosticIdMultipleBaseCalls = "RMBCA0005";
  private static readonly LocalizableString TitleMultipleBaseCalls = "multiple BaseCalls found";
  private static readonly LocalizableString MessageMultipleBaseCalls = "multiple BaseCalls found";
  private static readonly LocalizableString DescriptionMultipleBaseCalls = "multiple BaseCalls found in this method, there should only be one BaseCall.";

  public static readonly DiagnosticDescriptor DiagnosticDescriptionMultipleBaseCalls = new(
      DiagnosticIdMultipleBaseCalls,
      TitleMultipleBaseCalls,
      MessageMultipleBaseCalls,
      Category,
      DiagnosticSeverity.Warning,
      true,
      DescriptionMultipleBaseCalls);

  private const string DiagnosticIdWrongBaseCall = "RMBCA0006";
  private static readonly LocalizableString TitleWrongBaseCall = "incorrect BaseCall";
  private static readonly LocalizableString MessageWrongBaseCall = "BaseCall does not call the overridden Method";
  private static readonly LocalizableString DescriptionWrongBaseCall = "BaseCall does not call the overridden Method.";

  public static readonly DiagnosticDescriptor DiagnosticDescriptionWrongBaseCall = new(
      DiagnosticIdWrongBaseCall,
      TitleWrongBaseCall,
      MessageWrongBaseCall,
      Category,
      DiagnosticSeverity.Warning,
      true,
      DescriptionWrongBaseCall);

  //list of Rules
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [DiagnosticDescriptionNoBaseCallFound, DiagnosticDescriptionBaseCallFoundInLoop, DiagnosticDescriptionMultipleBaseCalls, DiagnosticDescriptionWrongBaseCall];

  public override void Initialize (AnalysisContext context)
  {
    context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

    context.EnableConcurrentExecution();
  }

  private static void AnalyzeMethod (SyntaxNodeAnalysisContext context)
  {
    if (!BaseCallCheckShouldHappen(context)) return;

    var node = (context.Node as MethodDeclarationSyntax)!;

    if (node.Body == null) ReportBaseCallDiagnostic(context, BaseCallType.None);

    var (_, _, diagnostic) = BaseCallCheckerRecursive(context, node, 0, 0);

    ReportBaseCallDiagnostic(context, diagnostic);
  }

  //for anonymous method and Local function analyzers
  public static bool SimpleContainsBaseCall (SyntaxNodeAnalysisContext context, SyntaxNode node)
  {
    if (node is ExpressionSyntax expression)
      return IsBaseCall(context, expression);

    foreach (var childNode in node.DescendantNodes())
    {
      if (IsBaseCall(context, childNode))
        return true;
      if (BaseCallInAnIfRecursive(context, childNode))
        return true;
      if (BaseCallInLoopRecursive(context, childNode))
        return true;
    }

    return false;
  }

  //trimmed down version of BaseCallCheckerRecursive
  private static bool BaseCallInAnIfRecursive (SyntaxNodeAnalysisContext context, SyntaxNode node)
  {
    if (IsBaseCall(context, node)) return true;
    if (!IsBranch(node)) return false;

    var ifStatementSyntax = node as IfStatementSyntax;
    ElseClauseSyntax? elseClauseSyntax = null;

    do //loop over every if / elseIf / else clause
    {
      var statement = ifStatementSyntax?.Statement ?? elseClauseSyntax?.Statement;
      var childNodes = statement is BlockSyntax blockSyntax
          ? blockSyntax.ChildNodes()
          : new[] { statement }!;

      foreach (var childNode in childNodes)
      {
        //nested if -> recursion
        if (IsIf(childNode) || IsElse(childNode))
        {
          if (BaseCallInAnIfRecursive(context, childNode))
            return true;
        }
        else if (IsLoop(childNode) && BaseCallInLoopRecursive(context, childNode))
          ReportBaseCallDiagnostic(context, BaseCallType.InLoop);

        else if (IsBaseCall(context, childNode))
          return true;
      }

      //go to next else branch
      elseClauseSyntax = ifStatementSyntax?.Else;
      ifStatementSyntax = elseClauseSyntax?.Statement as IfStatementSyntax;
    } while (elseClauseSyntax != null);

    return false;
  }

  private static bool BaseCallCheckShouldHappen (SyntaxNodeAnalysisContext context)
  {
    var node = (MethodDeclarationSyntax)context.Node;

    if (!node.Modifiers.Any(SyntaxKind.OverrideKeyword)) return false;

    if (!((PredefinedTypeSyntax)node.ReturnType).Keyword.IsKind(SyntaxKind.VoidKeyword))
      return false; //temporary, checker is not yet implemented for non-void methods //TODO: implement for non-void methods

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

    BaseCall CheckForBaseCallCheckAttribute (IMethodSymbol overriddenMethod)
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

  private static bool IsBaseCall (SyntaxNodeAnalysisContext context, SyntaxNode childNode)
  {
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


    if (context.Node is not MethodDeclarationSyntax node) return true; //for anonymous methods and local functions


    //Method signature
    var methodName = node.Identifier.Text;
    var parameters = node.ParameterList.Parameters;
    var numberOfParameters = parameters.Count;
    var typesOfParameters = parameters.Select(
        param =>
            context.SemanticModel.GetDeclaredSymbol(param)?.Type).ToArray();

    //Method signature of BaseCall
    var nameOfCalledMethod = simpleMemberAccessExpressionNode.Name.Identifier.Text;
    var arguments = invocationExpressionNode.ArgumentList.Arguments;
    var numberOfArguments = arguments.Count;
    var typesOfArguments = arguments.Select(
        arg =>
            context.SemanticModel.GetTypeInfo(arg.Expression).Type).ToArray();


    //check if it's really a basecall
    if (nameOfCalledMethod.Equals(methodName)
        && numberOfParameters == numberOfArguments
        && typesOfParameters.Length == typesOfArguments.Length
        && typesOfParameters.Zip(typesOfArguments, (p, a) => (p, a))
            .All(pair => SymbolEqualityComparer.Default.Equals(pair.p, pair.a)))
      return true;

    //wrong basecall
    var squiggliesLocation = Location.Create(
        invocationExpressionNode.SyntaxTree,
        TextSpan.FromBounds(
            invocationExpressionNode.GetLeadingTrivia().Span.End,
            invocationExpressionNode.ArgumentList.Span.End
        )
    );
    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptionWrongBaseCall, squiggliesLocation));
    return false;
  }

  private static bool BaseCallInLoopRecursive (SyntaxNodeAnalysisContext context, SyntaxNode node)
  {
    if (IsBaseCall(context, node)) return true;
    if (IsBranch(node)) return BaseCallInAnIfRecursive(context, node);
    if (!IsLoop(node)) return false;

    var loopStatement = (node as ForStatementSyntax)?.Statement ??
                        (node as WhileStatementSyntax)?.Statement ??
                        (node as ForEachStatementSyntax)?.Statement ??
                        (node as DoStatementSyntax)?.Statement;

    return loopStatement!.ChildNodes().Any(childNode => BaseCallInLoopRecursive(context, childNode));
  }

  private static (int min, int max, BaseCallType diagnostic) BaseCallCheckerRecursive (SyntaxNodeAnalysisContext context, SyntaxNode node, int minArg, int maxArg)
  {
    //min... minimal number of basecalls
    //max... maximal number of basecalls
    //-1 means it returns
    //-2 means a diagnostic was found


    var listOfResults = new List<(int min, int max)>();
    var ifStatementSyntax = node as IfStatementSyntax;
    ElseClauseSyntax? elseClauseSyntax = null;


    do //loop over every if / elseIf / else clause
    {
      var min = minArg;
      var max = maxArg;
      BaseCallType diagnostic = BaseCallType.Normal;

      //get childNodes
      var statement = ifStatementSyntax?.Statement ?? elseClauseSyntax?.Statement;
      IEnumerable<SyntaxNode> childNodes;
      if (statement == null) //not an if or else -> get childnodes of Method Declaration
      {
        var methodDeclaration = (MethodDeclarationSyntax)node;
        childNodes = methodDeclaration.Body!.ChildNodes();
      }
      else
      {
        childNodes = statement is BlockSyntax blockSyntax
            ? blockSyntax.ChildNodes() //blocks with {}
            : new[] { statement }; //blocks without{}
      }

      //loop over childNodes
      foreach (var childNode in childNodes)
      {
        //nested if -> recursion
        if (IsIf(childNode) || IsElse(childNode))
        {
          var (resmin, resmax, resDiagnostic) = BaseCallCheckerRecursive(context, childNode, min, max);

          if (resmin == -2) //recursion found a diagnostic -> stop everything
            return (resmin, resmax, resDiagnostic);

          if (resmin == -1)
          {
            diagnostic = resDiagnostic;
            min = resmin;
            max = resmax;
            break;
          }

          min = Math.Max(min, resmin);
          max = Math.Max(max, resmax);
        }

        else if (IsReturn(childNode))
        {
          diagnostic = GetBaseCallType(min, max);
          min = -1;
          max = -1;
          break;
        }

        else if (IsBaseCall(context, childNode))
        {
          min++;
          max++;
        }

        else if (IsLoop(childNode) && BaseCallInLoopRecursive(context, childNode))
          diagnostic = BaseCallType.InLoop;
      }

      if (diagnostic != BaseCallType.Normal) //found a diagnostic
        return (-2, -2, diagnostic);

      listOfResults.Add((min, max));

      //go to next else branch
      elseClauseSyntax = ifStatementSyntax?.Else;
      ifStatementSyntax = elseClauseSyntax?.Statement as IfStatementSyntax;
    } while (elseClauseSyntax != null);

    //find the overall min and max
    listOfResults.RemoveAll(item => item == (-1, -1));

    if (listOfResults.Count == 0)
      return (-1, -1, BaseCallType.Normal);

    minArg = listOfResults[0].min;
    maxArg = listOfResults[0].max;
    foreach (var (minInstance, maxInstance) in listOfResults)
    {
      minArg = Math.Min(minArg, minInstance);
      maxArg = Math.Max(maxArg, maxInstance);
    }

    return (minArg, maxArg, GetBaseCallType(minArg, maxArg));
  }

  private static BaseCallType GetBaseCallType (int min, int max)
  {
    if (max >= 2) return BaseCallType.Multiple;
    if (min == 0) return BaseCallType.None;
    return BaseCallType.Normal;
  }

  private static void ReportBaseCallDiagnostic (SyntaxNodeAnalysisContext context, BaseCallType type)
  {
    if (type == BaseCallType.Normal)
      return;

    var node = (MethodDeclarationSyntax)context.Node;
    //location of the squigglies (whole line of the method declaration)
    var squiggliesLocation = Location.Create(
        node.SyntaxTree,
        TextSpan.FromBounds(
            node.GetLeadingTrivia().Span.End,
            node.ParameterList.Span.End
        )
    );
    var diagnostic = type switch
    {
        BaseCallType.None => DiagnosticDescriptionNoBaseCallFound,

        BaseCallType.InLoop => DiagnosticDescriptionBaseCallFoundInLoop,

        BaseCallType.Multiple => DiagnosticDescriptionMultipleBaseCalls,

        _ => throw new ArgumentOutOfRangeException(),
    };
    context.ReportDiagnostic(Diagnostic.Create(diagnostic, squiggliesLocation));
  }

  public static bool HasIgnoreBaseCallCheckAttribute (SyntaxNodeAnalysisContext context)
  {
    SyntaxList<AttributeListSyntax> attributeLists;

    if (context.Node is MethodDeclarationSyntax methodDeclaration)
      attributeLists = methodDeclaration.AttributeLists;

    else if (context.Node is LocalFunctionStatementSyntax localFunction)
      attributeLists = localFunction.AttributeLists;

    else
      throw new Exception("Expected a method declaration or function declaration");


    foreach (var attributeListSyntax in attributeLists)
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

  private static readonly Func<SyntaxNode, bool> IsLoop = node => node is ForStatementSyntax or WhileStatementSyntax or ForEachStatementSyntax or DoStatementSyntax;
  private static readonly Func<SyntaxNode, bool> IsBranch = node => node is IfStatementSyntax or ElseClauseSyntax;
  private static readonly Func<SyntaxNode, bool> IsIf = node => node is IfStatementSyntax;
  private static readonly Func<SyntaxNode, bool> IsElse = node => node is ElseClauseSyntax;
  private static readonly Func<SyntaxNode, bool> IsReturn = node => node is ReturnStatementSyntax;
}