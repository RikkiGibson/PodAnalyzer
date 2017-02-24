using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PodAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PodAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "PropertySelfAssign";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSyntaxNodeAction(AnalyzeConstructor, ImmutableArray.Create(SyntaxKind.ConstructorDeclaration));
        }

        private static void AnalyzeConstructor(SyntaxNodeAnalysisContext context)
        {
            var assignments = ((ConstructorDeclarationSyntax)context.Node)
                .Body
                .DescendantNodes()
                .OfType<AssignmentExpressionSyntax>()
                .Where(n => IsPropertyAssignToSelf(context, n));

            foreach (var node in assignments)
            {
                var propertyName = node.Left.ToString();
                var diagnostic = Diagnostic.Create(Rule, node.GetLocation(), propertyName);
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

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            // TODO: Replace the following code with your own analysis, generating Diagnostic objects for any issues you find
            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

            // Find just those named type symbols with names containing lowercase letters.
            if (namedTypeSymbol.Name.ToCharArray().Any(char.IsLower))
            {
                // For all such symbols, produce a diagnostic.
                var diagnostic = Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
