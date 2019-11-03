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

        [Fact]
        public Task MultipleProperties_BadAssignment()
        {
            var beforeSource = multiplePropertiesClassDecl + @"
static class C
{
    static Pod p = new Pod
    {
        P1 = ""hello"",
        123 = 123
    };
}
";

            var expectedDiagnostics = new[]
            {
                new DiagnosticResult("CS7036", DiagnosticSeverity.Error).WithLocation(16, 24).WithArguments("p1", "Pod.Pod(string, string)"),
                new DiagnosticResult("CS0200", DiagnosticSeverity.Error).WithLocation(18, 9).WithArguments("Pod.P1"),
                new DiagnosticResult("CS0131", DiagnosticSeverity.Error).WithLocation(19, 9),
                new DiagnosticResult("CS0747", DiagnosticSeverity.Error).WithLocation(19, 9),
            };

            var afterSource = multiplePropertiesClassDecl + @"
static class C
{
    static Pod p = new Pod
    {
        P1 = ""hello"",
        123 = 123
    };
}
";
            return VerifyCodeFixAsync(beforeSource, expectedDiagnostics, afterSource);
        }

        [Fact]
        public Task ContainingCollectionInitializer()
        {
            var beforeSource = @"
using System.Collections.Generic;

public class Pod
{
    public List<int> Numbers { get; }

    public Pod(List<int> numbers)
    {
        Numbers = numbers;
    }
}

class C
{
    static void Test()
    {
        new Pod
        {
            Numbers = new List<int> { 1, 2, 3 }
        };
    }
}
";

            var expectedDiagnostics = new[]
            {
                new DiagnosticResult("CS7036", DiagnosticSeverity.Error).WithLocation(18, 13).WithArguments("numbers", "Pod.Pod(List<int>)"),
                new DiagnosticResult("CS0200", DiagnosticSeverity.Error).WithLocation(20, 13).WithArguments("Pod.Numbers"),
            };

            var afterSource = @"
using System.Collections.Generic;

public class Pod
{
    public List<int> Numbers { get; }

    public Pod(List<int> numbers)
    {
        Numbers = numbers;
    }
}

class C
{
    static void Test()
    {
        new Pod(
            numbers: new List<int> { 1, 2, 3 }
        );
    }
}
";
            return VerifyCodeFixAsync(beforeSource, expectedDiagnostics, afterSource);
        }

        [Fact]
        public Task ContainingNestedCollectionInitializer()
        {
            var beforeSource = @"
using System.Collections.Generic;

public class Widget
{
    public int Prop { get; set; }
}

public class Pod
{
    public List<Widget> Widgets { get; }

    public Pod(
        List<Widget> widgets)
    {
        Widgets = widgets;
    }
}

class C
{
    static void Test()
    {
        new Pod
        {
            Widgets = new List<Widget>
            {
                new Widget { Prop = 1 },
                new Widget { Prop = 2 },
                new Widget { Prop = 3 }
            }
        };
    }
}
";

            var expectedDiagnostics = new[]
            {
                new DiagnosticResult("CS7036", DiagnosticSeverity.Error).WithLocation(24, 13).WithArguments("widgets", "Pod.Pod(List<Widget>)"),
                new DiagnosticResult("CS0200", DiagnosticSeverity.Error).WithLocation(26, 13).WithArguments("Pod.Widgets"),
            };

            var afterSource = @"
using System.Collections.Generic;

public class Widget
{
    public int Prop { get; set; }
}

public class Pod
{
    public List<Widget> Widgets { get; }

    public Pod(
        List<Widget> widgets)
    {
        Widgets = widgets;
    }
}

class C
{
    static void Test()
    {
        new Pod(
            widgets: new List<Widget>
            {
                new Widget { Prop = 1 },
                new Widget { Prop = 2 },
                new Widget { Prop = 3 }
            }
        );
    }
}
";
            return VerifyCodeFixAsync(beforeSource, expectedDiagnostics, afterSource);
        }

        [Fact]
        public Task VerbatimParameter()
        {
            var beforeSource = @"
public class Pod
{
    public double Long { get; }

    public Pod(double @long)
    {
        Long = @long;
    }
}

class C
{
    static void Test()
    {
        new Pod
        {
            Long = 0.0
        };
    }
}
";

            var expectedDiagnostics = new[]
            {
                new DiagnosticResult("CS7036", DiagnosticSeverity.Error).WithLocation(16, 13).WithArguments("long", "Pod.Pod(double)"),
                new DiagnosticResult("CS0200", DiagnosticSeverity.Error).WithLocation(18, 13).WithArguments("Pod.Long"),
            };

            var afterSource = @"
public class Pod
{
    public double Long { get; }

    public Pod(double @long)
    {
        Long = @long;
    }
}

class C
{
    static void Test()
    {
        new Pod(
            @long: 0.0
        );
    }
}
";
            return VerifyCodeFixAsync(beforeSource, expectedDiagnostics, afterSource);
        }

        [Fact]
        public Task Indexer()
        {
            var beforeSource = @"
public class Pod
{
    public int this[int i] { get => i + _number; }

    private int _number;
    public Pod(int number)
    {
        _number = number;
    }
}

class C
{
    static void Test()
    {
        new Pod
        {
            [0] = 0,
            [1] = 1
        };
    }
}
";

            var expectedDiagnostics = new[]
            {
                new DiagnosticResult("CS7036", DiagnosticSeverity.Error).WithLocation(17, 13).WithArguments("number", "Pod.Pod(int)"),
                new DiagnosticResult("CS0200", DiagnosticSeverity.Error).WithLocation(19, 13).WithArguments("Pod.this[int]"),
                new DiagnosticResult("CS0200", DiagnosticSeverity.Error).WithLocation(20, 13).WithArguments("Pod.this[int]"),
            };

            var afterSource = @"
public class Pod
{
    public int this[int i] { get => i + _number; }

    private int _number;
    public Pod(int number)
    {
        _number = number;
    }
}

class C
{
    static void Test()
    {
        new Pod
        {
            [0] = 0,
            [1] = 1
        };
    }
}
";
            return VerifyCodeFixAsync(beforeSource, expectedDiagnostics, afterSource);
        }

        [Fact]
        public Task CallMissingArgument_NotObjectCreation()
        {
            var source = @"
class C
{
    static void M(int i) { }

    static void Test()
    {
        M();
    }
}
";

            var expectedDiagnostics = new[]
            {
                new DiagnosticResult("CS7036", DiagnosticSeverity.Error).WithLocation(8, 9).WithArguments("i", "C.M(int)")
            };

            return VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource: source);
        }
    }
}
