using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System;
using System.Collections.Generic;
using System.Text;

namespace PodAnalyzer.Test
{
    public static class TestUtilities
    {
        public static DiagnosticResult GetCSharpResultAt(int line, int column, DiagnosticDescriptor descriptor, params object[] args)
            => new DiagnosticResult(descriptor).WithArguments(args).WithLocation(line, column);
    }
}
