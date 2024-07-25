// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;
using Microsoft.CodeAnalysis.CSharp;

namespace Remotion.Infrastructure.Analyzers.BaseCalls;

public static partial class BaseCallChecker
{
    public static bool ContainsBaseCall (SyntaxNodeAnalysisContext context, SyntaxNode node, bool isMixin, out BaseCallDescriptor[] baseCalls)
  {
    baseCalls = GetBaseCalls(context, node, isMixin).ToArray();
    return baseCalls.Length > 0;
  }
  
  public static IEnumerable<BaseCallDescriptor> GetBaseCalls (SyntaxNodeAnalysisContext context, SyntaxNode? nodeToCheck, bool isMixin)
  {
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
}