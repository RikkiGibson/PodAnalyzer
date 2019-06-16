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
using Microsoft.CodeAnalysis.Formatting;

namespace PodAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConstructorProvider)), Shared]
    public class ConstructorProvider : CodeFixProvider
    {
        private readonly string nl = Environment.NewLine;
        private const string title = "Make properties getter only and generate constructor";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(TypeCanBeImmutableAnalyzer.Rule.Id, GetterPropertyNeverAssignedAnalyzer.POD002.Id); }
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
            var idString = propertySyntax.Identifier.Text;
            var newIdString = idString.Substring(0, 1).ToUpper() + idString.Substring(1);
            var newIdToken = SyntaxFactory.Identifier(newIdString);

            var newAccessors = propertySyntax.AccessorList.Accessors.Where(a => a.IsKind(SyntaxKind.GetAccessorDeclaration));
            var newAccessorList = propertySyntax.AccessorList.WithAccessors(SyntaxFactory.List(newAccessors));
            var newProperty = propertySyntax
                .WithIdentifier(newIdToken)
                .WithAccessorList(newAccessorList);

            return newProperty;
        }

        private SyntaxList<MemberDeclarationSyntax> RewriteMembers(ClassDeclarationSyntax typeDecl)
        {
            var autoProperties = typeDecl.Members
                .OfType<PropertyDeclarationSyntax>()
                .Where(ps => ps.AccessorList.Accessors.Any(a => a.Body == null))
                .ToImmutableArray();

            var getterProperties = autoProperties
                .Select(ps => GetterOnlyProperty(ps))
                .ToImmutableArray();

            var ctor = GenerateConstructor(typeDecl, getterProperties);

            var otherMembers = typeDecl.Members
                .Where(ps => !autoProperties.Contains(ps));

            var newMembers = SyntaxFactory.List(getterProperties.Concat(otherMembers).Concat(new[] { ctor }));

            return newMembers;
        }

        private ParameterSyntax GenerateParameter(PropertyDeclarationSyntax property)
        {
            var idString = property.Identifier.Text;
            var newIdString = idString.Substring(0, 1).ToLower() + idString.Substring(1);
            var newIdToken = SyntaxFactory.Identifier(newIdString);

            var param = SyntaxFactory
                .Parameter(
                    attributeLists: SyntaxFactory.List<AttributeListSyntax>(),
                    modifiers: SyntaxFactory.TokenList(),
                    type: property.Type,
                    identifier: newIdToken,
                    @default: null)
                .NormalizeWhitespace(elasticTrivia: true);

            return param;
        }

        private ExpressionStatementSyntax GenerateAssignmentStatement(
            PropertyDeclarationSyntax property,
            ParameterSyntax parm)
        {
            var exprStatement = (ExpressionStatementSyntax)SyntaxFactory
                .ParseStatement($"{property.Identifier} = {parm.Identifier};{nl}")
                .NormalizeWhitespace(elasticTrivia: true)
                .WithTrailingTrivia(SyntaxFactory.ParseTrailingTrivia(nl));

            return exprStatement;
        }

        private ConstructorDeclarationSyntax GenerateConstructor(
            ClassDeclarationSyntax typeDecl,
            ImmutableArray<PropertyDeclarationSyntax> properties)
        {
            var leadingTrivia = typeDecl.GetLeadingTrivia();
            var parms = properties.Select(p => GenerateParameter(p).WithLeadingTrivia(leadingTrivia)).ToImmutableArray();
            var separator = SyntaxFactory.ParseToken("," + nl);
            var separators = Enumerable.Repeat(separator, parms.Length - 1);
            var separatedList = SyntaxFactory.SeparatedList(parms, separators);
            
            var parmsList = SyntaxFactory.ParameterList(SyntaxFactory.ParseToken("(" + nl), separatedList, SyntaxFactory.Token(SyntaxKind.CloseParenToken));

            var statements = new ExpressionStatementSyntax[parms.Length];
            for (var i = 0; i < parms.Length; i++)
            {
                statements[i] = GenerateAssignmentStatement(properties[i], parms[i]);
            }

            var ctor = SyntaxFactory
                .ConstructorDeclaration(
                    attributeLists: SyntaxFactory.List<AttributeListSyntax>(),
                    modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
                    identifier: typeDecl.Identifier.WithTrailingTrivia(),
                    parameterList: parmsList,
                    initializer: null,
                    body: SyntaxFactory.Block(statements));

            return ctor;
        }

        private async Task<Solution> MakeImmutableAsync(Document document, ClassDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            var newTypeDecl = typeDecl
                .WithMembers(RewriteMembers(typeDecl))
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)));

            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(typeDecl, newTypeDecl);

            var newDoc = document.WithSyntaxRoot(newRoot);

            return newDoc.Project.Solution;
        }
    }
}
