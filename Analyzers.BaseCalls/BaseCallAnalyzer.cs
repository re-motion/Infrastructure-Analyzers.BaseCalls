// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;

namespace Remotion.Infrastructure.Analyzers.BaseCalls;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BaseCallAnalyzer : DiagnosticAnalyzer
{
  //rules

  private const string c_category = "Usage";
  private const DiagnosticSeverity c_severity = DiagnosticSeverity.Warning;

  private const string c_diagnosticId = "RMBCA0001";
  private static readonly LocalizableString s_title = "Base Call missing";
  private static readonly LocalizableString s_messageFormat = "Base Call missing";
  private static readonly LocalizableString s_description = "Base Call is missing.";

  public static readonly DiagnosticDescriptor NoBaseCall = new(
      c_diagnosticId,
      s_title,
      s_messageFormat,
      c_category,
      c_severity,
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
      c_severity,
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
      c_severity,
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
      c_severity,
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
      c_severity,
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
      c_severity,
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
      c_severity,
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
      c_severity,
      true,
      s_descriptionInInNonOverridingMethod);

  private const string c_diagnosticIdError = "RMBCA0000";
  private static readonly LocalizableString s_titleError = "Error";
  private static readonly LocalizableString s_messageError = "Error: {0}";
  private static readonly LocalizableString s_descriptionError = "Error.";

  private static readonly DiagnosticDescriptor s_error = new(
      c_diagnosticIdError,
      s_titleError,
      s_messageError,
      c_category,
      c_severity,
      true,
      s_descriptionError);


  //list of Rules
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
  [
      NoBaseCall, InLoop, MultipleBaseCalls, WrongBaseCall,
      InTryOrCatch, InNonOverridingMethod, InLocalFunction,
      InAnonymousMethod, s_error
  ];

  public override void Initialize (AnalysisContext context)
  {
    context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);

    context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.LocalFunctionStatement);


    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
  }

  private static void AnalyzeMethod (SyntaxNodeAnalysisContext context)
  {
    try
    {
      //also checks if there are baseCalls in local functions
      if (context.Node is LocalFunctionStatementSyntax)
      {
        AnalyzeLocalFunction();
        return;
      }

      var node = (MethodDeclarationSyntax)context.Node;

      if (!BaseCallCheckShouldHappen(context, out var isMixin))
        return;


      // method is empty
      if (node.Body == null && node.ExpressionBody == null || !ContainsBaseOrNextCall(context, node, false, isMixin, out _))
      {
        //location of the squigglies (Method Name)
        var squiggliesLocation = Location.Create(
            node.SyntaxTree,
            node.Identifier.Span
        );
        ReportDiagnostic(context, Diagnostic.Create(NoBaseCall, squiggliesLocation));
        return;
      }

      // normal, overriding methods
      var (_, diagnostic) = BaseCallCheckerRecursive(context, node, new NumberOfBaseCalls(0), isMixin);

      if (diagnostic == null) return;

      ReportDiagnostic(context, diagnostic);
      return;

      void AnalyzeLocalFunction ()
      {
        var localFunction = context.Node as LocalFunctionStatementSyntax;

        if (BaseCallCheckShouldHappen(context, out _))
          return;


        if (localFunction == null)
          return;

        SyntaxNode body = localFunction.Body!;


        if (ContainsBaseOrNextCall(context, body, false, false, out var location))
          ReportDiagnostic(context, Diagnostic.Create(InLocalFunction, location));
      }
    }
    catch (Exception ex)
    {
      context.ReportDiagnostic(Diagnostic.Create(s_error, context.Node.GetLocation(), ex.ToString()));
    }
  }

  private static void AnalyzeAnonymousMethod (SyntaxNodeAnalysisContext context, SyntaxNode? node)
  {
    SyntaxNode body;

    if (node is SimpleLambdaExpressionSyntax simpleLambda)
      body = simpleLambda.Body;
    else if (node is AnonymousMethodExpressionSyntax anonymousMethod)
      body = anonymousMethod.Body;
    else if (node is ParenthesizedLambdaExpressionSyntax parenthesizedLambdaExpressionSyntax)
      body = parenthesizedLambdaExpressionSyntax.Body;
    else
      return;


    if (ContainsBaseOrNextCall(context, body, false, false, out var location))
      ReportDiagnostic(context, Diagnostic.Create(InAnonymousMethod, location));
  }

  private static bool ContainsBaseOrNextCall (SyntaxNodeAnalysisContext context, SyntaxNode? nodeToCheck, bool baseCallMustBeCorrect, bool isMixin, out Location? location)
  {
    if (nodeToCheck is null)
    {
      location = null;
      return false;
    }

    foreach (var childNode in nodeToCheck.DescendantNodesAndSelf())
    {
      if (childNode is not InvocationExpressionSyntax invocationExpressionSyntax)
        continue;

      if (isMixin)
      {
        var nextIdentifier = (invocationExpressionSyntax.Expression as MemberAccessExpressionSyntax)?.Expression as IdentifierNameSyntax;
        if (!(invocationExpressionSyntax.Expression is not MemberAccessExpressionSyntax
              || nextIdentifier?.Identifier.Text is "Next"
              && context.SemanticModel.GetSymbolInfo(nextIdentifier).Symbol?.OriginalDefinition.ToDisplayString()
                  is "Remotion.Mixins.Mixin<TTarget, TNext>.Next" or "Remotion.Mixins.Mixin<TTarget>.Next")) //check if it is the correct identifier
          continue;
      }
      else
      {
        if ((invocationExpressionSyntax.Expression as MemberAccessExpressionSyntax)?.Expression is not BaseExpressionSyntax)
          continue;
      }


      location = Location.Create(
          invocationExpressionSyntax.SyntaxTree,
          invocationExpressionSyntax.Span
      );


      if (!baseCallMustBeCorrect)
        return true;

      var semanticModel = context.SemanticModel;
      var node = (MethodDeclarationSyntax)context.Node;

      //Method signature
      var methodName = node.Identifier.Text;
      var parameters = node.ParameterList.Parameters;
      var numberOfParameters = parameters.Count;
      var typesOfParameters = parameters.Select(
          param =>
              semanticModel.GetDeclaredSymbol(param)?.Type).ToArray();


      //Method signature of BaseCall
      var nameOfCalledMethod = ((MemberAccessExpressionSyntax)invocationExpressionSyntax.Expression).Name.Identifier.Text;
      var arguments = invocationExpressionSyntax.ArgumentList.Arguments;
      var numberOfArguments = arguments.Count;
      var typesOfArguments = arguments.Select(
          arg =>
              semanticModel.GetTypeInfo(arg.Expression).Type).ToArray();


      //comparing method signature and method signature of BaseCall
      if (nameOfCalledMethod.Equals(methodName)
          && numberOfParameters == numberOfArguments
          && typesOfParameters.Length == typesOfArguments.Length
          && typesOfParameters.Zip(typesOfArguments, (p, a) => (p, a))
              .All(pair => SymbolEqualityComparer.Default.Equals(pair.p, pair.a)))
        return true;

      ReportDiagnostic(context, Diagnostic.Create(WrongBaseCall, location));
      return false;
    }

    location = null;
    return false;
  }

  /// <summary>
  /// Checks if the Method overrides and if there is an attribute preventing the baseCall check.
  /// </summary>
  /// <param name="context">Context is there to call other methods that need this parameter</param>
  /// <param name="isMixin">true, if the override is in context of a Mixin</param>
  /// <returns>true, if a baseCall check should happen</returns>
  /// <exception cref="ArgumentOutOfRangeException">the attribute BaseCall should only have 3 members</exception>
  /// <exception cref="Exception">This method should not throw any other exceptions</exception>
  private static bool BaseCallCheckShouldHappen (SyntaxNodeAnalysisContext context, out bool isMixin)
  {
    isMixin = false;
    if (context.Node is LocalFunctionStatementSyntax)
      return HasIgnoreBaseCallCheckAttribute();

    var node = (MethodDeclarationSyntax)context.Node;

    var doesOverride = DoesOverride(out isMixin);

    //check for IgnoreBaseCallCheck attribute
    if (HasIgnoreBaseCallCheckAttribute()) return false;


    if (!doesOverride)
    {
      if (node.Modifiers.Any(SyntaxKind.NewKeyword))
        ContainsBaseOrNextCall(context, node, true, false, out _);

      else if (ContainsBaseOrNextCall(context, node, false, false, out var location))
        ReportDiagnostic(context, Diagnostic.Create(InNonOverridingMethod, location));

      return false;
    }

    var isVoid = node.ReturnType is PredefinedTypeSyntax predefinedTypeSyntax
                 && predefinedTypeSyntax.Keyword.IsKind(SyntaxKind.VoidKeyword);

    if (isMixin)
      return isVoid;


    //get overridden method
    var currentMethod = context.SemanticModel.GetDeclaredSymbol(node);

    while (currentMethod != null)
    {
      if (currentMethod.IsAbstract)
        return false;

      var baseCallCheck = CheckForBaseCallCheckAttribute(currentMethod);
      switch (baseCallCheck)
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

      if (currentMethod.IsVirtual || currentMethod.IsAbstract)
        break;

      currentMethod = currentMethod.OverriddenMethod;
    }

    return isVoid;


    BaseCall CheckForBaseCallCheckAttribute (IMethodSymbol overriddenMethod)
    {
      foreach (var attributeDescription in overriddenMethod.GetAttributes().Select(attr => attr.ToString()).ToList())
      {
        switch (attributeDescription)
        {
          case "Remotion.Infrastructure.Analyzers.BaseCalls.BaseCallCheckAttribute(Remotion.Infrastructure.Analyzers.BaseCalls.BaseCall.IsOptional)":
            return BaseCall.IsOptional;
          case "Remotion.Infrastructure.Analyzers.BaseCalls.BaseCallCheckAttribute(Remotion.Infrastructure.Analyzers.BaseCalls.BaseCall.IsMandatory)":
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

      return ContainsAttribute(attributeLists, "Remotion.Infrastructure.Analyzers.BaseCalls.IgnoreBaseCallCheckAttribute.IgnoreBaseCallCheckAttribute()");
    }

    bool DoesOverride (out bool isMixin)
    {
      if (node.Modifiers.Any(SyntaxKind.OverrideKeyword))
      {
        isMixin = false;
        return true;
      }

      // for mixins -> check if there is an [OverrideTarget] attribute
      SyntaxList<AttributeListSyntax> attributeLists;
      if (context.Node is MethodDeclarationSyntax methodDeclaration)
        attributeLists = methodDeclaration.AttributeLists;
      else
        throw new ArgumentException("Expected a method declaration");

      var containsOverrideTargetAttribute = ContainsAttribute(attributeLists, "Remotion.Mixins.OverrideTargetAttribute.OverrideTargetAttribute()");
      isMixin = containsOverrideTargetAttribute;
      return containsOverrideTargetAttribute;
    }

    bool ContainsAttribute (SyntaxList<AttributeListSyntax> attributeLists, string searchedFullNameOfNamespace)
    {
      return attributeLists.Any(
          attributeListSyntax => attributeListSyntax.Attributes.Select(
              attribute => context.SemanticModel.GetSymbolInfo(attribute).Symbol).OfType<ISymbol>().Select(
              attributeSemanticModel => attributeSemanticModel.ToString()).Any(
              attributeFullNameOfNamespace => attributeFullNameOfNamespace.Equals(searchedFullNameOfNamespace)));
    }
  }

  /// <summary>
  /// Checks a block of Code for a BaseCall and looks if there are multiple, none, there is one in a loop, etc. It also takes different branches into consideration.
  /// Basic explanation of the algorithm (I advise you to first read the parameter descriptions):
  /// 
  /// It first looks to see if the argument node is an if-statement.
  /// If so, it loops through all the branches and checks each one for a basecall. Else it just analyzes the childNodes of the given node.
  /// Now it loops through the childNodes and check each one if it is:
  /// 
  /// If Statement:
  ///   Calls a Recursion, then check if it found a diagnostic (-> return the found diagnostic) or every branch in the if returns (return returns)
  /// Return statement:
  ///   Checks if the return statement includes a BaseCall, then returns -1, diagnostic of numberOfBaseCalls before it was -1.
  /// Try-Catch-Block:
  ///   Checks if the try or any catch block contains a basecall (-> return TryCatchDiagnostic), then calls a recursion with the finally block (same as in If).
  /// Loop:
  ///   Checks if the loop contains a basecall (-> return LoopDiagnostic)
  /// Switch:
  ///   Checks if every branch of the if contains a basecall (else -> return BaseCallMissingDiagnostic).
  /// BaseCall:
  ///   increment numberOfBaseCalls by 1.
  ///
  /// numberOfBaseCalls will be stored in listOfResults and the next branch will be checked.
  /// Returns the lowest min and the highest max in listOfResults
  /// </summary>
  /// <param name="context">is there to call other methods that need this parameter</param>
  /// <param name="node">current node to check, the node itself won't be checked, just the childNodes of it</param>
  /// <param name="numberOfBaseCalls">
  /// .min: Minimum Number of BaseCalls that could get executed
  /// .max: Maximum Number of BaseCalls that could get executed
  /// </param>
  /// magic Numbers for numberOfBaseCalls:
  ///   -1: the checked block always returns
  ///   -2: there is a diagnostic in the checked block
  /// <param name="isMixin">
  /// This project is made to work with the Framework Remotion.Mixins.
  /// If it is a mixin, a base call has the form of Next.Method(); instead of base.Method();</param>
  /// <returns>Returns the number of baseCalls in the path with the least and most baseCalls and the diagnostic</returns>
  /// /// <exception cref="NullReferenceException">
  /// will be thrown when the node that is parsed does not have a body. In the current implementation this should be impossible as it's checked before.
  /// </exception>
  /// <exception cref="Exception">
  /// This Method should not throw any other exceptions.
  /// </exception>
  private static (NumberOfBaseCalls numberOfBaseCalls, Diagnostic? diagnostic) BaseCallCheckerRecursive (
      SyntaxNodeAnalysisContext context,
      SyntaxNode node,
      NumberOfBaseCalls numberOfBaseCalls,
      bool isMixin)
  {
    const int returns = -1;
    const int diagnosticFound = -2;

    DiagnosticDescriptor? diagnosticDescriptor = null;

    var methodNameLocation = Location.Create(
        context.Node.SyntaxTree,
        ((MethodDeclarationSyntax)context.Node).Identifier.Span
    );

    var listOfResults = new List<NumberOfBaseCalls>();
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
      if (LoopOverChildNodes(childNodes, numberOfBaseCalls, null, out var baseCallCheckerRecursive))
        return baseCallCheckerRecursive;


      //go to next else branch
      elseClauseSyntax = ifStatementSyntax?.Else;
      ifStatementSyntax = elseClauseSyntax?.Statement as IfStatementSyntax;

      if (elseClauseSyntax != null && ifStatementSyntax == null)
        hasElseWithNoIf = true;
    } while (elseClauseSyntax != null);


    //find the overall min and max
    listOfResults.RemoveAll(item => item.Equals(new NumberOfBaseCalls(returns)));

    if (listOfResults.Count == 0)
      return hasElseWithNoIf ? (new NumberOfBaseCalls(returns), null) : (new NumberOfBaseCalls(0), null);

    numberOfBaseCalls.Min = listOfResults[0].Min;
    numberOfBaseCalls.Max = listOfResults[0].Max;

    //when the if does not have an else with no if in it, it is not guaranteed that it will go through a branch
    if (!hasElseWithNoIf) numberOfBaseCalls.Min = 0;

    foreach (var numberOfBaseCallsListItem in listOfResults)
    {
      numberOfBaseCalls.Min = Math.Min(numberOfBaseCalls.Min, numberOfBaseCallsListItem.Min);
      numberOfBaseCalls.Max = Math.Max(numberOfBaseCalls.Max, numberOfBaseCallsListItem.Max);
    }

    diagnosticDescriptor = GetDiagnosticDescription(numberOfBaseCalls);
    return (numberOfBaseCalls, diagnosticDescriptor == null ? null : Diagnostic.Create(diagnosticDescriptor, methodNameLocation));


    DiagnosticDescriptor? GetDiagnosticDescription (NumberOfBaseCalls numberOfBaseCallsLocal)
    {
      if (numberOfBaseCallsLocal.Max >= 2) return MultipleBaseCalls;
      return numberOfBaseCallsLocal.Min == 0 ? NoBaseCall : null;
    }


    bool LoopOverChildNodes ( //return true if a diagnostic was found
        IEnumerable<SyntaxNode>? childNodes,
        NumberOfBaseCalls numberOfBaseCallsLocal,
        Diagnostic? diagnostic,
        out (NumberOfBaseCalls numberOfBaseCalls, Diagnostic? diagnostic) result)
    {
      if (childNodes == null)
      {
        result = (numberOfBaseCallsLocal, diagnostic == null ? null : (diagnosticDescriptor == null) ? null : Diagnostic.Create(diagnosticDescriptor, methodNameLocation));
        return diagnostic != null;
      }

      foreach (var childNode in childNodes)
      {
        //nested if -> recursion
        if (s_isIf(childNode) || s_isElse(childNode))
        {
          var (numberOfBaseCallsResult, resultDiagnostic) = BaseCallCheckerRecursive(context, childNode, numberOfBaseCallsLocal, isMixin);

          if (numberOfBaseCallsResult.Min == diagnosticFound) //recursion found a diagnostic -> stop everything
          {
            result = (numberOfBaseCallsResult, resultDiagnostic);
            return true;
          }

          if (numberOfBaseCallsResult.Min == returns)
          {
            diagnostic = resultDiagnostic;
            numberOfBaseCallsLocal = new NumberOfBaseCalls(returns);
            break;
          }

          numberOfBaseCallsLocal.Min = Math.Max(numberOfBaseCallsLocal.Min, numberOfBaseCallsResult.Min);
          numberOfBaseCallsLocal.Max = Math.Max(numberOfBaseCallsLocal.Max, numberOfBaseCallsResult.Max);
        }

        else if (s_isReturn(childNode))
        {
          if (ContainsBaseOrNextCall(context, childNode, true, isMixin, out var location))
          {
            numberOfBaseCallsLocal.Increment();
          }

          var returnLocation = Location.Create(context.Node.SyntaxTree, childNode.Span);
          var diagnosticDescriptorHere = GetDiagnosticDescription(numberOfBaseCallsLocal);
          diagnostic = diagnosticDescriptorHere == null ? null : Diagnostic.Create(diagnosticDescriptorHere, location == null ? returnLocation : location);

          numberOfBaseCallsLocal.Min = returns;
          numberOfBaseCallsLocal.Max = returns;
          break;
        }

        else if (s_isTry(childNode))
        {
          //if baseCall is in try or in catch block
          Location? location = null;
          var tryHasBaseCall = false;
          foreach (var n in ((TryStatementSyntax)childNode).Block.ChildNodes())
          {
            if (ContainsBaseOrNextCall(context, n, false, isMixin, out var outLocation))
            {
              tryHasBaseCall = true;
              if (location == null)
                location = outLocation;
              break;
            }
          }

          var anyCatchHasBaseCall = false;
          foreach (var c in ((TryStatementSyntax)childNode).Catches)
          {
            var catchHasBaseCall = false;
            foreach (var n in c.ChildNodes())
            {
              if (ContainsBaseOrNextCall(context, n, false, isMixin, out var outLocation))
              {
                catchHasBaseCall = true;
                if (location == null)
                  location = outLocation;
                break;
              }
            }

            if (catchHasBaseCall)
            {
              anyCatchHasBaseCall = true;
              break;
            }
          }

          if (tryHasBaseCall || anyCatchHasBaseCall)
          {
            result = (new NumberOfBaseCalls(diagnosticFound), Diagnostic.Create(InTryOrCatch, location));
            return true;
          }

          var childNodesOfFinallyBlock = ((TryStatementSyntax)childNode).Finally?.Block.ChildNodes();
          if (LoopOverChildNodes(childNodesOfFinallyBlock, numberOfBaseCallsLocal, diagnostic, out result))
            return true;
          numberOfBaseCallsLocal = result.numberOfBaseCalls;
        }

        else if (s_isLoop(childNode) && ContainsBaseOrNextCall(context, childNode, false, isMixin, out var locationInLoop))
          diagnostic = Diagnostic.Create(InLoop, locationInLoop);

        else if (s_isNormalSwitch(childNode) && ContainsBaseOrNextCall(context, childNode, true, isMixin, out _))
        {
          var allContainBaseCall = ((SwitchStatementSyntax)childNode).ChildNodes()
              .OfType<SwitchSectionSyntax>()
              .All(switchSectionSyntax => ContainsBaseOrNextCall(context, switchSectionSyntax, true, isMixin, out _));

          if (allContainBaseCall)
            numberOfBaseCallsLocal.Increment();
          else
          {
            diagnostic = Diagnostic.Create(NoBaseCall, Location.Create(context.Node.SyntaxTree, ((SwitchStatementSyntax)childNode).SwitchKeyword.Span));
            break;
          }
        }
        else if (s_isSwitchExpression(childNode) && ContainsBaseOrNextCall(context, childNode, true, isMixin, out _))
        {
          var allContainBaseCall = ((SwitchExpressionSyntax)childNode).Arms.All(n => ContainsBaseOrNextCall(context, n, true, isMixin, out _));

          if (allContainBaseCall)
            numberOfBaseCallsLocal.Increment();
          else
          {
            diagnostic = Diagnostic.Create(NoBaseCall, Location.Create(context.Node.SyntaxTree, ((SwitchStatementSyntax)childNode).SwitchKeyword.Span));
            break;
          }
        }

        else if (s_isUsingStatement(childNode))
        {
          var childNodesOfUsingBlock = ((UsingStatementSyntax)childNode).Statement.ChildNodes();
          if (LoopOverChildNodes(childNodesOfUsingBlock, numberOfBaseCallsLocal, diagnostic, out result))
            return true;
          numberOfBaseCallsLocal = result.numberOfBaseCalls;
        }

        else if (ContainsBaseOrNextCall(context, childNode, true, isMixin, out var location4))
        {
          numberOfBaseCallsLocal.Increment();
          if (numberOfBaseCallsLocal.Max >= 2)
          {
            diagnostic = Diagnostic.Create(MultipleBaseCalls, location4);
            break;
          }
        }

        if (ContainsAnonymousMethod(childNode, out var anonymousMethod))
        {
          AnalyzeAnonymousMethod(context, anonymousMethod);
        }
      }

      if (diagnostic != null) //found a diagnostic
      {
        result = (new NumberOfBaseCalls(diagnosticFound), diagnostic);
        return true;
      }

      listOfResults.Add(numberOfBaseCallsLocal);
      result = (numberOfBaseCallsLocal, null);
      return false;
    }
  }

  private struct NumberOfBaseCalls (int min, int max)
  {
    public int Min { get; set; } = min;
    public int Max { get; set; } = max;

    public NumberOfBaseCalls (int numberOfBaseCalls)
        : this(numberOfBaseCalls, numberOfBaseCalls)
    {
    }

    public void Increment ()
    {
      Min++;
      Max++;
    }

    public static bool operator == (NumberOfBaseCalls left, NumberOfBaseCalls right)
    {
      return left.Equals(right);
    }

    public static bool operator != (NumberOfBaseCalls left, NumberOfBaseCalls right)
    {
      return !(left == right);
    }

    public override bool Equals (object? o)
    {
      return o is NumberOfBaseCalls other && Equals(other);
    }

    public bool Equals (NumberOfBaseCalls other)
    {
      return Min == other.Min && Max == other.Max;
    }

    public override int GetHashCode ()
    {
      unchecked
      {
        return (Min * 397 + 228463) ^ Max;
      }
    }
  }

  private static readonly Func<SyntaxNode, bool> s_isLoop = node => node is ForStatementSyntax or WhileStatementSyntax or ForEachStatementSyntax or DoStatementSyntax;
  private static readonly Func<SyntaxNode, bool> s_isIf = node => node is IfStatementSyntax;
  private static readonly Func<SyntaxNode, bool> s_isElse = node => node is ElseClauseSyntax;
  private static readonly Func<SyntaxNode, bool> s_isReturn = node => node is ReturnStatementSyntax;
  private static readonly Func<SyntaxNode, bool> s_isTry = node => node is TryStatementSyntax;
  private static readonly Func<SyntaxNode, bool> s_isNormalSwitch = node => node is SwitchStatementSyntax;
  private static readonly Func<SyntaxNode, bool> s_isSwitchExpression = node => node is SwitchExpressionSyntax;
  private static readonly Func<SyntaxNode, bool> s_isUsingStatement = node => node is UsingStatementSyntax;

  private static bool ContainsAnonymousMethod (SyntaxNode node, out SyntaxNode? anonymousMethod)
  {
    foreach (var childNode in node.DescendantNodesAndSelf())
    {
      switch (childNode)
      {
        case AnonymousMethodExpressionSyntax anonymousMethodFound:
          anonymousMethod = anonymousMethodFound;
          return true;
        case SimpleLambdaExpressionSyntax simpleLambdaExpressionSyntax:
          anonymousMethod = simpleLambdaExpressionSyntax;
          return true;
        case ParenthesizedLambdaExpressionSyntax parenthesizedLambdaExpressionSyntax:
          anonymousMethod = parenthesizedLambdaExpressionSyntax;
          return true;
      }
    }

    anonymousMethod = null;
    return false;
  }

  private static void ReportDiagnostic (SyntaxNodeAnalysisContext context, Diagnostic diagnostic)
  {
    context.ReportDiagnostic(diagnostic);
  }
}