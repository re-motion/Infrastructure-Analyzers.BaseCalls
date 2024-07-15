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
  Multiple,
  InTryCatch,
  InNonOverridingMethod
}

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BaseCallAnalyzer : DiagnosticAnalyzer
{
  //rules
  private const string c_diagnosticId = "RMBCA0001";
  private const string c_category = "Usage";
  private static readonly LocalizableString s_title = "Base Call missing";
  private static readonly LocalizableString s_messageFormat = "Base Call missing";
  private static readonly LocalizableString s_description = "Base Call is missing.";

  public static readonly DiagnosticDescriptor DiagnosticDescriptionNoBaseCallFound = new(
      c_diagnosticId,
      s_title,
      s_messageFormat,
      c_category,
      DiagnosticSeverity.Warning,
      true,
      s_description);


  private const string c_diagnosticIdLoopMessage = "RMBCA0002";
  private static readonly LocalizableString s_titleLoopMessage = "Base Call found in a loop";
  private static readonly LocalizableString s_messageFormatLoopMessage = "Base Call found in a loop";
  private static readonly LocalizableString s_descriptionLoopMessage = "Base Call found in a loop, not allowed here.";

  public static readonly DiagnosticDescriptor DiagnosticDescriptionBaseCallFoundInLoop = new(
      c_diagnosticIdLoopMessage,
      s_titleLoopMessage,
      s_messageFormatLoopMessage,
      c_category,
      DiagnosticSeverity.Warning,
      true,
      s_descriptionLoopMessage);

  private const string c_diagnosticIdMultipleBaseCalls = "RMBCA0005";
  private static readonly LocalizableString s_titleMultipleBaseCalls = "multiple BaseCalls found";
  private static readonly LocalizableString s_messageMultipleBaseCalls = "multiple BaseCalls found";
  private static readonly LocalizableString s_descriptionMultipleBaseCalls = "multiple BaseCalls found in this method, there should only be one BaseCall.";

  public static readonly DiagnosticDescriptor DiagnosticDescriptionMultipleBaseCalls = new(
      c_diagnosticIdMultipleBaseCalls,
      s_titleMultipleBaseCalls,
      s_messageMultipleBaseCalls,
      c_category,
      DiagnosticSeverity.Warning,
      true,
      s_descriptionMultipleBaseCalls);

  private const string c_diagnosticIdWrongBaseCall = "RMBCA0006";
  private static readonly LocalizableString s_titleWrongBaseCall = "incorrect BaseCall";
  private static readonly LocalizableString s_messageWrongBaseCall = "BaseCall does not call the overridden Method";
  private static readonly LocalizableString s_descriptionWrongBaseCall = "BaseCall does not call the overridden Method.";

  public static readonly DiagnosticDescriptor DiagnosticDescriptionWrongBaseCall = new(
      c_diagnosticIdWrongBaseCall,
      s_titleWrongBaseCall,
      s_messageWrongBaseCall,
      c_category,
      DiagnosticSeverity.Warning,
      true,
      s_descriptionWrongBaseCall);

  private const string c_diagnosticIdInTryOrCatch = "RMBCA0007";
  private static readonly LocalizableString s_titleInTryOrCatch = "BaseCall in Try or Catch block";
  private static readonly LocalizableString s_messageInTryOrCatch = "BaseCall is not allowed in Try or Catch block";
  private static readonly LocalizableString s_descriptionInTryOrCatch = "BaseCall is not allowed in Try or Catch block.";

  public static readonly DiagnosticDescriptor DiagnosticDescriptionInTryOrCatch = new(
      c_diagnosticIdInTryOrCatch,
      s_titleInTryOrCatch,
      s_messageInTryOrCatch,
      c_category,
      DiagnosticSeverity.Warning,
      true,
      s_descriptionInTryOrCatch);

  private const string c_diagnosticIdInInNonOverridingMethod = "RMBCA0008";
  private static readonly LocalizableString s_titleInInNonOverridingMethod = "BaseCall in non overriding Method";
  private static readonly LocalizableString s_messageInInNonOverridingMethod = "BaseCall is not allowed in non overriding Method";
  private static readonly LocalizableString s_descriptionInInNonOverridingMethod = "BaseCall is not allowed in non overriding Method.";

  public static readonly DiagnosticDescriptor DiagnosticDescriptionInInNonOverridingMethod = new(
      c_diagnosticIdInInNonOverridingMethod,
      s_titleInInNonOverridingMethod,
      s_messageInInNonOverridingMethod,
      c_category,
      DiagnosticSeverity.Warning,
      true,
      s_descriptionInInNonOverridingMethod);

  //list of Rules
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
  [
      DiagnosticDescriptionNoBaseCallFound, DiagnosticDescriptionBaseCallFoundInLoop, DiagnosticDescriptionMultipleBaseCalls, DiagnosticDescriptionWrongBaseCall,
      DiagnosticDescriptionInTryOrCatch, DiagnosticDescriptionInInNonOverridingMethod
  ];

  public override void Initialize (AnalysisContext context)
  {
    context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
  }

  private static void AnalyzeMethod (SyntaxNodeAnalysisContext context)
  {
    var node = (context.Node as MethodDeclarationSyntax)!;

    //check for non-overriding methods
    var overrides = node.Modifiers.Any(SyntaxKind.OverrideKeyword);
    if (!overrides && !HasIgnoreBaseCallCheckAttribute(context))
    {
      if (ContainsAnyBaseCall(context, node))
        ReportBaseCallDiagnostic(context, BaseCallType.InNonOverridingMethod);
      return;
    }


    if (!BaseCallCheckShouldHappen(context)) return;

    // method is empty
    if (node.Body == null && node.ExpressionBody == null)
    {
      ReportBaseCallDiagnostic(context, BaseCallType.None);
      return;
    }

    // normal, overriding methods
    var (_, _, diagnostic) = BaseCallCheckerRecursive(context, node, 0, 0);

    ReportBaseCallDiagnostic(context, diagnostic);
  }

  public static bool ContainsAnyBaseCall (SyntaxNodeAnalysisContext context, SyntaxNode? node)
  {
    return node != null && node.DescendantNodesAndSelf().Any(cn => IsBaseCall(context, cn, false));
  }

  private static bool ContainsCorrectBaseCall (SyntaxNodeAnalysisContext context, SyntaxNode? node)
  {
    return node != null && node.DescendantNodesAndSelf().Any(cn => IsBaseCall(context, cn, true));
  }

  private static bool BaseCallCheckShouldHappen (SyntaxNodeAnalysisContext context)
  {
    var node = (MethodDeclarationSyntax)context.Node;

    if (!node.Modifiers.Any(SyntaxKind.OverrideKeyword)) return false;

    //check for IgnoreBaseCallCheck attribute
    if (HasIgnoreBaseCallCheckAttribute(context)) return false;

    //get overridden method
    var overriddenMethodAsIMethodSymbol = context.SemanticModel.GetDeclaredSymbol(node)?.OverriddenMethod;
    var overriddenMethodAsNode = overriddenMethodAsIMethodSymbol?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as MethodDeclarationSyntax;

    if (overriddenMethodAsNode != null && overriddenMethodAsNode.Modifiers.Any(SyntaxKind.AbstractKeyword))
      return false;

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
        switch (attributeDescription)
        {
          case "Remotion.Infrastructure.Analyzers.BaseCalls.BaseCallCheckAttribute(BaseCall.IsOptional)":
            return BaseCall.IsOptional;
          case "Remotion.Infrastructure.Analyzers.BaseCalls.BaseCallCheckAttribute(BaseCall.IsMandatory)":
            return BaseCall.IsMandatory;
        }
      }

      return BaseCall.Default;
    }
  }

  private static bool IsBaseCall (SyntaxNodeAnalysisContext context, SyntaxNode childNode, bool checkIfBaseCallIsRight)
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

      if (childNode is MemberAccessExpressionSyntax thisIsForSimpleLambdas)
      {
        _ = thisIsForSimpleLambdas.Expression as BaseExpressionSyntax ?? throw new InvalidOperationException();
        return true;
      }

      var expressionStatementNode = childNode as ExpressionStatementSyntax ?? throw new InvalidOperationException();
      invocationExpressionNode = expressionStatementNode.Expression as InvocationExpressionSyntax ?? throw new InvalidOperationException();
      simpleMemberAccessExpressionNode = invocationExpressionNode.Expression as MemberAccessExpressionSyntax ?? throw new InvalidOperationException();

      _ = simpleMemberAccessExpressionNode.Expression as BaseExpressionSyntax ?? throw new InvalidOperationException();
    }
    catch (InvalidOperationException)
    {
      return false;
    }


    if (context.Node is not MethodDeclarationSyntax node || !checkIfBaseCallIsRight) return true;


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

  private static (int min, int max, BaseCallType diagnostic) BaseCallCheckerRecursive (SyntaxNodeAnalysisContext context, SyntaxNode node, int min, int max)
  {
    //min... minimal number of basecalls
    //max... maximal number of basecalls
    //-1 means it returns
    //-2 means a diagnostic was found


    var listOfResults = new List<(int min, int max)>();
    var ifStatementSyntax = node as IfStatementSyntax;
    var hasElseWithNoIf = ifStatementSyntax == null;
    ElseClauseSyntax? elseClauseSyntax = null;


    do //loop over every if / elseIf / else clause
    {
      //get childNodes
      var statement = ifStatementSyntax?.Statement ?? elseClauseSyntax?.Statement;
      IEnumerable<SyntaxNode> childNodes;
      if (statement != null)
      {
        childNodes = statement is BlockSyntax blockSyntax
            ? blockSyntax.ChildNodes() //blocks with {}
            : new[] { statement }; //blocks without{}
      }
      else //not an if or else -> get childNodes of Method Declaration
      {
        var methodDeclaration = (MethodDeclarationSyntax)node;
        if (methodDeclaration.Body != null)
          childNodes = methodDeclaration.Body.ChildNodes();

        else if (methodDeclaration.ExpressionBody != null)
          childNodes = methodDeclaration.ExpressionBody.ChildNodes();

        else
          throw new Exception("expected MethodDeclaration with body or ExpressionBody as ArrowExpressionClause");
      }


      //loop over childNodes
      if (LoopOverChildNodes(childNodes, min, max, BaseCallType.Normal, out var baseCallCheckerRecursive))
        return baseCallCheckerRecursive;


      //go to next else branch
      elseClauseSyntax = ifStatementSyntax?.Else;
      ifStatementSyntax = elseClauseSyntax?.Statement as IfStatementSyntax;

      if (elseClauseSyntax != null && ifStatementSyntax == null)
        hasElseWithNoIf = true;
    } while (elseClauseSyntax != null);


    //find the overall min and max
    listOfResults.RemoveAll(item => item == (-1, -1));

    if (listOfResults.Count == 0)
      return hasElseWithNoIf ? (-1, -1, BaseCallType.Normal) : (0, 0, BaseCallType.Normal);

    min = listOfResults[0].min;
    max = listOfResults[0].max;

    //when the if does not have an else with no if in it, it is not guaranteed that it will go through a branch
    if (!hasElseWithNoIf) min = 0;

    foreach (var (minInstance, maxInstance) in listOfResults)
    {
      min = Math.Min(min, minInstance);
      max = Math.Max(max, maxInstance);
    }

    return (min, max, GetBaseCallType(min, max));


    BaseCallType GetBaseCallType (int minLocal, int maxLocal)
    {
      if (maxLocal >= 2) return BaseCallType.Multiple;
      if (minLocal == 0) return BaseCallType.None;
      return BaseCallType.Normal;
    }


    bool LoopOverChildNodes ( //return true if a diagnostic was found
        IEnumerable<SyntaxNode>? childNodes,
        int minLocal,
        int maxLocal,
        BaseCallType diagnostic,
        out (int min, int max, BaseCallType diagnostic) baseCallCheckerRecursive)
    {
      if (childNodes == null)
      {
        baseCallCheckerRecursive = (minLocal, maxLocal, diagnostic);
        return diagnostic != BaseCallType.Normal;
      }

      foreach (var childNode in childNodes)
      {
        //nested if -> recursion
        if (IsIf(childNode) || IsElse(childNode))
        {
          var (resmin, resmax, resDiagnostic) = BaseCallCheckerRecursive(context, childNode, minLocal, maxLocal);

          if (resmin == -2) //recursion found a diagnostic -> stop everything
          {
            baseCallCheckerRecursive = (resmin, resmax, resDiagnostic);
            return true;
          }

          if (resmin == -1)
          {
            diagnostic = resDiagnostic;
            minLocal = resmin;
            maxLocal = resmax;
            break;
          }

          minLocal = Math.Max(minLocal, resmin);
          maxLocal = Math.Max(maxLocal, resmax);
        }

        else if (IsReturn(childNode))
        {
          if (ContainsCorrectBaseCall(context, childNode))
          {
            minLocal++;
            maxLocal++;
          }

          diagnostic = GetBaseCallType(minLocal, maxLocal);
          minLocal = -1;
          maxLocal = -1;
          break;
        }

        else if (IsTry(childNode))
        {
          //if baseCall is in try or in catch block
          if (((TryStatementSyntax)childNode).Block.ChildNodes().Any(n => ContainsAnyBaseCall(context, n))
              || ((TryStatementSyntax)childNode).Catches.Any(c => c.ChildNodes().Any(n => ContainsAnyBaseCall(context, n))))
          {
            baseCallCheckerRecursive = (-2, -2, BaseCallType.InTryCatch);
            return true;
          }

          var newChildNodes = ((TryStatementSyntax)childNode).Finally?.Block.ChildNodes();
          if (LoopOverChildNodes(newChildNodes, minLocal, maxLocal, diagnostic, out baseCallCheckerRecursive))
            return true;
          minLocal = baseCallCheckerRecursive.min;
          maxLocal = baseCallCheckerRecursive.max;
        }

        else if (IsLoop(childNode) && ContainsAnyBaseCall(context, childNode))
          diagnostic = BaseCallType.InLoop;

        else if (IsNormalSwitch(childNode) && ContainsCorrectBaseCall(context, childNode))
        {
          var allContainBaseCall = ((SwitchStatementSyntax)childNode).ChildNodes()
              .OfType<SwitchSectionSyntax>()
              .All(switchSectionSyntax => ContainsCorrectBaseCall(context, switchSectionSyntax));

          if (allContainBaseCall)
            minLocal++;
          maxLocal++;
        }
        else if (IsSwitchExpression(childNode) && ContainsCorrectBaseCall(context, childNode))
        {
          var allContainBaseCall = ((SwitchExpressionSyntax)childNode).Arms.All(n => ContainsCorrectBaseCall(context, n));

          if (allContainBaseCall)
            minLocal++;
          maxLocal++;
        }

        else if (ContainsCorrectBaseCall(context, childNode))
        {
          minLocal++;
          maxLocal++;
        }
      }

      if (diagnostic != BaseCallType.Normal) //found a diagnostic
      {
        baseCallCheckerRecursive = (-2, -2, diagnostic);
        return true;
      }

      listOfResults.Add((minLocal, maxLocal));
      baseCallCheckerRecursive = (minLocal, maxLocal, BaseCallType.Normal);
      return false;
    }
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

        BaseCallType.InTryCatch => DiagnosticDescriptionInTryOrCatch,

        BaseCallType.InNonOverridingMethod => DiagnosticDescriptionInInNonOverridingMethod,

        _ => throw new ArgumentOutOfRangeException(),
    };
    context.ReportDiagnostic(Diagnostic.Create(diagnostic, squiggliesLocation));
  }

  public static bool HasIgnoreBaseCallCheckAttribute (SyntaxNodeAnalysisContext context)
  {
    var attributeLists = context.Node switch
    {
        MethodDeclarationSyntax methodDeclaration => methodDeclaration.AttributeLists,
        LocalFunctionStatementSyntax localFunction => localFunction.AttributeLists,
        _ => throw new Exception("Expected a method declaration or function declaration")
    };

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
  private static readonly Func<SyntaxNode, bool> IsIf = node => node is IfStatementSyntax;
  private static readonly Func<SyntaxNode, bool> IsElse = node => node is ElseClauseSyntax;
  private static readonly Func<SyntaxNode, bool> IsReturn = node => node is ReturnStatementSyntax;
  private static readonly Func<SyntaxNode, bool> IsTry = node => node is TryStatementSyntax;
  private static readonly Func<SyntaxNode, bool> IsNormalSwitch = node => node is SwitchStatementSyntax;
  private static readonly Func<SyntaxNode, bool> IsSwitchExpression = node => node is SwitchExpressionSyntax;
}