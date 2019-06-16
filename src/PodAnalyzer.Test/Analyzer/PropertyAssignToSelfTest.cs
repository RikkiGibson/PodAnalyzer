using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Microsoft.CodeAnalysis.CSharp.Testing.XUnit;
using static PodAnalyzer.Test.TestUtilities;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis;

namespace PodAnalyzer.Test
{
    public class PropertyAssignToSelfTest : AnalyzerVerifier<PropertyAssignToSelfAnalyzer>
    {
        [Fact]
        public Task PropertyAssignToSelf_Warns()
        {
            var source = @"
class C
{
    string P { get; }
    C()
    {
        P = P;
    }
}
";
            return VerifyAnalyzerAsync(source,
                // Test0.cs(7,9): warning POD001: Property 'P' is assigned to itself
                GetCSharpResultAt(7, 9, PropertyAssignToSelfAnalyzer.POD001, "P"));
        }

        [Fact]
        public Task PropertyAssignToSelf_ExpressionBody_Warns()
        {
            var source = @"
class C
{
    string P { get; }
    C() => P = P;
}
";
            return VerifyAnalyzerAsync(source,
                // Test0.cs(5,12): warning POD001: Property 'P' is assigned to itself
                GetCSharpResultAt(5, 12, PropertyAssignToSelfAnalyzer.POD001, "P"));
        }

        [Fact]
        public Task ExternConstructor_NoWarning_NoCrash()
        {
            var source = @"
class C
{
    extern C();
}
";
            return VerifyAnalyzerAsync(source);
        }
    }
}
