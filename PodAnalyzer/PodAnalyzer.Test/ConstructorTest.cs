using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PodAnalyzer.Test
{
    public class ConstructorTest
    {
        const string _resourceFolderPath = "Resources/CodeFix";

        private readonly AnalyzerTester _analyzerTester = new AnalyzerTester(
            resourceFolderPath: _resourceFolderPath,
            analyzer: new TypeCanBeImmutableAnalyzer());

        private readonly CodeFixTester _codeFixTester = new CodeFixTester(new ConstructorProvider());
        
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
            if (Environment.NewLine == "\n")
            {
                newTextString = newTextString.Replace("\r\n", "\n");
            }

            var expectedPath = Path.Combine(_resourceFolderPath, Path.ChangeExtension(baseFilename, ".out.cs"));
            var expectedText = File.ReadAllText(expectedPath);
            Assert.Equal(expectedText, newTextString);
        }

        [Fact]
        public Task SimpleTest() => TestCodeFix("Test1");

        [Fact]
        public Task MultiPropTest() => TestCodeFix("MultiProp");
    }
}
