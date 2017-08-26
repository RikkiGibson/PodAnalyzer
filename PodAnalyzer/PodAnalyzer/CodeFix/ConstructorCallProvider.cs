using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace PodAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConstructorCallProvider)), Shared]
    public class ConstructorCallProvider : CodeFixProvider
    {
        private readonly string nl = Environment.NewLine;
        private const string title = "Convert object initializer expression to constructor call";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create("CS7036"); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var objectCreation = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ObjectCreationExpressionSyntax>().First();

            if (objectCreation.Initializer == null)
            {
                return;
            }

            var semanticModel = await context.Document.GetSemanticModelAsync();
            var assignments = objectCreation.Initializer.Expressions.OfType<AssignmentExpressionSyntax>();
            var hasAssignToGetterOnly = assignments.Any(a => IsAssignToGetterOnlyProperty(semanticModel, a));
            if (!hasAssignToGetterOnly)
            {
                return;
            }
            
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedSolution: c => GenerateConstructorCallAsync(context.Document, objectCreation, c),
                    equivalenceKey: title),
                diagnostic);
        }

        private static bool IsAssignToGetterOnlyProperty(SemanticModel semanticModel, AssignmentExpressionSyntax assignment)
        {
            var info = semanticModel.GetSymbolInfo(assignment.Left);
            var symbol = (info.Symbol ?? info.CandidateSymbols.FirstOrDefault()) as IPropertySymbol;
            if (symbol == null || !symbol.IsReadOnly)
            {
                return false;
            }

            // TODO: how can you really tell if it's a getter-only auto prop? Auto props are syntax sugar.
            var hasConstructor = symbol.ContainingType.InstanceConstructors.Any();
            return hasConstructor;
        }

        private static ArgumentSyntax GenerateArgument(AssignmentExpressionSyntax assignment)
        {
            var idString = ((IdentifierNameSyntax)assignment.Left).Identifier.Text;
            var newIdString = idString.Substring(0, 1).ToLower() + idString.Substring(1);
            var newIdSyntax = SyntaxFactory.IdentifierName(newIdString);
            var arg = SyntaxFactory.Argument(
                nameColon: SyntaxFactory.NameColon(newIdString),
                refOrOutKeyword: SyntaxFactory.Token(SyntaxKind.None),
                expression: assignment.Right);

            return arg;
        }

        private async Task<Solution> GenerateConstructorCallAsync(
            Document document,
            ObjectCreationExpressionSyntax creationExpression,
            CancellationToken cancellationToken)
        {
            var leadingTrivia = creationExpression.Ancestors()
                .OfType<StatementSyntax>()
                .First()
                .GetLeadingTrivia()
                .AddRange(SyntaxFactory.ParseLeadingTrivia("    "));

            var args = creationExpression.Initializer.Expressions
                .OfType<AssignmentExpressionSyntax>()
                .Select(e => GenerateArgument(e).WithLeadingTrivia(leadingTrivia).WithoutTrailingTrivia())
                .ToImmutableArray();
            
            var separators = Enumerable.Repeat(SyntaxFactory.ParseToken("," + nl), args.Length - 1);

            var argList = SyntaxFactory.ArgumentList(
                SyntaxFactory.ParseToken("(" + nl),
                SyntaxFactory.SeparatedList(args, separators),
                SyntaxFactory.ParseToken(")"));

            var newCreation = SyntaxFactory
                .ObjectCreationExpression(creationExpression.Type.WithTrailingTrivia(), argList, null);

            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(creationExpression, newCreation);

            var newDoc = document.WithSyntaxRoot(newRoot);

            return newDoc.Project.Solution;
        }
    }
}
