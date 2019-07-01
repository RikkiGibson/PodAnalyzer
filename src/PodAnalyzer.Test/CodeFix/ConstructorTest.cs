using Microsoft.CodeAnalysis.CSharp.Testing.XUnit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static PodAnalyzer.Test.TestUtilities;

namespace PodAnalyzer.Test
{
    public class ConstructorTest: CodeFixVerifier<TypeCanBeImmutableAnalyzer, ConstructorProvider>
    {
        [Fact]
        public Task SingleProperty()
        {
            var source = @"
public class C
{
    public int Prop { get; set; }
}
";

            var expectedDiagnostics = new[]
            {
                // Test0.cs(2,1): hidden POD003: Type 'C' can be made immutable
                GetCSharpResultAt(2, 1, TypeCanBeImmutableAnalyzer.POD003, "C")
            };

            var fixedSource = @"
public class C
{
    public int Prop { get; }

    public C(
        int prop)
    {
        Prop = prop;
    }
}
";
            return VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource);
        }

        [Fact]
        public Task ConstructorAlreadyExists()
        {
            var source = @"
public class C
{
    public int Prop { get; set; }

    public C(int prop)
    {
        Prop = prop;
    }
}
";

            var expectedDiagnostics = new[]
            {
                // Test0.cs(2,1): hidden POD003: Type 'C' can be made immutable
                GetCSharpResultAt(2, 1, TypeCanBeImmutableAnalyzer.POD003, "C")
            };

            var fixedSource = @"
public class C
{
    public int Prop { get; }

    public C(int prop)
    {
        Prop = prop;
    }
}
";
            return VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource);
        }

        [Fact]
        public Task StructWithSingleProperty()
        {
            var source = @"
public struct S
{
    public int Prop { get; set; }
}
";

            var expectedDiagnostics = new[]
            {
                // Test0.cs(2,1): hidden POD003: Type 'S' can be made immutable
                GetCSharpResultAt(2, 1, TypeCanBeImmutableAnalyzer.POD003, "S")
            };

            var fixedSource = @"
public struct S
{
    public int Prop { get; }

    public S(
        int prop)
    {
        Prop = prop;
    }
}
";
            return VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource);
        }

        [Fact]
        public Task PartialClass()
        {
            var source = @"
public partial class C
{
    public int Prop { get; set; }
}
";

            var fixedSource = @"
public partial class C
{
    public int Prop { get; set; }
}
";
            return VerifyCodeFixAsync(source, fixedSource);
        }

        [Fact]
        public Task MultiProperty()
        {
            var source = @"
public class C
{
    public int Prop1 { get; set; }
    public string Prop2 { get; set; }
}
";

            var expectedDiagnostics = new[]
            {
                // Test0.cs(2,1): hidden POD003: Type 'C' can be made immutable
                GetCSharpResultAt(2, 1, TypeCanBeImmutableAnalyzer.POD003, "C")
            };

            var fixedSource = @"
public class C
{
    public int Prop1 { get; }
    public string Prop2 { get; }

    public C(
        int prop1,
        string prop2)
    {
        Prop1 = prop1;
        Prop2 = prop2;
    }
}
";
            return VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource);
        }

        [Fact]
        public Task InternalClass()
        {
            var source = @"
internal class C
{
    internal int Prop1 { get; set; }
    internal string Prop2 { get; set; }
}
";

            var expectedDiagnostics = new[]
            {
                // Test0.cs(2,1): hidden POD003: Type 'C' can be made immutable
                GetCSharpResultAt(2, 1, TypeCanBeImmutableAnalyzer.POD003, "C")
            };

            var fixedSource = @"
internal class C
{
    internal int Prop1 { get; }
    internal string Prop2 { get; }

    internal C(
        int prop1,
        string prop2)
    {
        Prop1 = prop1;
        Prop2 = prop2;
    }
}
";
            return VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource);
        }

        [Fact]
        public Task NestedClass()
        {
            var source = @"
public class C
{
    private class C1
    {
        internal int Prop1 { get; set; }
        internal string Prop2 { get; set; }
    }
}
";

            var expectedDiagnostics = new[]
            {
                // Test0.cs(2,1): hidden POD003: Type 'C.C1' can be made immutable
                GetCSharpResultAt(4, 5, TypeCanBeImmutableAnalyzer.POD003, "C.C1")
            };

            var fixedSource = @"
public class C
{
    private class C1
    {
        internal int Prop1 { get; }
        internal string Prop2 { get; }

        private C1(
            int prop1,
            string prop2)
        {
            Prop1 = prop1;
            Prop2 = prop2;
        }
    }
}
";
            return VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource);
        }

        [Fact]
        public Task MultipleVisibilityMods()
        {
            var source = @"
public class C
{
    protected internal class C1
    {
        internal int Prop1 { get; set; }
        internal string Prop2 { get; set; }
    }
}
";

            var expectedDiagnostics = new[]
            {
                // Test0.cs(2,1): hidden POD003: Type 'C.C1' can be made immutable
                GetCSharpResultAt(4, 5, TypeCanBeImmutableAnalyzer.POD003, "C.C1")
            };

            var fixedSource = @"
public class C
{
    protected internal class C1
    {
        internal int Prop1 { get; }
        internal string Prop2 { get; }

        protected internal C1(
            int prop1,
            string prop2)
        {
            Prop1 = prop1;
            Prop2 = prop2;
        }
    }
}
";
            return VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource);
        }

        [Fact]
        public Task MixedProperties()
        {
            var source = @"
internal class C
{
    internal int Prop1 { get; set; }
    internal string Prop2 { get; }
}
";

            var expectedDiagnostics = new[]
            {
                // Test0.cs(2,1): hidden POD003: Type 'C' can be made immutable
                GetCSharpResultAt(2, 1, TypeCanBeImmutableAnalyzer.POD003, "C")
            };

            var fixedSource = @"
internal class C
{
    internal int Prop1 { get; }
    internal string Prop2 { get; }

    internal C(
        int prop1,
        string prop2)
    {
        Prop1 = prop1;
        Prop2 = prop2;
    }
}
";
            return VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource);
        }

        [Fact]
        public Task PreexistingConstructor()
        {
            var source = @"
internal class C
{
    internal int Prop1 { get; set; }

    internal string Prop2 { get; set; }

    internal C() { }

    void M1() { }
}
";

            var expectedDiagnostics = new[]
            {
                // Test0.cs(2,1): hidden POD003: Type 'C' can be made immutable
                GetCSharpResultAt(2, 1, TypeCanBeImmutableAnalyzer.POD003, "C")
            };

            var fixedSource = @"
internal class C
{
    internal int Prop1 { get; }

    internal string Prop2 { get; }

    internal C() { }

    internal C(
        int prop1,
        string prop2)
    {
        Prop1 = prop1;
        Prop2 = prop2;
    }

    void M1() { }
}
";
            return VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource);
        }

        [Fact]
        public Task CantBeImmutable()
        {
            var source = @"
internal class C
{
    internal int i;
}
";

            var fixedSource = @"
internal class C
{
    internal int i;
}
";
            return VerifyCodeFixAsync(source, fixedSource);
        }

        [Fact]
        public Task PropertyAttribute()
        {
            var source = @"
class A : System.Attribute { }

namespace NS
{
    internal class C
    {

        /// <summary>Gets the Prop1.</summary>
        [A]
        internal int Prop1 { get; set; }
        internal string Prop2 { get; }
    }
}
";

            var expectedDiagnostics = new[]
            {
                // Test0.cs(6,1): hidden POD003: Type 'NS.C' can be made immutable
                GetCSharpResultAt(6, 1, TypeCanBeImmutableAnalyzer.POD003, "NS.C")
            };

            var fixedSource = @"
class A : System.Attribute { }

namespace NS
{
    internal class C
    {

        /// <summary>Gets the Prop1.</summary>
        [A]
        internal int Prop1 { get; }
        internal string Prop2 { get; }

        internal C(
            int prop1,
            string prop2)
        {
            Prop1 = prop1;
            Prop2 = prop2;
        }
    }
}
";
            return VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource);
        }

        [Fact]
        public Task ParamNameIsReserved()
        {
            var source = @"
public class C
{
    public double Long { get; set; }
}
";

            var expectedDiagnostics = new[]
            {
                // Test0.cs(2,1): hidden POD003: Type 'C' can be made immutable
                GetCSharpResultAt(2, 1, TypeCanBeImmutableAnalyzer.POD003, "C")
            };

            var fixedSource = @"
public class C
{
    public double Long { get; }

    public C(
        double @long)
    {
        Long = @long;
    }
}
";
            return VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource);
        }

        [Fact]
        public Task ExpressionBodiedProperty()
        {
            var source = @"
public class C
{
    public int Computed => 42;
    public int Prop { get; set; }
}
";

            var expectedDiagnostics = new[]
            {
                // Test0.cs(2,1): hidden POD003: Type 'C' can be made immutable
                GetCSharpResultAt(2, 1, TypeCanBeImmutableAnalyzer.POD003, "C")
            };

            var fixedSource = @"
public class C
{
    public int Computed => 42;
    public int Prop { get; }

    public C(
        int prop)
    {
        Prop = prop;
    }
}
";
            return VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource);
        }
    }
}
