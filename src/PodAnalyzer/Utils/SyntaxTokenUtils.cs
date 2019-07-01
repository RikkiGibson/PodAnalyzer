using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace PodAnalyzer.Utils
{
    internal static class SyntaxTokenUtils
    {
        internal static SyntaxToken ParsePossiblyReservedName(string name)
        {
            SyntaxToken newIdToken = ((IdentifierNameSyntax)SyntaxFactory.ParseName(name)).Identifier;
            if (newIdToken.ContainsDiagnostics)
            {
                // Assume it's because the lowercased param name is reserved
                newIdToken = SyntaxFactory.VerbatimIdentifier(SyntaxTriviaList.Empty, name, name, SyntaxTriviaList.Empty);
            }

            return newIdToken;
        }

        internal static SyntaxToken CreateParameterName(string propertyName)
        {
            var parameterName = char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);
            return ParsePossiblyReservedName(parameterName);
        }
    }
}
