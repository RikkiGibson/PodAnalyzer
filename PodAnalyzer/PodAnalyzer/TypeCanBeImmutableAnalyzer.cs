using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Threading.Tasks;

namespace PodAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TypeCanBeImmutableAnalyzer : DiagnosticAnalyzer
    {
        internal static DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(id: "POD003",
                title: new LocalizableResourceString(nameof(Resources.POD003Title), Resources.ResourceManager, typeof(Resources)),
                messageFormat: new LocalizableResourceString(nameof(Resources.POD003MessageFormat), Resources.ResourceManager, typeof(Resources)),
                category: "Design",
                defaultSeverity: DiagnosticSeverity.Hidden,
                isEnabledByDefault: true,
                description: new LocalizableResourceString(nameof(Resources.POD003Description), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNamedTypeDeclaration, SyntaxKind.ClassDeclaration);
        }

        private void AnalyzeNamedTypeDeclaration(SyntaxNodeAnalysisContext context)
        {
            if (context.Node.IsKind(SyntaxKind.ClassDeclaration))
            {
                AnalyzeClassDeclaration(context, (ClassDeclarationSyntax)context.Node);
            }
        }

        private void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context, ClassDeclarationSyntax node)
        {
            var properties = node.Members.OfType<PropertyDeclarationSyntax>();
            if (properties.Any())
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, node.GetLocation(), node.Identifier));
            }
        }
    }
}
