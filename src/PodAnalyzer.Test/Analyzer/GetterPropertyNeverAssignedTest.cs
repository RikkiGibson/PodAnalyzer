using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PodAnalyzer.Test.Analyzer
{
    public class GetterPropertyNeverAssignedTest
    {
        const string _resourceFolderPath = "Resources/Analyzer";

        private readonly AnalyzerTester tester = new AnalyzerTester(
            resourceFolderPath: _resourceFolderPath,
            analyzer: new ConstructorParameterNeverAssignedAnalyzer());

        [Fact]
        public async Task TestExpressionBodiedPropertyDoesntCrash()
        {
            var project = tester.CreateTestProject("ArrowProperty.cs");
            var diags = await tester.ComputeDiagnostics(project: project);
            Assert.Equal(ImmutableArray<Diagnostic>.Empty, diags);
        }
    }
}
