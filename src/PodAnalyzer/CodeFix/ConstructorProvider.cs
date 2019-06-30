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
using PodAnalyzer.Utils;

namespace PodAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConstructorProvider)), Shared]
    public class ConstructorProvider : CodeFixProvider
    {
        private readonly string nl = Environment.NewLine;
        private const string title = "Make properties getter only and generate constructor";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(TypeCanBeImmutableAnalyzer.POD003.Id, GetterPropertyNeverAssignedAnalyzer.POD002.Id); }
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
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedSolution: c => MakeImmutableAsync(context.Document, declaration, c),
                    equivalenceKey: title),
                diagnostic);
        }

        private static MemberDeclarationSyntax VisitMember(MemberDeclarationSyntax member)
        {
            if (member is PropertyDeclarationSyntax property)
            {
                var accessor = property.AccessorList?.Accessors.FirstOrDefault();
                if (accessor != null && accessor.Body == null)
                {
                    return GetterOnlyProperty(property);
                }
            }

            return member;
        }

        private static PropertyDeclarationSyntax GetterOnlyProperty(PropertyDeclarationSyntax propertySyntax)
        {
            var newAccessors = propertySyntax.AccessorList.Accessors.Where(a => a.IsKind(SyntaxKind.GetAccessorDeclaration));
            var newAccessorList = propertySyntax.AccessorList.WithAccessors(SyntaxFactory.List(newAccessors));
            var newProperty = propertySyntax
                .WithAccessorList(newAccessorList);

            return newProperty;
        }

        private SyntaxList<MemberDeclarationSyntax> RewriteMembers(TypeDeclarationSyntax typeDecl, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var members = typeDecl.Members;
            var membersSize = members.Count + 1;
            var membersBuilder = ImmutableArray.CreateBuilder<MemberDeclarationSyntax>(membersSize);

            var constructorIndex = members.LastIndexOf(m => m.IsKind(SyntaxKind.ConstructorDeclaration));

            for (int i = 0; i < typeDecl.Members.Count; i++)
            {
                membersBuilder.Add(VisitMember(members[i]));

                if (i == constructorIndex)
                {
                    var ctorOpt = GenerateConstructorIfNecessary(typeDecl, semanticModel, cancellationToken);
                    if (ctorOpt != null)
                    {
                        membersBuilder.Add(ctorOpt);
                    }
                }
            }

            // there were no constructors
            if (constructorIndex == -1)
            {
                var ctorOpt = GenerateConstructorIfNecessary(typeDecl, semanticModel, cancellationToken);
                if (ctorOpt != null)
                {
                    membersBuilder.Add(ctorOpt);
                }
            }

            var newMembers = SyntaxFactory.List(membersBuilder.ToImmutable());
            return newMembers;
        }

        private ParameterSyntax GenerateParameter(PropertyDeclarationSyntax property)
        {
            var idString = property.Identifier.Text;
            var newIdToken = SyntaxTokenUtils.CreateParameterName(propertyName: idString);

            // `int Prop { get; }` becomes `int prop`
            var param = SyntaxFactory.Parameter(
                attributeLists: SyntaxFactory.List<AttributeListSyntax>(),
                modifiers: SyntaxFactory.TokenList(),
                type: property.Type,
                identifier: newIdToken,
                @default: null);

            // TODO: can user indent settings be respected?
            var indentedParam = param.WithLeadingTrivia(
                SyntaxFactory.TriviaList(
                    new[] { property.GetLeadingTrivia().Last(t => t.IsKind(SyntaxKind.WhitespaceTrivia)) }
                        .Concat(SyntaxFactory.ParseLeadingTrivia("    "))));

            return indentedParam;
        }

        private ExpressionStatementSyntax GenerateAssignmentStatement(
            PropertyDeclarationSyntax property,
            ParameterSyntax parm)
        {
            var exprStatement = SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(property.Identifier),
                        SyntaxFactory.IdentifierName(parm.Identifier)))
                .WithLeadingTrivia(parm.GetLeadingTrivia())
                .WithTrailingTrivia(SyntaxFactory.ParseTrailingTrivia(nl));

            return exprStatement;
        }

        private ConstructorDeclarationSyntax GenerateConstructorIfNecessary(TypeDeclarationSyntax typeDecl, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var properties = typeDecl.Members
                .OfType<PropertyDeclarationSyntax>()
                .Where(p => p.AccessorList?.Accessors.Any(a => a.Body == null) == true
                    && !p.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)))
                .ToImmutableArray();

            foreach (var ctorSyntax in typeDecl.Members.OfType<ConstructorDeclarationSyntax>())
            {
                // TODO: cancellation token
                var ctorSymbol = semanticModel.GetDeclaredSymbol(ctorSyntax);
                var parameters = ctorSymbol.Parameters;
                if (ctorSymbol.Parameters.Length != properties.Length)
                {
                    continue;
                }

                var parametersMatchProperties = true;
                var length = parameters.Length;
                for (var i = 0; i < length; i++)
                {
                    var sameType = parameters[i].Type.Equals(semanticModel.GetSymbolInfo(properties[i].Type, cancellationToken).Symbol);
                    if (!sameType)
                    {
                        parametersMatchProperties = false;
                    }
                }

                if (parametersMatchProperties)
                {
                    // generating a constructor is redundant
                    return null;
                }
            }

            var parms = properties.Select(p => GenerateParameter(p)).ToImmutableArray();
            var separator = SyntaxFactory.ParseToken("," + nl);
            var separators = Enumerable.Repeat(separator, parms.Length - 1);
            var separatedList = SyntaxFactory.SeparatedList(parms, separators);
            
            var parmsList = SyntaxFactory.ParameterList(SyntaxFactory.ParseToken("(" + nl), separatedList, SyntaxFactory.Token(SyntaxKind.CloseParenToken));

            var statements = new ExpressionStatementSyntax[parms.Length];
            for (var i = 0; i < parms.Length; i++)
            {
                statements[i] = GenerateAssignmentStatement(properties[i], parms[i]);
            }

            var visiblityMods = typeDecl.Modifiers
                .Select(m => m.Kind())
                .Where(k => k == SyntaxKind.PublicKeyword || k == SyntaxKind.InternalKeyword || k == SyntaxKind.ProtectedKeyword || k == SyntaxKind.PrivateKeyword)
                .Select(SyntaxFactory.Token);

            var ctor = SyntaxFactory
                .ConstructorDeclaration(
                    attributeLists: SyntaxFactory.List<AttributeListSyntax>(),
                    modifiers: SyntaxFactory.TokenList(visiblityMods),
                    identifier: typeDecl.Identifier.WithoutTrivia(),
                    parameterList: parmsList,
                    initializer: null,
                    body: SyntaxFactory.Block(statements));

            return ctor;
        }

        private async Task<Solution> MakeImmutableAsync(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var newTypeDecl = typeDecl
                .WithMembers(RewriteMembers(typeDecl, semanticModel, cancellationToken));

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(typeDecl, newTypeDecl);

            var newDoc = document.WithSyntaxRoot(newRoot);

            return newDoc.Project.Solution;
        }
    }
}
