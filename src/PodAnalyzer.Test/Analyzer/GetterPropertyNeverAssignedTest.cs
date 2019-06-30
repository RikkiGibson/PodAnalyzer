using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Microsoft.CodeAnalysis.CSharp.Testing.XUnit;
using static PodAnalyzer.Test.TestUtilities;

namespace PodAnalyzer.Test
{
    public class GetterPropertyNeverAssignedTest : AnalyzerVerifier<GetterPropertyNeverAssignedAnalyzer>
    {
        [Fact]
        public Task GetterOnlyAutoProperty_NoConstructor_Warns()
        {
            var source = @"
class C
{
    int P { get; }
}
";
            return VerifyAnalyzerAsync(source,
                // Test0.cs(4,9): warning POD002: Getter-only property 'C.P' was never assigned to
                GetCSharpResultAt(4, 9, GetterPropertyNeverAssignedAnalyzer.POD002, "C.P"));
        }

        [Fact]
        public Task AutoProperty_NoConstructor_NoWarning()
        {
            var source = @"
class C
{
    int P { get; set; }
}
";
            return VerifyAnalyzerAsync(source);
        }

        [Fact]
        public Task GetterOnlyAutoProperty_WithConstructor_NoWarning()
        {
            var source = @"
class C
{
    int P { get; }
    C(int p)
    {
        P = p;
    }
}
";
            return VerifyAnalyzerAsync(source);
        }

        [Fact]
        public Task GetterOnlyAutoProperty_NotDefinitelyAssigned_NoWarning()
        {
            var source = @"
class C
{
    int P { get; }
    C(int p)
    {
        if (p == 0)
        {
            P = p;
        }
    }
}
";
            return VerifyAnalyzerAsync(source);
        }

        [Fact]
        public Task GetterOnlyAutoProperty_ExternConstructor_NoWarning()
        {
            var source = @"
class C
{
    int P { get; }
    extern C();
}
";
            return VerifyAnalyzerAsync(source);
        }

        [Fact]
        public Task GetterOnlyAutoProperty_ExpressionBodyConstructor_NoWarning()
        {
            var source = @"
class C
{
    int P { get; }
    C(int p) => P = P;
}
";
            return VerifyAnalyzerAsync(source);
        }

        [Fact]
        public Task GetterOnlyAutoProperty_WithConstructor_Warns()
        {
            var source = @"
class C
{
    int P { get; }
    C() { }
}
";
            return VerifyAnalyzerAsync(source,
                // Test0.cs(4,9): warning POD002: Getter-only property 'C.P' was never assigned to
                GetCSharpResultAt(4, 9, GetterPropertyNeverAssignedAnalyzer.POD002, "C.P"));
        }

        [Fact]
        public Task GetterOnlyAutoProperty_TwoConstructors_Warns()
        {
            var source = @"
class C
{
    int P { get; }
    C(int p) { P = p; }
    C() { }
}
";
            return VerifyAnalyzerAsync(source,
                // Test0.cs(5,9): warning POD002: Getter-only property 'C.P' was never assigned to
                GetCSharpResultAt(4, 9, GetterPropertyNeverAssignedAnalyzer.POD002, "C.P"));
        }

        [Fact]
        public Task GetterOnlyAutoProperty_TwoConstructors_Chaining_NoWarning()
        {
            var source = @"
class C
{
    int P { get; }
    C(int p) { P = p; }
    C() : this(42) { }
}
";
            return VerifyAnalyzerAsync(source);
        }

        [Fact]
        public Task GetterOnlyAutoProperty_TwoConstructors_BaseChaining_Warns()
        {
            var source = @"
class C1
{
    protected C1(int x) { }
}

class C2 : C1
{
    int P { get; }
    C2() : base(42) { }
}
";
            return VerifyAnalyzerAsync(source,
                // Test0.cs(9,9): warning POD002: Getter-only property 'C2.P' was never assigned to
                GetCSharpResultAt(9, 9, GetterPropertyNeverAssignedAnalyzer.POD002, "C2.P"));
        }

        [Fact]
        public Task ExpressionBodiedProperty_NoWarning_NoCrash()
        {
            var source = @"
class C
{
    int P => 42;
}
";
            return VerifyAnalyzerAsync(source);
        }
    }
}
