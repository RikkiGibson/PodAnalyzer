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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PodAnalyzerCodeFixProvider)), Shared]
    public class PodAnalyzerCodeFixProvider : CodeFixProvider
    {
        private const string title = "Make properties getter only and generate constructor";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray<string>.Empty; }
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
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedSolution: c => MakeImmutableAsync(context.Document, declaration, c),
                    equivalenceKey: title),
                diagnostic);
        }

        private static PropertyDeclarationSyntax GetterOnlyProperty(PropertyDeclarationSyntax propertySyntax)
        {
            var newAccessors = propertySyntax.AccessorList.Accessors.Where(a => a.IsKind(SyntaxKind.GetAccessorDeclaration));
            var newAccessorList = propertySyntax.AccessorList.WithAccessors(SyntaxFactory.List(newAccessors));
            var newProperty = propertySyntax.WithAccessorList(newAccessorList);

            return newProperty;
        }

        private async Task<Solution> MakeImmutableAsync(Document document, ClassDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            var autoProperties = typeDecl.Members
                .OfType<PropertyDeclarationSyntax>()
                .Where(ps => ps.AccessorList.Accessors.Any(a => a.Body == null))
                .ToImmutableArray();

            var getterProperties = autoProperties
                .Select(ps => GetterOnlyProperty(ps));

            var otherMembers = typeDecl.Members
                .Where(ps => !autoProperties.Contains(ps));

            var newMembers = SyntaxFactory.List(getterProperties.Concat(otherMembers));
            var newTypeDecl = typeDecl.WithMembers(newMembers);

            var root = await document.GetSyntaxRootAsync();
            var newRoot = root.ReplaceNode(typeDecl, newTypeDecl);

            var newDoc = document.WithSyntaxRoot(newRoot);

            return newDoc.Project.Solution;
        }
    }
}
