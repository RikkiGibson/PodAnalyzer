using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PodAnalyzer.Test
{
    public class ConstructorParameterNeverAssignedTest
    {
        const string _resourceFolderPath = "Resources/Analyzer";

        private readonly AnalyzerTester tester = new AnalyzerTester(
            resourceFolderPath: _resourceFolderPath,
            analyzer: new ConstructorParameterNeverAssignedAnalyzer());

        [Fact]
        public async Task TestConstructorInitializerUsesParamNoWarning()
        {
            var project = tester.CreateTestProject("ConstructorInitializer.cs");
            var diags = (await tester.ComputeDiagnostics(project: project)).Count(d => d.Id == ConstructorParameterNeverAssignedAnalyzer.Rule.Id);
            Assert.Equal(0, diags);
        }
    }
}
