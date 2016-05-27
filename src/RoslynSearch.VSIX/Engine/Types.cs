using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoslynSearch.VSIX.Engine
{
    public enum SearchSource
    {
        EntireSolution,
        CurrentProject,
        CurrentDocument
    }

    public struct SearchResult
    {
        public string ExactMatch;
        public string EntireLine;
        public string Path;
        public int LineNumber;

        public override string ToString() => $"{Path}:{LineNumber}: {ExactMatch}";
    }
}
