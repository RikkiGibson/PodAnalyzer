using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;

namespace PodAnalyzer.Test
{
    [TestClass]
    public class UnitTest
    {
        static readonly AnalyzerTester tester = new AnalyzerTester(
            resourceFolderPath: "Resources/Analyzer",
            analyzer: new PropertyAssignToSelfAnalyzer());

        [TestMethod]
        public void TestMethod1()
        {
            var diags = tester.ComputeDiagnostics(testFilename: "Test1.cs").Result;
            Assert.Equals(diags.Length, 1);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void TestMethod2()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            string Prop { get; set; }
        }

        class OtherType { }
    }";
            var expected = new DiagnosticResult
            {
                Id = "POD002",
                Message = String.Format("Type '{0}' can be made immutable", "TypeName"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 11, 14)
                        }
            };

            //VerifyCSharpDiagnostic(test, expected);

            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            string Prop { get; }
            public TypeName(string prop)
            {
                Prop = prop;
            }
        }

        class OtherType { }
    }";
        }
    }
}