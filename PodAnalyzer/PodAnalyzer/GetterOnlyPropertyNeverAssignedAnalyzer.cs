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
    public class GetterPropertyNeverAssignedAnalyzer : DiagnosticAnalyzer
    {
        private static DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(id: "POD002",
                title: new LocalizableResourceString(nameof(Resources.POD002Title), Resources.ResourceManager, typeof(Resources)),
                messageFormat: new LocalizableResourceString(nameof(Resources.POD002MessageFormat), Resources.ResourceManager, typeof(Resources)),
                category: "Usage",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: new LocalizableResourceString(nameof(Resources.POD002Description), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
        }

        private async void AnalyzeProperty(SymbolAnalysisContext context)
        {
            var property = (IPropertySymbol)context.Symbol;
            if (property.SetMethod != null)
            {
                return;
            }

            if (!await IsGetterPropertyAssigned(context, property))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, property.Locations[0], property.Name));
            }
        }

        private static async Task<bool> IsGetterPropertyAssigned(SymbolAnalysisContext context, IPropertySymbol property)
        {
            foreach (var syntaxRef in property.DeclaringSyntaxReferences)
            {
                var propertySyntax = (PropertyDeclarationSyntax)await syntaxRef.GetSyntaxAsync();
                if (propertySyntax.Initializer != null)
                {
                    return true;
                }
            }

            var ctors = property.ContainingType.InstanceConstructors;
            foreach (var ctorSymbol in property.ContainingType.InstanceConstructors)
            {
                foreach (var syntaxRef in ctorSymbol.DeclaringSyntaxReferences)
                {
                    var ctorSyntax = (ConstructorDeclarationSyntax)await syntaxRef.GetSyntaxAsync();
                    var assignments = ctorSyntax.Body.DescendantNodes().OfType<AssignmentExpressionSyntax>();
                    foreach (var assignment in assignments)
                    {
                        var symbol = context.Compilation.GetSemanticModel(ctorSyntax.SyntaxTree).GetSymbolInfo(assignment.Left);
                        if (property.Equals(symbol.Symbol))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
