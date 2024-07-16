// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Remotion.Infrastructure.Analyzers.BaseCalls;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(BaseCallCodeFixProvider))]
[Shared]
public class BaseCallCodeFixProvider : CodeFixProvider
{
  public override ImmutableArray<string> FixableDiagnosticIds =>
      ["RMBCA0001", "RMBCA0005", "RMBCA0006", "RMBCA0007", "RMBCA0008"];

  public sealed override FixAllProvider GetFixAllProvider ()
  {
    return WellKnownFixAllProviders.BatchFixer;
  }

  public sealed override async Task RegisterCodeFixesAsync (CodeFixContext context)
  {
    var diagnostic = context.Diagnostics.First();
    var diagnosticSpan = diagnostic.Location.SourceSpan;

    var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
    var methodDeclarationNode = root!.FindNode(diagnosticSpan).FirstAncestorOrSelf<MethodDeclarationSyntax>();

    context.RegisterCodeFix(
        CodeAction.Create(
            title: "Ignore with attribute",
            createChangedDocument: c => AddIgnoreAttributeAsync(context.Document, methodDeclarationNode ?? throw new Exception("expected MethodDeclarationSyntax"), c),
            equivalenceKey: "Ignore with attribute"),
        diagnostic);
  }

  private async Task<Document> AddIgnoreAttributeAsync (Document document, MethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
  {
    //create new attribute
    var attribute = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("IgnoreBaseCallCheck"));
    var attributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute));

    //add attribute to method
    var newMethodDeclaration = methodDeclaration.AddAttributeLists(attributeList);

    //update syntax root
    var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
    
    if (root == null)
      return document;

    var newRoot = root.ReplaceNode(methodDeclaration, newMethodDeclaration);

    return document.WithSyntaxRoot(newRoot);
  }
}