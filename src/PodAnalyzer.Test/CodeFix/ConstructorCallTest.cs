using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Microsoft.CodeAnalysis.CSharp.Testing.XUnit;
using static PodAnalyzer.Test.TestUtilities;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace PodAnalyzer.Test
{
#pragma warning disable RS1001
    public class NoOpAnalyzer : DiagnosticAnalyzer
#pragma warning restore RS1001
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray<DiagnosticDescriptor>.Empty;

        public override void Initialize(AnalysisContext context)
        {
        }
    }

    public class ConstructorCallTest : CodeFixVerifier<NoOpAnalyzer, ConstructorCallProvider>
    {
        [Fact]
        public Task MultipleProperties()
        {
            var classDecl = @"
public class Pod
{
    public string P1 { get; }
    public string P2 { get; }

    public Pod(string p1, string p2)
    {
        P1 = p1;
        P2 = p2;
    }
}
";

            var beforeSource = classDecl + @"
static class C
{
    static void Test()
    {
        new Pod
        {
            P1 = ""hello"",
            P2 = ""world""
        };
    }
}";

            var expectedDiagnostics = new[]
            {
                new DiagnosticResult("CS7036", DiagnosticSeverity.Error).WithLocation(18, 13).WithArguments("p1", "Pod.Pod(string, string)"),
                new DiagnosticResult("CS0200", DiagnosticSeverity.Error).WithLocation(20, 13).WithArguments("Pod.P1"),
                new DiagnosticResult("CS0200", DiagnosticSeverity.Error).WithLocation(21, 13).WithArguments("Pod.P2"),
            };

            var afterSource = classDecl + @"
static class C
{
    static void Test()
    {
        new Pod(
            p1: ""hello"",
            p2: ""world""
        );
    }
}
";
            return VerifyCodeFixAsync(beforeSource, expectedDiagnostics, afterSource);
        }
    }
}
