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
using System.Diagnostics;

namespace PodAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class GetterPropertyNeverAssignedAnalyzer : DiagnosticAnalyzer
    {
        public static DiagnosticDescriptor POD002 =
            new DiagnosticDescriptor(id: "POD002",
                title: new LocalizableResourceString(nameof(Resources.POD002Title), Resources.ResourceManager, typeof(Resources)),
                messageFormat: new LocalizableResourceString(nameof(Resources.POD002MessageFormat), Resources.ResourceManager, typeof(Resources)),
                category: "Usage",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: new LocalizableResourceString(nameof(Resources.POD002Description), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(POD002); } }

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
                    context.ReportDiagnostic(Diagnostic.Create(POD002, property.Locations[0], property));
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
            Debug.Assert(!property.IsImplicitlyDeclared);
            var propertySyntax = (PropertyDeclarationSyntax)property.DeclaringSyntaxReferences.First().GetSyntax();
            if (propertySyntax.Initializer != null)
            {
                return true;
            }
                
                
            if (// A null accessor list indicates an expression-bodied property
                propertySyntax.AccessorList == null ||
                // If accessor body is non-null, this is a computed property, not an auto property
                propertySyntax.AccessorList.Accessors.Any(a => a.Body != null))
            {
                return true;
            }

            // if any constructor exists which does not assign the property (including implicit constructor), return false
            foreach (var ctorSymbol in property.ContainingType.InstanceConstructors)
            {
                Debug.Assert(ctorSymbol.DeclaringSyntaxReferences.Length <= 1);
                var ctorSyntax = ctorSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as ConstructorDeclarationSyntax;
                if (ctorSyntax == null)
                {
                    // implicit constructor
                    return false;
                }

                var bodyOpt = (SyntaxNode)ctorSyntax.Body ?? ctorSyntax.ExpressionBody;
                if (bodyOpt == null)
                {
                    // don't check extern constructors
                    continue;
                }

                if (ctorSyntax.Initializer != null && ctorSyntax.Initializer.ThisOrBaseKeyword.Kind() == SyntaxKind.ThisKeyword)
                {
                    // If the constructor calls out to another constructor in the same class, rely on the other constructor to initialize
                    // (if the other constructor doesn't initialize, we'll warn anyway)
                    return true;
                }

                var isAssigned = false;
                var assignments = bodyOpt.DescendantNodes().OfType<AssignmentExpressionSyntax>();
                foreach (var assignment in assignments)
                {
                    var symbol = context.Compilation.GetSemanticModel(ctorSyntax.SyntaxTree).GetSymbolInfo(assignment.Left, context.CancellationToken);
                    if (property.Equals(symbol.Symbol))
                    {
                        isAssigned = true;
                        break;
                    }
                }

                if (!isAssigned)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
