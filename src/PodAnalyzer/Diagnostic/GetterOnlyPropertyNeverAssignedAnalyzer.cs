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
        internal static DiagnosticDescriptor Rule =
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
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
        }

        private void AnalyzeProperty(SymbolAnalysisContext context)
        {
            try
            {
                var property = (IPropertySymbol)context.Symbol;
                if (property.SetMethod != null)
                {
                    return;
                }

                if (!IsAutoGetterPropertyAssigned(context, property))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, property.Locations[0], property.Name));
                }
            }
            catch (NullReferenceException e)
            {
                throw new Exception(e.StackTrace.Replace("\r\n", " - "));
            }
            catch (AggregateException e)
            {
                throw new InvalidOperationException(e.StackTrace.Replace("\r\n", " - "), e.InnerException);
            }
        }

        private static bool IsAutoGetterPropertyAssigned(SymbolAnalysisContext context, IPropertySymbol property)
        {
            foreach (var syntaxRef in property.DeclaringSyntaxReferences)
            {
                var propertySyntax = (PropertyDeclarationSyntax)syntaxRef.GetSyntax();
                if (propertySyntax.Initializer != null)
                {
                    return true;
                }
                
                // A null accessor list indicates an expression-bodied property
                // If accessor body is non-null, this is a computed property, not an auto property
                if (propertySyntax.AccessorList == null ||
                    propertySyntax.AccessorList.Accessors.Any(a => a.Body != null))
                {
                    return true;
                }
            }
            
            foreach (var ctorSymbol in property.ContainingType.InstanceConstructors)
            {
                foreach (var syntaxRef in ctorSymbol.DeclaringSyntaxReferences)
                {
                    var ctorSyntax = (ConstructorDeclarationSyntax)syntaxRef.GetSyntax();
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
