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
        static CancellationTokenSource tokenSource = null;

        public static IEnumerable<SearchResult> Search(string query, SearchSource source, bool usingScript, CancellationToken token = default(CancellationToken))
        {
            if (token == null)
            {
                tokenSource?.Cancel();
                tokenSource = new CancellationTokenSource();
                token = tokenSource.Token;
            }

            switch (source)
            {
                case SearchSource.EntireSolution:
                    if (usingScript)
                        return Search(query, WorkspaceHelpers.GetCurrentSolution().Projects.SelectMany(n => n.Documents), token);
                    else
                        return SearchInStrings(query, WorkspaceHelpers.GetCurrentSolution().Projects.SelectMany(n => n.Documents), token);

                case SearchSource.CurrentProject:
                    if (usingScript)
                        return Search(query, WorkspaceHelpers.GetCurrentProject().Documents, token);
                    else
                        return SearchInStrings(query, WorkspaceHelpers.GetCurrentProject().Documents, token);

                case SearchSource.CurrentDocument:
                    if (usingScript)
                        return Search(query, new Document[] { WorkspaceHelpers.GetCurrentDocument() }, token);
                    else
                        return SearchInStrings(query, new Document[] { WorkspaceHelpers.GetCurrentDocument() }, token);

                default:
                    throw new ArgumentOutOfRangeException(nameof(source));
            }
        }

        private static IEnumerable<SearchResult> SearchInStrings(string query, IEnumerable<Document> source, CancellationToken token)
        {
            List<SearchResult> results = new List<SearchResult>();
            Parallel.ForEach(source, async currentDocument =>
            {
                var root = await currentDocument.GetSyntaxRootAsync(token);
                var matches = root.DescendantNodes().OfType<LiteralExpressionSyntax>().Where(n => n.IsKind(SyntaxKind.StringLiteralExpression)).Where(n => n.ToString().Contains(query));
                results.AddRange(matches.Select(m =>
                {
                    var position = m.GetLocation().GetLineSpan();
                    return new SearchResult()
                    {
                        ExactMatch = m.ToString(),
                        LineNumber = position.StartLinePosition.Line,
                        Path = position.Path,
                        EntireLine = String.Empty,
                    };
                }));
            });
            return results;

        }

        class Globals
        {
            internal SyntaxNode SyntaxRoot;
        }

        private static IEnumerable<SearchResult> Search(string query, IEnumerable<Document> source, CancellationToken token)
        {
            List<SearchResult> results = new List<SearchResult>();
            try
            {
                var script = CSharpScript.Create<IEnumerable<SyntaxNode>>(
                    query,
                    ScriptOptions.Default
                        .WithReferences(typeof(SyntaxNode).Assembly)
                        .WithImports("Microsoft.CodeAnalysis", "Microsoft.CodeAnalysis.CSharp", "Microsoft.CodeAnalysis.CSharp.Syntax", "System.Linq"),
                    globalsType: typeof(Globals)
                    );
                Parallel.ForEach(source, async currentDocument =>
                {
                    var root = await currentDocument.GetSyntaxRootAsync(token);
                    var matches = await script.RunAsync(new Globals { SyntaxRoot = root });
                    results.AddRange(matches.ReturnValue.Select(m =>
                    {
                        var position = m.GetLocation().GetLineSpan();
                        return new SearchResult()
                        {
                            ExactMatch = m.ToString(),
                            LineNumber = position.StartLinePosition.Line,
                            Path = position.Path,
                            EntireLine = String.Empty,
                        };
                    }));
                });
                return results;
            }
            catch (CompilationErrorException cee)
            {
                throw;
            }
            
        }

    }
}
