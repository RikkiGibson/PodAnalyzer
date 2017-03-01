using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PodAnalyzer.Test
{
    public class CodeFixTester
    {
        private readonly CodeFixProvider _provider;
        public CodeFixTester(CodeFixProvider provider)
        {
            _provider = provider;
        }

        public async Task<ImmutableArray<CodeAction>> CreateCodeActionsAsync(Document document, IEnumerable<Diagnostic> diagnostics)
        {
            var diagnostic = diagnostics.First(d => _provider.FixableDiagnosticIds.Contains(d.Id));
            var actions = new List<CodeAction>();
            var context = new CodeFixContext(
                document,
                diagnostic,
                registerCodeFix: (action, diags) => actions.Add(action),
                cancellationToken: CancellationToken.None);

            await _provider.RegisterCodeFixesAsync(context);
            return actions.ToImmutableArray();
        }

        public async Task<Document> ApplyFixAsync(Document document, CodeAction action)
        {
            var operations = await action.GetOperationsAsync(CancellationToken.None);
            var operation = operations.OfType<ApplyChangesOperation>().Single();
            var newDocument = operation.ChangedSolution.GetDocument(document.Id);
            return newDocument;
        }
    }
}
