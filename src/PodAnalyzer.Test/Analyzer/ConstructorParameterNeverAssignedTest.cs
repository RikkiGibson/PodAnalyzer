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

    public class ConstructorParameterNeverAssignedTest : AnalyzerVerifier<ConstructorParameterNeverAssignedAnalyzer>
    {
        [Fact]
        public Task UsedParam_NoWarning()
        {
            var source = @"
public class A
{
    public int I { get; }
    public A(int i) { I = i; }
}
";
            return VerifyAnalyzerAsync(source);
        }

        [Fact]
        public Task UnusedParam_Warns()
        {
            var source = @"
public class A
{
    public int I { get; }
    public A(int i, int j) { I = i; }
}
";
            return VerifyAnalyzerAsync(source,
                // Test0.cs(5,25): warning POD004: Parameter 'j' was never used
                GetCSharpResultAt(5, 25, ConstructorParameterNeverAssignedAnalyzer.POD004, "j"));
        }

        [Fact]
        public Task ConstructorInitializer_UsedParam_NoWarning()
        {
            var source = @"
public class A
{
    public A(int x) { _ = x; }
    public A(int x, int y) : this(x + y) { }
}
";

            return VerifyAnalyzerAsync(source);
        }

        [Fact]
        public Task ConstructorInitializer_UnusedParam_Warns()
        {
            var source = @"
public class A
{
    public A(int x) { _ = x; }
    public A(int x, int y) : this(x) { }
}
";

            return VerifyAnalyzerAsync(source,
            // Test0.cs(5,25): warning POD004: Parameter 'y' was never used
            GetCSharpResultAt(5, 25, ConstructorParameterNeverAssignedAnalyzer.POD004, "y"));
        }

        [Fact]
        public Task BaseInitializer_UnusedParam_Warns()
        {
            var source = @"
public class A
{
    public A(int i) { _ = i; }
}

public class B : A
{

    public B(int i) : base(0) { }
}
";

            return VerifyAnalyzerAsync(source,
                // Test0.cs(10,18): warning POD004: Parameter 'i' was never used
                GetCSharpResultAt(10, 18, ConstructorParameterNeverAssignedAnalyzer.POD004, "i"));
        }


    }
}
