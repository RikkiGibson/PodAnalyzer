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
using PodAnalyzer.Utils;

namespace PodAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConstructorCallProvider)), Shared]
    public class ConstructorCallProvider : CodeFixProvider
    {
        private const string title = "Convert object initializer expression to constructor call";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create("CS7036", "CS0200"); }
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
            var hasAssignToGetterOnly = assignments.All(a => IsAssignToGetterOnlyProperty(semanticModel, a));
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

            // todo: do we need to validate that a constructor exists with a corresponding parameter for this property?
            var hasConstructor = symbol.ContainingType.InstanceConstructors.Any();
            return hasConstructor;
        }

        private static ArgumentSyntax GenerateArgument(AssignmentExpressionSyntax assignment)
        {
            var identifier = ((IdentifierNameSyntax)assignment.Left).Identifier;
            var idString = identifier.Text;
            var newIdToken = SyntaxTokenUtils.CreateParameterName(propertyName: idString);

            var rightTrivia = assignment.Right.GetTrailingTrivia();

            // Handle case `new Obj { Prop = expr };` -> new Obj(prop: expr);`
            var assignmentRight = rightTrivia.ToFullString() == " " ? assignment.Right.WithoutTrailingTrivia() : assignment.Right;

            var arg = SyntaxFactory.Argument(
                nameColon: SyntaxFactory.NameColon(SyntaxFactory.IdentifierName(newIdToken))
                    .WithLeadingTrivia(identifier.LeadingTrivia)
                    .WithTrailingTrivia(assignment.OperatorToken.TrailingTrivia),
                refKindKeyword: SyntaxFactory.Token(SyntaxKind.None),
                expression: assignmentRight);

            return arg;
        }

        private async Task<Solution> GenerateConstructorCallAsync(
            Document document,
            ObjectCreationExpressionSyntax creationExpression,
            CancellationToken cancellationToken)
        {
            var initializer = creationExpression.Initializer;
            var args = initializer.Expressions
                .OfType<AssignmentExpressionSyntax>()
                .Select(e => GenerateArgument(e))
                .ToImmutableArray();

            var argList = SyntaxFactory.ArgumentList(
                SyntaxFactory.Token(SyntaxKind.OpenParenToken).WithTrailingTrivia(initializer.OpenBraceToken.TrailingTrivia),
                SyntaxFactory.SeparatedList(args, initializer.Expressions.GetSeparators()),
                SyntaxFactory.Token(SyntaxKind.CloseParenToken).WithTriviaFrom(initializer.CloseBraceToken));

            var newCreation = SyntaxFactory
                .ObjectCreationExpression(creationExpression.NewKeyword, creationExpression.Type.WithoutTrailingTrivia(), argList, initializer: null)
                .WithTriviaFrom(creationExpression);

            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(creationExpression, newCreation);

            var newDoc = document.WithSyntaxRoot(newRoot);

            return newDoc.Project.Solution;
        }
    }
}
