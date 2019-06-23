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
        public static DiagnosticDescriptor POD003 =
            new DiagnosticDescriptor(id: "POD003",
                title: new LocalizableResourceString(nameof(Resources.POD003Title), Resources.ResourceManager, typeof(Resources)),
                messageFormat: new LocalizableResourceString(nameof(Resources.POD003MessageFormat), Resources.ResourceManager, typeof(Resources)),
                category: "Design",
                defaultSeverity: DiagnosticSeverity.Hidden,
                isEnabledByDefault: true,
                description: new LocalizableResourceString(nameof(Resources.POD003Description), Resources.ResourceManager, typeof(Resources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(POD003); } }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeClassOrStructDeclaration, SyntaxKind.ClassDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeClassOrStructDeclaration, SyntaxKind.StructDeclaration);
        }

        private static void AnalyzeClassOrStructDeclaration(SyntaxNodeAnalysisContext context)
        {
            var node = (TypeDeclarationSyntax)context.Node;

            var hasMutableAutoProperty = node.Members
                .OfType<PropertyDeclarationSyntax>()
                .Any(p => IsMutableAutoProperty(p));

            // TODO: consider relaxing this constraint in the future
            var isPartial = node.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));

            if (hasMutableAutoProperty && !isPartial)
            {
                context.ReportDiagnostic(Diagnostic.Create(POD003, node.GetLocation(), node.Identifier));
                return;
            }
        }

        private static bool IsMutableAutoProperty(PropertyDeclarationSyntax property)
        {
            var getter = property.AccessorList?.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration));
            if (getter == null || getter.Body != null)
            {
                return false;
            }

            var setter = property.AccessorList?.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.SetAccessorDeclaration));
            if (setter == null)
            {
                return false;
            }

            return true;
        }
    }
}
