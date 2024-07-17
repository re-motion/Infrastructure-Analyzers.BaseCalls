// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Remotion.Infrastructure.Analyzers.BaseCalls;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BaseCallAnalyzer : DiagnosticAnalyzer
{
  //rules
  private static readonly DiagnosticDescriptor? NoDiagnostic = null;

  private const string c_category = "Usage";

  private const string c_diagnosticId = "RMBCA0001";
  private static readonly LocalizableString s_title = "Base Call missing";
  private static readonly LocalizableString s_messageFormat = "Base Call missing";
  private static readonly LocalizableString s_description = "Base Call is missing.";

  public static readonly DiagnosticDescriptor NoBaseCall = new(
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

  public static readonly DiagnosticDescriptor InLoop = new(
      c_diagnosticIdLoopMessage,
      s_titleLoopMessage,
      s_messageFormatLoopMessage,
      c_category,
      DiagnosticSeverity.Warning,
      true,
      s_descriptionLoopMessage);

  private const string c_diagnosticIdAnonymousMethod = "RMBCA0003";
  private static readonly LocalizableString s_titleAnonymousMethod = "Base Call found in anonymous method";
  private static readonly LocalizableString s_messageFormatAnonymousMethod = "Base Call is not allowed in anonymous methods";
  private static readonly LocalizableString s_descriptionAnonymousMethod = "Base Calls should not be used in anonymous methods.";

  public static readonly DiagnosticDescriptor InAnonymousMethod = new(
      c_diagnosticIdAnonymousMethod,
      s_titleAnonymousMethod,
      s_messageFormatAnonymousMethod,
      c_category,
      DiagnosticSeverity.Warning,
      true,
      s_descriptionAnonymousMethod);

  private const string c_diagnosticIdLocalFunction = "RMBCA0004";
  private static readonly LocalizableString s_titleLocalFunction = "Base Call found in local function";
  private static readonly LocalizableString s_messageFormatLocalFunction = "Base Call is not allowed in local function";
  private static readonly LocalizableString s_descriptionLocalFunction = "Base Calls should not be used in local function.";

  public static readonly DiagnosticDescriptor InLocalFunction = new(
      c_diagnosticIdLocalFunction,
      s_titleLocalFunction,
      s_messageFormatLocalFunction,
      c_category,
      DiagnosticSeverity.Warning,
      true,
      s_descriptionLocalFunction);


  private const string c_diagnosticIdMultipleBaseCalls = "RMBCA0005";
  private static readonly LocalizableString s_titleMultipleBaseCalls = "multiple BaseCalls found";
  private static readonly LocalizableString s_messageMultipleBaseCalls = "multiple BaseCalls found";
  private static readonly LocalizableString s_descriptionMultipleBaseCalls = "multiple BaseCalls found in this method, there should only be one BaseCall.";

  public static readonly DiagnosticDescriptor MultipleBaseCalls = new(
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

  public static readonly DiagnosticDescriptor WrongBaseCall = new(
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

  public static readonly DiagnosticDescriptor InTryOrCatch = new(
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

  public static readonly DiagnosticDescriptor InNonOverridingMethod = new(
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
      NoBaseCall, InLoop, MultipleBaseCalls, WrongBaseCall,
      InTryOrCatch, InNonOverridingMethod, InLocalFunction,
      InAnonymousMethod
  ];

  public override void Initialize (AnalysisContext context)
  {
    context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);

    context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.AnonymousMethodExpression);
    context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.SimpleLambdaExpression);
    context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.ParenthesizedLambdaExpression);

    context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.LocalFunctionStatement);


    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
  }

  private static void AnalyzeMethod (SyntaxNodeAnalysisContext context)
  {
    var generalNode = context.Node;
    switch (generalNode)
    {
      case AnonymousMethodExpressionSyntax or SimpleLambdaExpressionSyntax or ParenthesizedLambdaExpressionSyntax:
        AnalyzeAnonymousMethod();
        return;
      case LocalFunctionStatementSyntax:
        AnalyzeLocalFunction();
        return;
    }

    var node = (generalNode as MethodDeclarationSyntax)!;

    var isMixin = IsMixin(context);

    if (!BaseCallCheckShouldHappen(context)) return;

    // method is empty
    if (node.Body == null && node.ExpressionBody == null)
    {
      ReportDiagnostic(context, NoBaseCall);
      return;
    }

    // normal, overriding methods
    var (_, _, diagnostic) = BaseCallCheckerRecursive(context, node, 0, 0, isMixin);

    ReportDiagnostic(context, diagnostic);
    return;


    void AnalyzeAnonymousMethod ()
    {
      var anonymousMethod = context.Node as AnonymousFunctionExpressionSyntax;
      if (anonymousMethod == null)
        return;

      SyntaxNode body;

      if (anonymousMethod is SimpleLambdaExpressionSyntax simpleLambda)
        body = simpleLambda.Body;
      else
        body = anonymousMethod.Body;

      if (ContainsBaseCall(context, body, false, false))
      {
        var squiggliesLocation = anonymousMethod.GetLocation();
        context.ReportDiagnostic(Diagnostic.Create(InAnonymousMethod, squiggliesLocation));
      }
    }

    void AnalyzeLocalFunction ()
    {
      var localFunction = context.Node as LocalFunctionStatementSyntax;

      if (BaseCallCheckShouldHappen(context))
        return;


      if (localFunction == null)
        return;

      SyntaxNode body = localFunction.Body!;


      if (ContainsBaseCall(context, body, false, false))
      {
        var localNode = (LocalFunctionStatementSyntax)context.Node;
        //location of the squigglies (whole line of the method declaration)
        var squiggliesLocation = Location.Create(
            localNode.SyntaxTree,
            TextSpan.FromBounds(
                localNode.GetLeadingTrivia().Span.End,
                localNode.ParameterList.Span.End
            )
        );
        context.ReportDiagnostic(Diagnostic.Create(InLocalFunction, squiggliesLocation));
      }
    }
  }

  private static bool IsMixin (SyntaxNodeAnalysisContext context)
  {
    var node = context.Node;

    while (true)
    {
      var classDeclaration = node.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();

      var baseTypeSyntax = classDeclaration?.BaseList?.Types.FirstOrDefault()?.Type;
      if (baseTypeSyntax == null)
        return false;

      if (context.SemanticModel.GetSymbolInfo(baseTypeSyntax).Symbol != null && context.SemanticModel.GetSymbolInfo(baseTypeSyntax).Symbol is INamedTypeSymbol baseTypeSymbol)
      {
        var baseClassFullName = baseTypeSymbol.ToDisplayString();

        if (baseClassFullName.StartsWith("Remotion.Mixins.Mixin<") && baseClassFullName.EndsWith(">"))
          return true;

        // Recursively check the base class
        if (baseTypeSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is ClassDeclarationSyntax baseClassDeclaration)
        {
          node = baseClassDeclaration;
          continue;
        }
      }

      return false;
    }
  }

  private static bool ContainsBaseCall (SyntaxNodeAnalysisContext context, SyntaxNode? outerNode, bool mustBeCorrect, bool isMixin)
  {
    return outerNode != null && outerNode.DescendantNodesAndSelf().Any(IsBaseOrNextCall);

    bool IsBaseOrNextCall (SyntaxNode childNode)
    {
      var node = (context.Node as MethodDeclarationSyntax)!;

      InvocationExpressionSyntax? invocationExpressionNode;
      MemberAccessExpressionSyntax? simpleMemberAccessExpressionNode;


      //trying to cast the childNode to an BaseExpressionSyntax, example Syntax tree:
      /*
       MethodDeclaration
        Body: Block
          Expression: InvocationExpression
            Expression: SimpleMemberAccessExpression
              Expression: BaseExpression
      */
      if (!isMixin)
      {
        if (childNode is MemberAccessExpressionSyntax thisIsForSimpleLambdas)
          return thisIsForSimpleLambdas.Expression is BaseExpressionSyntax;

        var expressionStatementNode = childNode as ExpressionStatementSyntax;
        invocationExpressionNode = expressionStatementNode?.Expression as InvocationExpressionSyntax;
        simpleMemberAccessExpressionNode = invocationExpressionNode?.Expression as MemberAccessExpressionSyntax;
        var baseExpressionSyntax = simpleMemberAccessExpressionNode?.Expression as BaseExpressionSyntax;

        if (baseExpressionSyntax == null) return false;
      }
      else
      {
        var nextIdentifierText = (((childNode as InvocationExpressionSyntax)?.Expression as MemberAccessExpressionSyntax)?.Expression as IdentifierNameSyntax)?.Identifier.Text;
        if (nextIdentifierText is not "Next")
          return false;

        invocationExpressionNode = childNode as InvocationExpressionSyntax;
        simpleMemberAccessExpressionNode = invocationExpressionNode?.Expression as MemberAccessExpressionSyntax;

        if (simpleMemberAccessExpressionNode == null) return false;
      }


      if (!mustBeCorrect) return true;


      //Method signature
      var methodName = node.Identifier.Text;
      var parameters = node.ParameterList.Parameters;
      var numberOfParameters = parameters.Count;
      var typesOfParameters = parameters.Select(
          param =>
              context.SemanticModel.GetDeclaredSymbol(param)?.Type).ToArray();


      //Method signature of BaseCall
      var nameOfCalledMethod = simpleMemberAccessExpressionNode!.Name.Identifier.Text;
      var arguments = invocationExpressionNode!.ArgumentList.Arguments;
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
      context.ReportDiagnostic(Diagnostic.Create(WrongBaseCall, squiggliesLocation));
      return true;
    }
  }

  private static bool BaseCallCheckShouldHappen (SyntaxNodeAnalysisContext context)
  {
    if (context.Node is LocalFunctionStatementSyntax) return HasIgnoreBaseCallCheckAttribute();

    var node = (MethodDeclarationSyntax)context.Node;

    //check for IgnoreBaseCallCheck attribute
    if (HasIgnoreBaseCallCheckAttribute()) return false;


    if (!DoesOverride())
    {
      if (ContainsBaseCall(context, node, false, false))
        ReportDiagnostic(context, InNonOverridingMethod);
      return false;
    }

    if (IsMixin(context))
      return ((PredefinedTypeSyntax)node.ReturnType).Keyword.IsKind(SyntaxKind.VoidKeyword);


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
            object? valueOfEnumArgument = null;
            if (attr.ConstructorArguments.Length > 0)
              valueOfEnumArgument = attr.ConstructorArguments[0].Value;
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

    bool HasIgnoreBaseCallCheckAttribute ()
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
    
    bool DoesOverride ()
    {
      if (node.Modifiers.Any(SyntaxKind.OverrideKeyword)) return true;

      if (!IsMixin(context)) return false;
      
      // for mixins -> check if there is an [OverrideTarget] attribute
      SyntaxList<AttributeListSyntax> attributeLists;
      if (context.Node is MethodDeclarationSyntax methodDeclaration)
        attributeLists = methodDeclaration.AttributeLists;
      else
        throw new Exception("Expected a method declaration");


      foreach (var attributeListSyntax in attributeLists)
      foreach (var attribute in attributeListSyntax.Attributes)
      {
        var imSymbol = context.SemanticModel.GetSymbolInfo(attribute).Symbol;

        if (imSymbol == null) continue;
        var fullNameOfNamespace = imSymbol.ToString();

        if (fullNameOfNamespace.Equals("Remotion.Mixins.OverrideTargetAttribute.OverrideTargetAttribute()"))
          return true;
      }
      return false;
    }
  }

  /// <summary>
  /// Checks a block of Code for a BaseCall and looks if there are multiple, none, there is one in a loop, etc.. It also takes different branches into consideration.
  /// Basic explanation of the algorithm (I advice you to first read the parameter descriptions):
  /// 
  /// It first looks to see if the argument node is an if-statement.
  /// If so, it loops through all of the branches and checks each one for a basecall. Else it just analyzes the childNodes of the given node.
  /// Now it loops through the childNodes and check each one if it is:
  /// 
  /// If Statement:
  ///   Calls a Recursion, then check if it found a diagnostic (-> return the found diagnostic) or every branch in the if returns (return returns)
  /// Return statement:
  ///   Checks if the return statement includes a BaseCall, then returns the -1, -1, diagnostic of min and max before they were -1.
  /// Try-Catch-Block:
  ///   Checks if the try or any catch block contains a basecall (-> return TryCatchDiagnostic), then calls a recursion with the finally block (same as in If).
  /// Loop:
  ///   Checks if the loop contains a basecall (-> return LoopDiagnostic)
  /// Switch:
  ///   Checks if every branch of the if contains a basecall (else -> return BaseCallMissingDiagnostic).
  /// BaseCall:
  ///   increment min and max by 1.
  ///
  /// min and max will be stored in listOfResults and the next branch will be checked.
  /// Returns the lowest min and the highest max in listOfResults
  /// </summary>
  /// <param name="context">is there to call other methods that need this parameter</param>
  /// <param name="node">current node to check, the node itself wont be checked, just the childNodes of it</param>
  /// <param name="min">Minimum Number of BaseCalls that could get executed</param>
  /// <param name="max">Maximum Number of BaseCalls that could get executed</param>
  /// magic Numbers for min and max:
  ///   -1: the checked block always returns
  ///   -2: there is a diagnostic in the checked block
  /// <param name="isMixin">
  /// This project is made to work with the Framework Remotion.Mixins.
  /// If it is a mixin, a base call has the form of Next.Method(); instead of base.Method();</param>
  /// <returns>Returns the number of baseCalls in the path with the least and most baseCalls and the diagnostic</returns>
  /// <exception cref="Exception">
  /// NullReferenceException("expected MethodDeclaration with body or ExpressionBody as ArrowExpressionClause (method does not have a body)")
  ///   will be thrown when the node that is parsed does not have a body. In the current implementation this should be impossible as its checked before.
  /// This Method should not throw any other exceptions.
  /// </exception>
  private static (int min, int max, DiagnosticDescriptor? diagnostic) BaseCallCheckerRecursive (SyntaxNodeAnalysisContext context, SyntaxNode node, int min, int max, bool isMixin)
  {
    const int returns = -1;
    const int diagnosticFound = -2;

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
          throw new NullReferenceException("expected MethodDeclaration with body or ExpressionBody as ArrowExpressionClause (method does not have a body)");
      }


      //loop over childNodes
      if (LoopOverChildNodes(childNodes, min, max, NoDiagnostic, out var baseCallCheckerRecursive))
        return baseCallCheckerRecursive;


      //go to next else branch
      elseClauseSyntax = ifStatementSyntax?.Else;
      ifStatementSyntax = elseClauseSyntax?.Statement as IfStatementSyntax;

      if (elseClauseSyntax != null && ifStatementSyntax == null)
        hasElseWithNoIf = true;
    } while (elseClauseSyntax != null);


    //find the overall min and max
    listOfResults.RemoveAll(item => item == (returns, returns));

    if (listOfResults.Count == 0)
      return hasElseWithNoIf ? (returns, returns, NoDiagnostic) : (0, 0, NoDiagnostic);

    min = listOfResults[0].min;
    max = listOfResults[0].max;

    //when the if does not have an else with no if in it, it is not guaranteed that it will go through a branch
    if (!hasElseWithNoIf) min = 0;

    foreach (var (minInstance, maxInstance) in listOfResults)
    {
      min = Math.Min(min, minInstance);
      max = Math.Max(max, maxInstance);
    }

    return (min, max, GetDiagnosticDescription(min, max));


    DiagnosticDescriptor? GetDiagnosticDescription (int minLocal, int maxLocal)
    {
      if (maxLocal >= 2) return MultipleBaseCalls;
      return minLocal == 0 ? NoBaseCall : NoDiagnostic;
    }


    bool LoopOverChildNodes ( //return true if a diagnostic was found
        IEnumerable<SyntaxNode>? childNodes,
        int minLocal,
        int maxLocal,
        DiagnosticDescriptor? diagnostic,
        out (int min, int max, DiagnosticDescriptor? diagnostic) baseCallCheckerRecursive)
    {
      if (childNodes == null)
      {
        baseCallCheckerRecursive = (minLocal, maxLocal, diagnostic);
        return diagnostic != null;
      }

      foreach (var childNode in childNodes)
      {
        //nested if -> recursion
        if (IsIf(childNode) || IsElse(childNode))
        {
          var (resultMin, resultMax, resDiagnostic) = BaseCallCheckerRecursive(context, childNode, minLocal, maxLocal, isMixin);

          if (resultMin == diagnosticFound) //recursion found a diagnostic -> stop everything
          {
            baseCallCheckerRecursive = (resultMin, resultMax, resDiagnostic);
            return true;
          }

          if (resultMin == returns)
          {
            diagnostic = resDiagnostic;
            minLocal = returns;
            maxLocal = returns;
            break;
          }

          minLocal = Math.Max(minLocal, resultMin);
          maxLocal = Math.Max(maxLocal, resultMax);
        }

        else if (IsReturn(childNode))
        {
          if (ContainsBaseCall(context, childNode, true, isMixin))
          {
            minLocal++;
            maxLocal++;
          }

          diagnostic = GetDiagnosticDescription(minLocal, maxLocal);
          minLocal = returns;
          maxLocal = returns;
          break;
        }

        else if (IsTry(childNode))
        {
          //if baseCall is in try or in catch block
          if (((TryStatementSyntax)childNode).Block.ChildNodes().Any(n => ContainsBaseCall(context, n, false, isMixin))
              || ((TryStatementSyntax)childNode).Catches.Any(c => c.ChildNodes().Any(n => ContainsBaseCall(context, n, false, isMixin))))
          {
            baseCallCheckerRecursive = (diagnosticFound, diagnosticFound, InTryOrCatch);
            return true;
          }

          var newChildNodes = ((TryStatementSyntax)childNode).Finally?.Block.ChildNodes();
          if (LoopOverChildNodes(newChildNodes, minLocal, maxLocal, diagnostic, out baseCallCheckerRecursive))
            return true;
          minLocal = baseCallCheckerRecursive.min;
          maxLocal = baseCallCheckerRecursive.max;
        }

        else if (IsLoop(childNode) && ContainsBaseCall(context, childNode, false, isMixin))
          diagnostic = InLoop;

        else if (IsNormalSwitch(childNode) && ContainsBaseCall(context, childNode, true, isMixin))
        {
          var allContainBaseCall = ((SwitchStatementSyntax)childNode).ChildNodes()
              .OfType<SwitchSectionSyntax>()
              .All(switchSectionSyntax => ContainsBaseCall(context, switchSectionSyntax, true, isMixin));

          if (allContainBaseCall)
            minLocal++;
          maxLocal++;
        }
        else if (IsSwitchExpression(childNode) && ContainsBaseCall(context, childNode, true, isMixin))
        {
          var allContainBaseCall = ((SwitchExpressionSyntax)childNode).Arms.All(n => ContainsBaseCall(context, n, true, isMixin));

          if (allContainBaseCall)
            minLocal++;
          maxLocal++;
        }

        else if (ContainsBaseCall(context, childNode, true, isMixin))
        {
          minLocal++;
          maxLocal++;
        }
      }

      if (diagnostic != null) //found a diagnostic
      {
        baseCallCheckerRecursive = (diagnosticFound, diagnosticFound, diagnostic);
        return true;
      }

      listOfResults.Add((minLocal, maxLocal));
      baseCallCheckerRecursive = (minLocal, maxLocal, NoDiagnostic);
      return false;
    }
  }

  private static void ReportDiagnostic (SyntaxNodeAnalysisContext context, DiagnosticDescriptor? descriptor)
  {
    if (descriptor == null)
      return;

    var node = (MethodDeclarationSyntax)context.Node;
    //location of the squigglies (whole line of the method declaration)
    var squiggliesLocation = Location.Create(
        node.SyntaxTree,
        node.Identifier.Span
    );

    context.ReportDiagnostic(Diagnostic.Create(descriptor, squiggliesLocation));
  }

  private static readonly Func<SyntaxNode, bool> IsLoop = node => node is ForStatementSyntax or WhileStatementSyntax or ForEachStatementSyntax or DoStatementSyntax;
  private static readonly Func<SyntaxNode, bool> IsIf = node => node is IfStatementSyntax;
  private static readonly Func<SyntaxNode, bool> IsElse = node => node is ElseClauseSyntax;
  private static readonly Func<SyntaxNode, bool> IsReturn = node => node is ReturnStatementSyntax;
  private static readonly Func<SyntaxNode, bool> IsTry = node => node is TryStatementSyntax;
  private static readonly Func<SyntaxNode, bool> IsNormalSwitch = node => node is SwitchStatementSyntax;
  private static readonly Func<SyntaxNode, bool> IsSwitchExpression = node => node is SwitchExpressionSyntax;
}