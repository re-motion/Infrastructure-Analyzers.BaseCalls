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
  private static readonly LocalizableString Title = "DummyTitle";

  // The message that will be displayed to the user.
  private static readonly LocalizableString MessageFormat = "DummyMessage";

  private static readonly LocalizableString Description = "DummyDescription.";

  // Don't forget to add your rules to the AnalyzerReleases.Shipped/Unshipped.md
  public static readonly DiagnosticDescriptor Rule = new(
      DiagnosticId,
      Title,
      MessageFormat,
      Category,
      DiagnosticSeverity.Warning,
      true,
      Description);

  // Keep in mind: you have to list your rules here.
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [Rule];

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
    //will always work because it is checked above
    var node = (MethodDeclarationSyntax)context.Node;

    //check for IgnoreBaseCallCheck attribute
    var attrFound = HasIgnoreBaseCallCheckAttribute(context);
    if (attrFound) return;


    //if the method does not have the keyword override, the basecallcheck is useless
    if (!node.Modifiers.Any(SyntaxKind.OverrideKeyword)) return;
    
    //get overridden method
    var methodSymbol = context.SemanticModel.GetDeclaredSymbol(node);
    var overriddenMethod = methodSymbol?.OverriddenMethod;
    if (overriddenMethod == null) return;
    
    //check overridden method for BaseCallCheck attribute
    var res = CheckForBaseCallCheckAttribute(overriddenMethod);
    if (res == BaseCall.IsOptional)
      return;
    
    
    //store Method signature
    var methodName = node.Identifier.Text;
    var parameters = node.ParameterList.Parameters;
    var numberOfParameters = parameters.Count;
    var typesOfParameters = parameters.Select(param => param.GetType()).ToArray();

    //int arity = node.Arity; TODO: implement for Generics


    //get child nodes of the method declaration
    if (node.Body != null)
    {
      var childNodes = node.Body.ChildNodes();

      foreach (var childNode in childNodes)
      {
        /*
       searching for basecalls by going through every line and checking if it's a basecall

       example syntax tree:

       MethodDeclaration
        Body: Block
          Expression: InvocationExpression
            Expression: SimpleMemberAccessExpression
              Expression: BaseExpression

       */
        if (childNode is not ExpressionStatementSyntax expressionStatementNode)
        {
          continue;
        }

        MemberAccessExpressionSyntax simpleMemberAccessExpressionNode;
        InvocationExpressionSyntax invocationExpressionNode;
        try
        {
          invocationExpressionNode = expressionStatementNode.Expression as InvocationExpressionSyntax ?? throw new InvalidOperationException();
          simpleMemberAccessExpressionNode = invocationExpressionNode.Expression as MemberAccessExpressionSyntax ?? throw new InvalidOperationException();
        
        }
        catch (InvalidOperationException)
        {
          continue;
        }

        //check if xxx in code line "base.xxx" is the same method signature(name + list of params) as before cause if so, it is a basecall
        var arguments = invocationExpressionNode.ArgumentList.Arguments;
        int numberOfArguments = arguments.Count;
        Type[] typesOfArguments = arguments.Select(arg => arg.GetType()).ToArray();
      
        if (simpleMemberAccessExpressionNode.Expression is BaseExpressionSyntax
            && simpleMemberAccessExpressionNode.Name.Identifier.Text == methodName
            && numberOfParameters == numberOfArguments
            && typesOfParameters.Equals(typesOfArguments)
           )
        {
          return;
        }
      }
    }

    //no basecall found -> Warning
    context.ReportDiagnostic(Diagnostic.Create(Rule, node.GetLocation()));
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
    var attributes = overriddenMethod.GetAttributes();
    var attributeDescriptions = attributes.Select(attr =>
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
      if (attributeDescription.Contains("Remotion.Infrastructure.Analyzers.BaseCalls.BaseCallCheckAttribute(BaseCall.IsOptional)"))
      {
        return BaseCall.IsOptional;
      }
    }
    return BaseCall.IsMandatory;
  }
}