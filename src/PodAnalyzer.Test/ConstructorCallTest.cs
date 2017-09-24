using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PodAnalyzer.Test
{
    public class ConstructorCallTest
    {
        const string _resourceFolderPath = "Resources/CodeFix";

        private readonly AnalyzerTester _analyzerTester = new AnalyzerTester(
            resourceFolderPath: _resourceFolderPath,
            analyzer: new TypeCanBeImmutableAnalyzer());

        private readonly CodeFixTester _codeFixTester = new CodeFixTester(new ConstructorCallProvider());
        
        public async Task TestCodeFix(string baseFilename)
        {
            var filename = Path.ChangeExtension(baseFilename, ".cs");
            var project = _analyzerTester.CreateTestProject(filename);
            var diagnostics = await _analyzerTester.ComputeDiagnostics(project: project);
            var actions = await _codeFixTester.CreateCodeActionsAsync(project.Documents.First(), diagnostics);
            var newDocument = await _codeFixTester.ApplyFixAsync(project.Documents.First(), actions.First());

            var newText = await newDocument.GetTextAsync();
            var newTextString = newText.ToString();

            // It doesn't please me to do this, but Roslyn seems to be inserting \r\n uninvited.
            // TODO: factor this out as a helper
            if (Environment.NewLine == "\n")
            {
                newTextString = newTextString.Replace("\r\n", "\n");
            }

            var expectedPath = Path.Combine(_resourceFolderPath, Path.ChangeExtension(baseFilename, ".out.cs"));
            var expectedText = File.ReadAllText(expectedPath);
            Assert.Equal(expectedText, newTextString);
        }

        [Fact]
        public Task SimpleTest() => TestCodeFix("ConstructorCall");
    }
}
