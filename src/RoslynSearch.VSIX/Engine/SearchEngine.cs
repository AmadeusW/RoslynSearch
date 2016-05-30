using RoslynSearch.VSIX.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace RoslynSearch.VSIX.Engine
{
    internal class SearchEngine
    {
        public static int ProgressMax { get; private set; }
        public static int Progress { get; private set; }
        static object ProgressLock = new object();

        public static  void Search(string query, SearchSource source, string excludedFilesInput, bool usingScript, CancellationToken token, Action<IEnumerable<SearchResult>> partialResultHandler)
        {
            string[] excludedFiles = excludedFilesInput.Split(',');
            switch (source)
            {
                case SearchSource.EntireSolution:
                    if (usingScript)
                        Search(query, WorkspaceHelpers.GetCurrentSolution().Projects.SelectMany(n => n.Documents), token, partialResultHandler, excludedFiles);
                    else
                        SearchInStrings(query, WorkspaceHelpers.GetCurrentSolution().Projects.SelectMany(n => n.Documents), token, partialResultHandler, excludedFiles);
                    return;

                case SearchSource.CurrentProject:
                    if (usingScript)
                        Search(query, WorkspaceHelpers.GetCurrentProject().Documents, token, partialResultHandler, excludedFiles);
                    else
                        SearchInStrings(query, WorkspaceHelpers.GetCurrentProject().Documents, token, partialResultHandler, excludedFiles);
                    return;

                case SearchSource.CurrentDocument:
                    if (usingScript)
                        Search(query, new Document[] { WorkspaceHelpers.GetCurrentDocument() }, token, partialResultHandler, excludedFiles);
                    else
                        SearchInStrings(query, new Document[] { WorkspaceHelpers.GetCurrentDocument() }, token, partialResultHandler, excludedFiles);
                    return;

                default:
                    throw new ArgumentOutOfRangeException(nameof(source));
            }
        }

        private static void SearchInStrings(string query, IEnumerable<Document> source, CancellationToken token, Action<IEnumerable<SearchResult>> partialResultHandler, string[] excludedFiles)
        {
            Progress = 0;
            ProgressMax = source.Count();

            Parallel.ForEach(source, async currentDocument =>
            {
                if (excludedFiles.Any(e => !String.IsNullOrEmpty(e) && currentDocument.FilePath.Contains(e)))
                    return;

                var root = await currentDocument.GetSyntaxRootAsync(token);
                var matches = root.DescendantNodes().OfType<LiteralExpressionSyntax>().Where(n => n.IsKind(SyntaxKind.StringLiteralExpression)).Where(n => n.ToString().Contains(query));
                if (matches.Any())
                {
                    var results = matches.Select(m =>
                    {
                        var position = m.GetLocation().GetLineSpan();
                        return new SearchResult()
                        {
                            ExactMatch = m.ToString(),
                            LineNumber = position.StartLinePosition.Line,
                            Path = position.Path,
                            EntireLine = String.Empty,
                        };
                    });
                    partialResultHandler(results);
                }
                Progress++;
            });

        }

        class Globals
        {
            internal SyntaxNode SyntaxRoot;
        }

        private static void Search(string query, IEnumerable<Document> source, CancellationToken token, Action<IEnumerable<SearchResult>> partialResultHandler, string[] excludedFiles)
        {
            Progress = 0;
            ProgressMax = source.Count();

            var script = CSharpScript.Create<IEnumerable<SyntaxNode>>(
                query,
                ScriptOptions.Default
                    .WithReferences(typeof(SyntaxNode).Assembly)
                    .WithImports("Microsoft.CodeAnalysis", "Microsoft.CodeAnalysis.CSharp", "Microsoft.CodeAnalysis.CSharp.Syntax", "System.Linq"),
                globalsType: typeof(Globals)
                );
            Parallel.ForEach(source, async currentDocument =>
            {
                if (excludedFiles.Any(e => !String.IsNullOrEmpty(e) && currentDocument.FilePath.Contains(e)))
                    return;

                var root = await currentDocument.GetSyntaxRootAsync(token);
                var matches = await script.RunAsync(new Globals { SyntaxRoot = root });
                if (matches.ReturnValue.Any())
                {
                    var results = matches.ReturnValue.Select(m =>
                    {
                        var position = m.GetLocation().GetLineSpan();
                        return new SearchResult()
                        {
                            ExactMatch = m.ToString(),
                            LineNumber = position.StartLinePosition.Line,
                            Path = position.Path,
                            EntireLine = String.Empty,
                        };
                    });
                    partialResultHandler(results);
                }
                Progress++;
            });
        }

    }
}
