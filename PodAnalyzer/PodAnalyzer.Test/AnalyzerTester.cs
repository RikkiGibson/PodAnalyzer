using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PodAnalyzer.Test
{
    public class AnalyzerTester
    {
        private static readonly ImmutableArray<MetadataReference> _coreReferences = ImmutableArray.Create<MetadataReference>(
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location));

        private static readonly Project _baseProject = new AdhocWorkspace()
            .AddProject("Test", LanguageNames.CSharp)
            .AddMetadataReferences(_coreReferences);

        private readonly string _resourceFolderPath;
        private readonly DiagnosticAnalyzer _analyzer;
        public AnalyzerTester(string resourceFolderPath, DiagnosticAnalyzer analyzer)
        {
            _resourceFolderPath = resourceFolderPath;
            _analyzer = analyzer;
        }

        public Project CreateTestProject(string testFilename)
        {
            var path = Path.Combine(_resourceFolderPath, testFilename);
            var source = File.ReadAllText(path);

            var document = _baseProject.AddDocument(name: testFilename, text: source, filePath: path);

            return document.Project;
        }

        public async Task<ImmutableArray<Diagnostic>> ComputeDiagnostics(Project project)
        {
            var comp = await project.GetCompilationAsync();
            var compAnalyzed = comp.WithAnalyzers(ImmutableArray.Create(_analyzer));
            var diagnostics = await compAnalyzed.GetAllDiagnosticsAsync();
            return diagnostics;
        }
    }
}
