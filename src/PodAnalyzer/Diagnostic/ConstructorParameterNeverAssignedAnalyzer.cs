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
    public class ConstructorParameterNeverAssignedAnalyzer : DiagnosticAnalyzer
    {
        public static DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(id: "POD004",
                title: new LocalizableResourceString(nameof(Resources.POD004Title), Resources.ResourceManager, typeof(Resources)),
                messageFormat: new LocalizableResourceString(nameof(Resources.POD004MessageFormat), Resources.ResourceManager, typeof(Resources)),
                category: "Usage",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: new LocalizableResourceString(nameof(Resources.POD004Description), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        }

        private static void AnalyzeNamedType(SymbolAnalysisContext context)
        {
            var symbol = (INamedTypeSymbol)context.Symbol;
            foreach (var ctorSymbol in symbol.InstanceConstructors)
            {
                var syntaxRefs = ctorSymbol.DeclaringSyntaxReferences;
                if (syntaxRefs.Length == 0)
                {
                    continue;
                }

                var ctorSyntax = (ConstructorDeclarationSyntax) syntaxRefs[0].GetSyntax();
                foreach (var parm in ctorSymbol.Parameters)
                {
                    if (!IsConstructorReferencingParam(context, parm, ctorSyntax))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Rule, parm.Locations[0], parm.Name));
                    }
                }
            }
        }

        private static bool IsConstructorReferencingParam(
            SymbolAnalysisContext context,
            IParameterSymbol param,
            ConstructorDeclarationSyntax ctorSyntax)
        {
            var isReferencingParam = ctorSyntax.Body
                .DescendantNodes()
                .Concat(ctorSyntax.Initializer?.ArgumentList.DescendantNodes() ?? Enumerable.Empty<SyntaxNode>())
                .OfType<IdentifierNameSyntax>()
                .Any(idSyntax => IsIdentifierReferencingParam(context, idSyntax, param));

            return isReferencingParam;
        }

        private static bool IsIdentifierReferencingParam(
            SymbolAnalysisContext context,
            IdentifierNameSyntax identifier,
            IParameterSymbol param)
        {
            if (identifier.Identifier.Text != param.Name)
            {
                return false;
            }

            var semanticModel = context.Compilation.GetSemanticModel(identifier.SyntaxTree);
            var idSymbol = semanticModel.GetSymbolInfo(identifier).Symbol;
            return param.Equals(idSymbol);
        }
    }
}
