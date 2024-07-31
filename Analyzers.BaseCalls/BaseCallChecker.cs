// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;


namespace Remotion.Infrastructure.Analyzers.BaseCalls;

public static partial class BaseCallChecker
{
  private static bool IsLoop (SyntaxNode node) => node is ForStatementSyntax or WhileStatementSyntax or ForEachStatementSyntax or DoStatementSyntax;
  private static bool IsForEachLoop (SyntaxNode node) => node is ForEachStatementSyntax;
  private static bool IsIf (SyntaxNode node) => node is IfStatementSyntax;
  private static bool IsElse (SyntaxNode node) => node is ElseClauseSyntax;
  private static bool IsReturn (SyntaxNode node) => node is ReturnStatementSyntax;
  private static bool IsTry (SyntaxNode node) => node is TryStatementSyntax;
  private static bool IsSwitch (SyntaxNode node) => node is SwitchStatementSyntax or SwitchExpressionSyntax;
  private static bool IsUsingStatement (SyntaxNode node) => node is UsingStatementSyntax;
  private static bool IsBlock (SyntaxNode node) => node is BlockSyntax;
  private static bool IsThrow (SyntaxNode node) => node is ThrowStatementSyntax;

  public static Diagnostic? BaseCallCheckerInitializer (SyntaxNodeAnalysisContext context, bool isMixin)
  {
    return Check(context, context.Node, new NumberOfBaseCalls(0), isMixin).diagnostic;
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
  /// Return or throw statement:
  ///   Checks if the return statement includes a BaseCall, then returns -1, diagnostic of numberOfBaseCalls before it was -1.
  /// Try-Catch-Block:
  ///   Checks if the try or any catch block contains a basecall (-> return TryCatchDiagnostic), then calls a recursion with the finally block (same as in If).
  /// Loop:
  ///   Checks if the loop contains a basecall (-> return LoopDiagnostic)
  /// Switch:
  ///   Checks if the switch contains a basecall (-> return SwitchDiagnostic).
  /// BaseCall:
  ///   increment numberOfBaseCalls by 1.
  /// using or Block statement:
  ///   search recursively.
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
  private static (NumberOfBaseCalls numberOfBaseCalls, Diagnostic? diagnostic) Check (
      SyntaxNodeAnalysisContext context,
      SyntaxNode node,
      NumberOfBaseCalls numberOfBaseCalls,
      bool isMixin)
  {
    var methodNameLocation = Location.Create(
        context.Node.SyntaxTree,
        ((MethodDeclarationSyntax)context.Node).Identifier.Span
    );
    DiagnosticDescriptor? diagnosticDescriptor = null;
    var listOfResults = new List<NumberOfBaseCalls>();
    var ifStatementSyntax = node as IfStatementSyntax;
    var hasUnconditionalElse = ifStatementSyntax == null;
    ElseClauseSyntax? elseClauseSyntax = null;


    do //loop over every if / elseIf / else clause
    {
      var statement = ifStatementSyntax?.Statement ?? elseClauseSyntax?.Statement;
      var childNodes = GetChildNodes(statement);


      if (LoopOverChildNodes(context, diagnosticDescriptor, isMixin, childNodes, numberOfBaseCalls, null, out var baseCallCheckerRecursive))
      {
        return baseCallCheckerRecursive;
      }

      listOfResults.Add(baseCallCheckerRecursive.numberOfBaseCalls);


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


    //when the if does not have an unconditional else, it is not guaranteed that it will go through a branch
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


    IEnumerable<SyntaxNode> GetChildNodes (StatementSyntax? statement)
    {
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

      return childNodes;
    }
  }

  private static DiagnosticDescriptor? GetDiagnosticDescription (NumberOfBaseCalls numberOfBaseCallsLocal)
  {
    if (numberOfBaseCallsLocal.Max >= 2)
    {
      return Rules.MultipleBaseCalls;
    }

    return numberOfBaseCallsLocal.Min == 0 ? Rules.NoBaseCall : null;
  }

  private static bool LoopOverChildNodes ( //returns true if a diagnostic was found
      SyntaxNodeAnalysisContext context,
      DiagnosticDescriptor? diagnosticDescriptor,
      bool isMixin,
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
      if (IsIf(childNode) || IsElse(childNode))
      {
        //nested if -> recursion
        var (numberOfBaseCallsResult, resultDiagnostic) = Check(context, childNode, numberOfBaseCalls, isMixin);

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
      else if (IsThrow(childNode))
      {
        numberOfBaseCalls = NumberOfBaseCalls.Returns;
        break;
      }
      else if (IsReturn(childNode))
      {
        var baseCalls = GetBaseCalls(context, childNode, isMixin).ToArray();
        BaseCallReporter.ReportAllWrong(context, baseCalls);
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
      else if (IsBlock(childNode))
      {
        var childNodesOfTryBlock = ((BlockSyntax)childNode).ChildNodes();
        if (LoopOverChildNodes(context, diagnosticDescriptor, isMixin, childNodesOfTryBlock, numberOfBaseCalls, diagnostic, out result))
        {
          return true;
        }

        numberOfBaseCalls = result.numberOfBaseCalls;
      }
      else if (IsTry(childNode))
      {
        //recursively check try
        Location? location = null;
        var childNodesOfTryBlock = ((TryStatementSyntax)childNode).Block.ChildNodes();
        if (LoopOverChildNodes(context, diagnosticDescriptor, isMixin, childNodesOfTryBlock, numberOfBaseCalls, diagnostic, out result))
        {
          return true;
        }

        numberOfBaseCalls = result.numberOfBaseCalls;

        //check for base calls in catch
        var anyCatchHasBaseCall = false;
        foreach (var catchClauseSyntax in ((TryStatementSyntax)childNode).Catches)
        {
          var baseCalls = GetBaseCalls(context, catchClauseSyntax, isMixin).ToArray();
          if (baseCalls.Length > 0)
          {
            if (location == null)
            {
              anyCatchHasBaseCall = true;
              location = baseCalls[0].Location;
            }

            break;
          }
        }

        if (anyCatchHasBaseCall)
        {
          result = (NumberOfBaseCalls.DiagnosticFound, Diagnostic.Create(Rules.InTryOrCatch, location));
          return true;
        }

        //recursively check finally block
        var childNodesOfFinallyBlock = ((TryStatementSyntax)childNode).Finally?.Block.ChildNodes();
        if (LoopOverChildNodes(context, diagnosticDescriptor, isMixin, childNodesOfFinallyBlock, numberOfBaseCalls, diagnostic, out result))
        {
          return true;
        }

        numberOfBaseCalls = result.numberOfBaseCalls;
      }
      else if (IsLoop(childNode))
      {
        //in a for each loop, it is allowed to have a baseCall as an expression in the header
        var newChildNode = childNode;
        if (IsForEachLoop(childNode))
        {
          var baseCallsHere = GetBaseCalls(context, ((ForEachStatementSyntax)childNode).Expression, isMixin).ToArray();
          BaseCallReporter.ReportAllWrong(context, baseCallsHere);
          if (baseCallsHere.Any(bc => bc.CallsBaseMethod))
          {
            numberOfBaseCalls.Increment();
          }

          newChildNode = (childNode as ForEachStatementSyntax)!.Statement;
        }

        if (ContainsBaseCall(context, newChildNode, isMixin, out var baseCalls))
        {
          diagnostic = Diagnostic.Create(Rules.InLoop, baseCalls[0].Location);
          numberOfBaseCalls = NumberOfBaseCalls.DiagnosticFound;
          break;
        }
      }
      else if (IsUsingStatement(childNode))
      {
        var childNodesOfUsingBlock = ((UsingStatementSyntax)childNode).Statement.ChildNodes();
        if (LoopOverChildNodes(context, diagnosticDescriptor, isMixin, childNodesOfUsingBlock, numberOfBaseCalls, diagnostic, out result))
        {
          return true;
        }

        numberOfBaseCalls = result.numberOfBaseCalls;
      }
      else if (ContainsSwitch(childNode, out var switchNode))
      {
        if (ContainsBaseCall(context, switchNode, isMixin, out var baseCalls))
        {
          BaseCallReporter.ReportAll(context, baseCalls, Rules.InSwitch);
          numberOfBaseCalls = NumberOfBaseCalls.DiagnosticFound;
          break;
        }
      }
      else if (ContainsAnonymousMethod(childNode, out var anonymousMethod))
      {
        if (ContainsBaseCall(context, anonymousMethod, false, out var baseCalls))
        {
          BaseCallReporter.ReportAll(context, baseCalls, Rules.InAnonymousMethod);
        }
      }
      else if (ContainsBaseCall(context, childNode, isMixin, out var baseCalls))
      {
        BaseCallReporter.ReportAllWrong(context, baseCalls);
        if (baseCalls.Any(bc => bc.CallsBaseMethod))
        {
          numberOfBaseCalls.Increment();
          if (numberOfBaseCalls.Max >= 2)
          {
            diagnostic = Diagnostic.Create(Rules.MultipleBaseCalls, baseCalls[0].Location);
            break;
          }
        }
      }
    }

    if (diagnostic != null) //found a diagnostic
    {
      result = (NumberOfBaseCalls.DiagnosticFound, diagnostic);
      return true;
    }

    result = (numberOfBaseCalls, null);
    return false;
  }
}