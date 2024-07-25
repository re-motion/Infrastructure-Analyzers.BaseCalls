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

  private const string c_diagnosticIdSwitch = "RMBCA0009";
  private static readonly LocalizableString s_titleSwitch = "Base Call found in Switch";
  private static readonly LocalizableString s_messageFormatSwitch = "Base Call is not allowed in Switch";
  private static readonly LocalizableString s_descriptionSwitch = "Base Calls should not be used in Switch.";

  public static readonly DiagnosticDescriptor InSwitch = new(
      c_diagnosticIdSwitch,
      s_titleSwitch,
      s_messageFormatSwitch,
      c_category,
      c_severity,
      true,
      s_descriptionSwitch);

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
      InAnonymousMethod, InSwitch, s_error
  ];

  public override void Initialize (AnalysisContext context)
  {
    context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);

    context.RegisterSyntaxNodeAction(AnalyzeLocalFunction, SyntaxKind.LocalFunctionStatement);


    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
  }

  private static void AnalyzeMethod (SyntaxNodeAnalysisContext context)
  {
    try
    {
      if (AttributeChecks.HasIgnoreBaseCallCheckAttribute(context))
      {
        return;
      }

      var node = context.Node as MethodDeclarationSyntax
                 ?? throw new ArgumentException("expected MethodDeclarationSyntax");

      if (!DoesOverride(context, out var isMixin))
      {
        var baseCalls = GetBaseCalls(context, node, isMixin).ToArray();
        if (node.Modifiers.Any(SyntaxKind.NewKeyword))
        {
          ReportAllWrong(context, baseCalls);
        }
        else if (baseCalls.Any())
        {
          ReportAll(context, baseCalls, InNonOverridingMethod);
        }

        return;
      }

      if (BaseCallMustBePresent(context, isMixin))
      {
        var diagnostic = BaseCallCheckerInitializer(context, isMixin);
        ReportDiagnostic(context, diagnostic);
      }
    }
    catch (Exception ex)
    {
      //for debugging, comment return and uncomment reportDiagnostic
      //return;
      context.ReportDiagnostic(Diagnostic.Create(s_error, context.Node.GetLocation(), ex.ToString()));
    }
  }

  private static void ReportAll (SyntaxNodeAnalysisContext context, BaseCallDescriptor[] baseCalls, DiagnosticDescriptor diagnosticDescriptor)
  {
    foreach (var baseCall in baseCalls)
    {
      context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, baseCall.Location));
    }
  }

  private static void ReportAllWrong (SyntaxNodeAnalysisContext context, BaseCallDescriptor[] baseCalls)
  {
    baseCalls = baseCalls.Where(descriptor => !descriptor.CallsBaseMethod).ToArray();
    ReportAll(context, baseCalls, WrongBaseCall);
  }

  private static void AnalyzeLocalFunction (SyntaxNodeAnalysisContext context)
  {
    SyntaxNode? body = ((LocalFunctionStatementSyntax)context.Node).Body;

    if (body is null)
    {
      return;
    }

    ReportAll(context, GetBaseCalls(context, body, false).ToArray(), InLocalFunction);
  }

  private static bool BaseCallMustBePresent (SyntaxNodeAnalysisContext context, bool isMixin)
  {
    var node = context.Node as MethodDeclarationSyntax
               ?? throw new ArgumentException("expected MethodDeclarationSyntax");

    var checkType = isMixin
        ? BaseCall.Default //TODO check microsoft cci based base call checker
        : AttributeChecks.CheckForBaseCallCheckAttribute(context);

    if (checkType is BaseCall.Default)
    {
      var isVoid = node.ReturnType is PredefinedTypeSyntax predefinedTypeSyntax
                   && predefinedTypeSyntax.Keyword.IsKind(SyntaxKind.VoidKeyword);

      checkType = isVoid ? BaseCall.IsMandatory : BaseCall.IsOptional;
    }

    if (checkType is BaseCall.IsOptional)
    {
      if (!ContainsBaseCall(context, node, isMixin, out _))
      {
        return false;
      }
    }

    return true;
  }

  private static bool DoesOverride (SyntaxNodeAnalysisContext context, out bool isMixin)
  {
    var node = context.Node as MethodDeclarationSyntax
               ?? throw new ArgumentException("expected MethodDeclarationSyntax");

    if (node.Modifiers.Any(SyntaxKind.OverrideKeyword))
    {
      isMixin = false;
      return true;
    }

    // for mixins -> check if there is an [OverrideTarget] attribute
    var hasOverrideTargetAttribute = AttributeChecks.HasOverrideTargetAttribute(context);
    isMixin = hasOverrideTargetAttribute;
    return hasOverrideTargetAttribute;
  }

  private static void ReportDiagnostic (SyntaxNodeAnalysisContext context, Diagnostic? diagnostic)
  {
    if (diagnostic == null)
    {
      return;
    }

    context.ReportDiagnostic(diagnostic);
  }


  //TODO move to separate class

  #region CheckForBaseCall

  private static bool IsLoop (SyntaxNode node) => node is ForStatementSyntax or WhileStatementSyntax or ForEachStatementSyntax or DoStatementSyntax;
  private static bool IsIf (SyntaxNode node) => node is IfStatementSyntax;
  private static bool IsElse (SyntaxNode node) => node is ElseClauseSyntax;
  private static bool IsReturn (SyntaxNode node) => node is ReturnStatementSyntax;
  private static bool IsTry (SyntaxNode node) => node is TryStatementSyntax;
  private static bool IsNormalSwitch (SyntaxNode node) => node is SwitchStatementSyntax;
  private static bool IsSwitchExpression (SyntaxNode node) => node is SwitchExpressionSyntax;
  private static bool IsUsingStatement (SyntaxNode node) => node is UsingStatementSyntax;

  private static IEnumerable<BaseCallDescriptor> GetBaseCalls (SyntaxNodeAnalysisContext context, SyntaxNode? nodeToCheck, bool isMixin)
  {
    //TODO return all base calls, do not report (no side effects)
    var childNodes = nodeToCheck is null ? [] : nodeToCheck.DescendantNodesAndSelf();

    foreach (var childNode in childNodes)
    {
      if (childNode is not InvocationExpressionSyntax invocationExpressionSyntax)
      {
        continue;
      }

      if (invocationExpressionSyntax.Expression is not MemberAccessExpressionSyntax memberAccessExpressionSyntax)
      {
        continue;
      }

      if (isMixin)
      {
        var nextIdentifier = memberAccessExpressionSyntax.Expression as IdentifierNameSyntax;

        var isCorrectIdentifier = false;

        if (nextIdentifier?.Identifier.Text == "Next")
        {
          var symbolDefinition = context.SemanticModel.GetSymbolInfo(nextIdentifier).Symbol?.OriginalDefinition.ToDisplayString();

          isCorrectIdentifier = symbolDefinition is "Remotion.Mixins.Mixin<TTarget, TNext>.Next" or "Remotion.Mixins.Mixin<TTarget>.Next";
        }

        if (!isCorrectIdentifier)
        {
          continue;
        }
      }
      else
      {
        if (memberAccessExpressionSyntax.Expression is not BaseExpressionSyntax)
        {
          continue;
        }
      }


      var location = Location.Create(
          invocationExpressionSyntax.SyntaxTree,
          invocationExpressionSyntax.Span
      );


      if (context.Node is LocalFunctionStatementSyntax)
      {
        yield return new BaseCallDescriptor(location, false);
        yield break;
      }

      var semanticModel = context.SemanticModel;
      var node = context.Node as MethodDeclarationSyntax
                 ?? throw new ArgumentException("expected MethodDeclarationSyntax");

      //Method signature
      var methodName = node.Identifier.Text;
      var parameters = node.ParameterList.Parameters;
      var numberOfParameters = parameters.Count;
      var typesOfParameters = parameters.Select(
          param =>
              semanticModel.GetDeclaredSymbol(param)?.Type).ToArray();


      //Method signature of BaseCall
      var nameOfCalledMethod = memberAccessExpressionSyntax.Name.Identifier.Text;
      var arguments = invocationExpressionSyntax.ArgumentList.Arguments;
      var numberOfArguments = arguments.Count;
      var typesOfArguments = arguments.Select(
          arg =>
              semanticModel.GetTypeInfo(arg.Expression).Type).ToArray();


      //comparing method signature and method signature of BaseCall
      var isCorrect = nameOfCalledMethod.Equals(methodName)
                      && numberOfParameters == numberOfArguments
                      && typesOfParameters.Length == typesOfArguments.Length
                      && typesOfParameters.Zip(typesOfArguments, (p, a) => (p, a))
                          .All(pair => SymbolEqualityComparer.Default.Equals(pair.p, pair.a));


      yield return new BaseCallDescriptor(location, isCorrect);
    }
  }

  private static Diagnostic? BaseCallCheckerInitializer (SyntaxNodeAnalysisContext context, bool isMixin)
  {
    return BaseCallChecker(context, context.Node, new NumberOfBaseCalls(0), isMixin).diagnostic;
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
  private static (NumberOfBaseCalls numberOfBaseCalls, Diagnostic? diagnostic) BaseCallChecker (
      SyntaxNodeAnalysisContext context,
      SyntaxNode node,
      NumberOfBaseCalls numberOfBaseCalls,
      bool isMixin)
  {
    DiagnosticDescriptor? diagnosticDescriptor = null;

    var methodNameLocation = Location.Create(
        context.Node.SyntaxTree,
        ((MethodDeclarationSyntax)context.Node).Identifier.Span
    );

    var listOfResults = new List<NumberOfBaseCalls>();
    var ifStatementSyntax = node as IfStatementSyntax;
    var hasUnconditionalElse = ifStatementSyntax == null;
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
        {
          childNodes = methodDeclaration.Body.ChildNodes();
        }
        else if (methodDeclaration.ExpressionBody != null)
        {
          childNodes = methodDeclaration.ExpressionBody.ChildNodes();
        }
        else
        {
          throw new InvalidOperationException("expected MethodDeclaration with body or ExpressionBody as ArrowExpressionClause (method does not have a body)");
        }
      }

      //loop over childNodes
      if (LoopOverChildNodes(context, diagnosticDescriptor, isMixin, listOfResults, childNodes, numberOfBaseCalls, null, out var baseCallCheckerRecursive))
      {
        return baseCallCheckerRecursive;
      }

      //go to next else branch
      elseClauseSyntax = ifStatementSyntax?.Else;
      ifStatementSyntax = elseClauseSyntax?.Statement as IfStatementSyntax;

      if (elseClauseSyntax != null && ifStatementSyntax == null)
      {
        hasUnconditionalElse = true;
      }
    } while (elseClauseSyntax != null);


    //find the overall min and max
    listOfResults.RemoveAll(item => item.Equals(NumberOfBaseCalls.Returns));

    if (listOfResults.Count == 0)
    {
      return hasUnconditionalElse ? (NumberOfBaseCalls.Returns, null) : (new NumberOfBaseCalls(0), null);
    }

    numberOfBaseCalls = listOfResults[0];


    //when the if does not have an else with no if in it, it is not guaranteed that it will go through a branch
    if (!hasUnconditionalElse)
    {
      numberOfBaseCalls.Min = 0;
    }

    foreach (var numberOfBaseCallsListItem in listOfResults)
    {
      numberOfBaseCalls.Min = Math.Min(numberOfBaseCalls.Min, numberOfBaseCallsListItem.Min);
      numberOfBaseCalls.Max = Math.Max(numberOfBaseCalls.Max, numberOfBaseCallsListItem.Max);
    }

    diagnosticDescriptor = GetDiagnosticDescription(numberOfBaseCalls);
    return (numberOfBaseCalls, diagnosticDescriptor == null ? null : Diagnostic.Create(diagnosticDescriptor, methodNameLocation));
  }

  private static DiagnosticDescriptor? GetDiagnosticDescription (NumberOfBaseCalls numberOfBaseCallsLocal)
  {
    if (numberOfBaseCallsLocal.Max >= 2)
    {
      return MultipleBaseCalls;
    }

    return numberOfBaseCallsLocal.Min == 0 ? NoBaseCall : null;
  }

  private static bool LoopOverChildNodes ( //returns true if a diagnostic was found
      SyntaxNodeAnalysisContext context,
      DiagnosticDescriptor? diagnosticDescriptor,
      bool isMixin,
      List<NumberOfBaseCalls> listOfResults,
      IEnumerable<SyntaxNode>? childNodes,
      NumberOfBaseCalls numberOfBaseCalls,
      Diagnostic? diagnostic,
      out (NumberOfBaseCalls numberOfBaseCalls, Diagnostic? diagnostic) result)
  {
    var methodNameLocation = Location.Create(
        context.Node.SyntaxTree,
        ((MethodDeclarationSyntax)context.Node).Identifier.Span
    );

    if (childNodes == null)
    {
      result = (numberOfBaseCalls, diagnostic == null ? null : diagnosticDescriptor == null ? null : Diagnostic.Create(diagnosticDescriptor, methodNameLocation));
      return diagnostic != null;
    }

    foreach (var childNode in childNodes)
    {
      //nested if -> recursion
      if (IsIf(childNode) || IsElse(childNode))
      {
        var (numberOfBaseCallsResult, resultDiagnostic) = BaseCallChecker(context, childNode, numberOfBaseCalls, isMixin);

        if (numberOfBaseCallsResult.Equals(NumberOfBaseCalls.DiagnosticFound)) //recursion found a diagnostic -> stop everything
        {
          result = (numberOfBaseCallsResult, resultDiagnostic);
          return true;
        }

        if (numberOfBaseCallsResult.Equals(NumberOfBaseCalls.Returns))
        {
          diagnostic = resultDiagnostic;
          numberOfBaseCalls = NumberOfBaseCalls.Returns;
          break;
        }

        numberOfBaseCalls.Min = Math.Max(numberOfBaseCalls.Min, numberOfBaseCallsResult.Min);
        numberOfBaseCalls.Max = Math.Max(numberOfBaseCalls.Max, numberOfBaseCallsResult.Max);
      }

      else if (IsReturn(childNode))
      {
        var baseCalls = GetBaseCalls(context, childNode, isMixin).ToArray();
        ReportAllWrong(context, baseCalls);
        Location? location = null;
        if (baseCalls.Length > 0)
        {
          numberOfBaseCalls.Increment();
          location = baseCalls[0].Location;
        }

        var returnLocation = Location.Create(context.Node.SyntaxTree, childNode.Span);

        location = location == null ? returnLocation : location;

        var diagnosticDescriptorHere = GetDiagnosticDescription(numberOfBaseCalls);

        diagnostic = diagnosticDescriptorHere == null ? null : Diagnostic.Create(diagnosticDescriptorHere, location);

        numberOfBaseCalls = NumberOfBaseCalls.Returns;
        break;
      }
      //TODO check for throw and just blocks


      else if (IsTry(childNode)) //TODO try is normal
      {
        //if baseCall is in try or in catch block
        Location? location = null;
        var tryHasBaseCall = false;
        foreach (var n in ((TryStatementSyntax)childNode).Block.ChildNodes())
        {
          var baseCalls = GetBaseCalls(context, n, isMixin).ToArray();
          ReportAllWrong(context, baseCalls);
          if (baseCalls.Length > 0)
          {
            tryHasBaseCall = true;
            if (location == null)
            {
              location = baseCalls[0].Location;
            }

            break;
          }
        }

        var anyCatchHasBaseCall = false;
        foreach (var c in ((TryStatementSyntax)childNode).Catches)
        {
          var catchHasBaseCall = false;
          foreach (var n in c.ChildNodes())
          {
            var baseCalls = GetBaseCalls(context, n, isMixin).ToArray();
            ReportAllWrong(context, baseCalls);
            if (baseCalls.Length > 0)
            {
              catchHasBaseCall = true;
              if (location == null)
              {
                location = baseCalls[0].Location;
              }

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
          result = (NumberOfBaseCalls.DiagnosticFound, Diagnostic.Create(InTryOrCatch, location));
          return true;
        }

        var childNodesOfFinallyBlock = ((TryStatementSyntax)childNode).Finally?.Block.ChildNodes();
        if (LoopOverChildNodes(context, diagnosticDescriptor, isMixin, listOfResults, childNodesOfFinallyBlock, numberOfBaseCalls, diagnostic, out result))
        {
          return true;
        }

        numberOfBaseCalls = result.numberOfBaseCalls;
      }
      else if (IsLoop(childNode))
      {
        if (ContainsBaseCall(context, childNode, isMixin, out var baseCalls))
        {
          diagnostic = Diagnostic.Create(InLoop, baseCalls[0].Location);
        }
      }
      else if (IsNormalSwitch(childNode) || IsSwitchExpression(childNode))
      {
        var baseCalls = GetBaseCalls(context, childNode, isMixin).ToArray();
        if (baseCalls.Length > 0)
        {
          ReportAll(context, baseCalls, InSwitch);
        }
      }
      else if (IsUsingStatement(childNode))
      {
        var childNodesOfUsingBlock = ((UsingStatementSyntax)childNode).Statement.ChildNodes();
        if (LoopOverChildNodes(context, diagnosticDescriptor, isMixin, listOfResults, childNodesOfUsingBlock, numberOfBaseCalls, diagnostic, out result))
        {
          return true;
        }

        numberOfBaseCalls = result.numberOfBaseCalls;
      }

      else if (ContainsBaseCall(context, childNode, isMixin, out var baseCalls))
      {
        ReportAllWrong(context, baseCalls);
        if (baseCalls.Any(bc => bc.CallsBaseMethod))
        {
          numberOfBaseCalls.Increment();
          if (numberOfBaseCalls.Max >= 2)
          {
            diagnostic = Diagnostic.Create(MultipleBaseCalls, baseCalls[0].Location);
            break;
          }
        }
      }

      if (ContainsAnonymousMethod(childNode, out var anonymousMethod))
      {
        AnalyzeAnonymousMethod(context, anonymousMethod);
      }
    }

    if (diagnostic != null) //found a diagnostic
    {
      result = (NumberOfBaseCalls.DiagnosticFound, diagnostic);
      return true;
    }

    listOfResults.Add(numberOfBaseCalls);
    result = (numberOfBaseCalls, null);
    return false;
  }

  private static bool ContainsBaseCall (SyntaxNodeAnalysisContext context, SyntaxNode node, bool isMixin, out BaseCallDescriptor[] baseCalls)
  {
    baseCalls = GetBaseCalls(context, node, isMixin).ToArray();
    return baseCalls.Length > 0;
  }

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

  private static void AnalyzeAnonymousMethod (SyntaxNodeAnalysisContext context, SyntaxNode? node)
  {
    SyntaxNode body;

    switch (node)
    {
      case SimpleLambdaExpressionSyntax simpleLambda:
        body = simpleLambda.Body;
        break;
      case AnonymousMethodExpressionSyntax anonymousMethod:
        body = anonymousMethod.Body;
        break;
      case ParenthesizedLambdaExpressionSyntax parenthesizedLambdaExpressionSyntax:
        body = parenthesizedLambdaExpressionSyntax.Body;
        break;
      default:
        return;
    }


    if (ContainsBaseCall(context, body, false, out var baseCalls))
    {
      ReportDiagnostic(context, Diagnostic.Create(InAnonymousMethod, baseCalls[0].Location));
    }
  }

  #endregion
}