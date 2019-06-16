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
    public class PropertyAssignToSelfAnalyzer : DiagnosticAnalyzer
    {
        public static DiagnosticDescriptor POD001 =
            new DiagnosticDescriptor(id: "POD001",
                title: new LocalizableResourceString(nameof(Resources.POD001Title), Resources.ResourceManager, typeof(Resources)),
                messageFormat: new LocalizableResourceString(nameof(Resources.POD001MessageFormat), Resources.ResourceManager, typeof(Resources)),
                category: "Usage",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: new LocalizableResourceString(nameof(Resources.POD001Description), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(POD001); } }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeConstructor, ImmutableArray.Create(SyntaxKind.ConstructorDeclaration));
        }

        private static void AnalyzeConstructor(SyntaxNodeAnalysisContext context)
        {
            var ctorSyntax = (ConstructorDeclarationSyntax)context.Node;
            if (ctorSyntax.Body == null && ctorSyntax.ExpressionBody == null)
            {
                // nothing to analyze
                return;
            }

            var assignments = ((SyntaxNode)ctorSyntax.Body ?? ctorSyntax.ExpressionBody)
                .DescendantNodes()
                .OfType<AssignmentExpressionSyntax>()
                .Where(n => IsPropertyAssignToSelf(context, n));

            foreach (var node in assignments)
            {
                var propertyName = node.Left.ToString();
                var diagnostic = Diagnostic.Create(POD001, node.GetLocation(), propertyName);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool IsPropertyAssignToSelf(SyntaxNodeAnalysisContext context, AssignmentExpressionSyntax assignment)
        {
            if (!assignment.Left.IsKind(SyntaxKind.IdentifierName) || !assignment.Right.IsKind(SyntaxKind.IdentifierName))
            {
                return false;
            }

            var lhs = (IdentifierNameSyntax)assignment.Left;
            var rhs = (IdentifierNameSyntax)assignment.Right;

            if (lhs.Identifier.Text != rhs.Identifier.Text)
            {
                return false;
            }

            var lhsSymbol = context.SemanticModel.GetSymbolInfo(lhs);
            var rhsSymbol = context.SemanticModel.GetSymbolInfo(rhs);

            if (lhsSymbol.Symbol?.Kind != SymbolKind.Property || rhsSymbol.Symbol?.Kind != SymbolKind.Property)
            {
                return false;
            }

            var isSameSymbol = object.Equals(lhsSymbol.Symbol, rhsSymbol.Symbol);
            return isSameSymbol;
        }
    }
}
