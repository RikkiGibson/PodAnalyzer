using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TestHelper;
using Xunit;
using Xunit.Abstractions;

namespace PodAnalyzer.Test
{
    public class PropertyAssignToSelfTest
    {
        static readonly AnalyzerTester tester = new AnalyzerTester(
            resourceFolderPath: "Resources/Analyzer",
            analyzer: new PropertyAssignToSelfAnalyzer());

        [Fact]
        public async Task Test()
        {
            var project = tester.CreateTestProject("Test1.cs");
            var diags = (await tester.ComputeDiagnostics(project: project)).Count(d => d.Id == PropertyAssignToSelfAnalyzer.Rule.Id);
            Assert.Equal(1, diags);
        }
    }
}
