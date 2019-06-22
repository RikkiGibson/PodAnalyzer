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
        private const string multiplePropertiesClassDecl = @"
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

        [Fact]
        public Task MultipleProperties_Local()
        {
            var beforeSource = multiplePropertiesClassDecl + @"
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
}
";

            var expectedDiagnostics = new[]
            {
                new DiagnosticResult("CS7036", DiagnosticSeverity.Error).WithLocation(18, 13).WithArguments("p1", "Pod.Pod(string, string)"),
                new DiagnosticResult("CS0200", DiagnosticSeverity.Error).WithLocation(20, 13).WithArguments("Pod.P1"),
                new DiagnosticResult("CS0200", DiagnosticSeverity.Error).WithLocation(21, 13).WithArguments("Pod.P2"),
            };

            var afterSource = multiplePropertiesClassDecl + @"
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

        [Fact]
        public Task MultipleProperties_Local_NestedBlock()
        {
            var beforeSource = multiplePropertiesClassDecl + @"
static class C
{
    static void Test()
    {
        if (true)
        {
            var p = new Pod
            {
                P1 = ""hello"",
                P2 = ""world""
            };
        }
    }
}
";

            var expectedDiagnostics = new[]
            {
                new DiagnosticResult("CS7036", DiagnosticSeverity.Error).WithLocation(20, 25).WithArguments("p1", "Pod.Pod(string, string)"),
                new DiagnosticResult("CS0200", DiagnosticSeverity.Error).WithLocation(22, 17).WithArguments("Pod.P1"),
                new DiagnosticResult("CS0200", DiagnosticSeverity.Error).WithLocation(23, 17).WithArguments("Pod.P2"),
            };

            var afterSource = multiplePropertiesClassDecl + @"
static class C
{
    static void Test()
    {
        if (true)
        {
            var p = new Pod(
                p1: ""hello"",
                p2: ""world""
            );
        }
    }
}
";
            return VerifyCodeFixAsync(beforeSource, expectedDiagnostics, afterSource);
        }

        [Fact]
        public Task MultipleProperties_Local_NestedExpression()
        {
            var beforeSource = multiplePropertiesClassDecl + @"
static class C
{
    static void Test()
    {
        Pod p = new Pod(""already"", ""good"");
        if (p.Equals(new Pod { P1 = ""not"", P2 = ""yet"" }))
        {
        }
    }
}
";

            var expectedDiagnostics = new[]
            {
                new DiagnosticResult("CS7036", DiagnosticSeverity.Error).WithLocation(19, 26).WithArguments("p1", "Pod.Pod(string, string)"),
                new DiagnosticResult("CS0200", DiagnosticSeverity.Error).WithLocation(19, 32).WithArguments("Pod.P1"),
                new DiagnosticResult("CS0200", DiagnosticSeverity.Error).WithLocation(19, 44).WithArguments("Pod.P2"),
            };

            var afterSource = multiplePropertiesClassDecl + @"
static class C
{
    static void Test()
    {
        Pod p = new Pod(""already"", ""good"");
        if (p.Equals(new Pod(p1: ""not"", p2: ""yet"")))
        {
        }
    }
}
";
            return VerifyCodeFixAsync(beforeSource, expectedDiagnostics, afterSource);
        }

        [Fact]
        public Task MultipleProperties_Field()
        {
            var beforeSource = multiplePropertiesClassDecl + @"
static class C
{
    static Pod p = new Pod
    {
        P1 = ""hello"",
        P2 = ""world""
    };
}
";

            var expectedDiagnostics = new[]
            {
                new DiagnosticResult("CS7036", DiagnosticSeverity.Error).WithLocation(16, 24).WithArguments("p1", "Pod.Pod(string, string)"),
                new DiagnosticResult("CS0200", DiagnosticSeverity.Error).WithLocation(18, 9).WithArguments("Pod.P1"),
                new DiagnosticResult("CS0200", DiagnosticSeverity.Error).WithLocation(19, 9).WithArguments("Pod.P2"),
            };

            var afterSource = multiplePropertiesClassDecl + @"
static class C
{
    static Pod p = new Pod(
        p1: ""hello"",
        p2: ""world""
    );
}
";
            return VerifyCodeFixAsync(beforeSource, expectedDiagnostics, afterSource);
        }

        [Fact]
        public Task MultipleProperties_Comments()
        {
            var beforeSource = multiplePropertiesClassDecl + @"
static class C
{
    static Pod p = /* here it comes: */ new /* */ Pod // comment
    { // comment
        P1 = ""hello"" /* comment */, // TODO: think of a better value
        P2 /* :( */ = /* :) */ ""world""
    } /* comment */; // end of statement
}
";

            var expectedDiagnostics = new[]
            {
                new DiagnosticResult("CS7036", DiagnosticSeverity.Error).WithLocation(16, 51).WithArguments("p1", "Pod.Pod(string, string)"),
                new DiagnosticResult("CS0200", DiagnosticSeverity.Error).WithLocation(18, 9).WithArguments("Pod.P1"),
                new DiagnosticResult("CS0200", DiagnosticSeverity.Error).WithLocation(19, 9).WithArguments("Pod.P2"),
            };

            var afterSource = multiplePropertiesClassDecl + @"
static class C
{
    static Pod p = /* here it comes: */ new /* */ Pod( // comment
        p1: ""hello"" /* comment */, // TODO: think of a better value
        p2: /* :) */ ""world""
    ) /* comment */; // end of statement
}
";
            return VerifyCodeFixAsync(beforeSource, expectedDiagnostics, afterSource);
        }
    }
}
