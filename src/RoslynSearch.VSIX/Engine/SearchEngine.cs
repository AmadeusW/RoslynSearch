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
using RoslynSearch.DTO;

namespace RoslynSearch.VSIX.Engine
{
    internal class SearchEngine
    {
        public static int ProgressMax { get; private set; }
        public static int Progress { get; private set; }
        static object ProgressLock = new object();

        public static async Task Search(string query, SearchSource source, string excludedFilesInput, bool usingScript, CancellationToken token, Action<IEnumerable<SearchResult>> partialResultHandler)
        {
            IEnumerable<string> excludedFiles = excludedFilesInput.Split(',').Select(n => n.Trim());
            switch (source)
            {
                case SearchSource.EntireSolution:
                    if (usingScript)
                        await Search(query, WorkspaceHelpers.GetCurrentSolution().Projects.SelectMany(n => n.Documents), token, partialResultHandler, excludedFiles);
                    else
                        await SearchInStrings(query, WorkspaceHelpers.GetCurrentSolution().Projects.SelectMany(n => n.Documents), token, partialResultHandler, excludedFiles);
                    return;

                case SearchSource.CurrentProject:
                    if (usingScript)
                        await Search(query, WorkspaceHelpers.GetCurrentProject().Documents, token, partialResultHandler, excludedFiles);
                    else
                        await SearchInStrings(query, WorkspaceHelpers.GetCurrentProject().Documents, token, partialResultHandler, excludedFiles);
                    return;

                case SearchSource.CurrentDocument:
                    if (usingScript)
                        await Search(query, new Document[] { WorkspaceHelpers.GetCurrentDocument() }, token, partialResultHandler, excludedFiles);
                    else
                        await SearchInStrings(query, new Document[] { WorkspaceHelpers.GetCurrentDocument() }, token, partialResultHandler, excludedFiles);
                    return;

                default:
                    throw new ArgumentOutOfRangeException(nameof(source));
            }
        }

        private static async Task SearchInStrings(string query, IEnumerable<Document> source, CancellationToken token, Action<IEnumerable<SearchResult>> partialResultHandler, IEnumerable<string> excludedFiles)
        {
            Progress = 0;
            ProgressMax = source.Count();

            await Task.WhenAll(source.Select(currentDocument => Task.Run(async() => 
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
            })));

        }

        private static async Task Search(string query, IEnumerable<Document> source, CancellationToken token, Action<IEnumerable<SearchResult>> partialResultHandler, IEnumerable<string> excludedFiles)
        {
            Progress = 0;
            ProgressMax = source.Count();

            var script = CSharpScript.Create<IEnumerable<SyntaxNode>>(
                query,
                ScriptOptions.Default
                    .WithReferences(typeof(SyntaxNode).Assembly, typeof(CSharpSyntaxNode).Assembly, typeof(Globals).Assembly)
                    .WithImports("Microsoft.CodeAnalysis", "Microsoft.CodeAnalysis.CSharp", "Microsoft.CodeAnalysis.CSharp.Syntax", "System.Linq", "RoslynSearch.DTO"),
                globalsType: typeof(Globals)
                );
            //foreach (var currentDocument in source)
            await Task.WhenAll(source.Select(currentDocument => Task.Run(async () =>
            {
                try
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
                }
                catch (Exception ex)
                {
                    var x = ex;
                }
                Progress++;
            })));
            //}
        }

    }
}
