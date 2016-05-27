using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoslynSearch.VSIX.Helpers
{
    internal class WorkspaceHelpers
    {
        private static IVsTextManager TextManager => (IVsTextManager)ServiceProvider.GlobalProvider.GetService(typeof(SVsTextManager));

        internal static Workspace CurrentWorkspace
        {
            get
            {
                var componentModel = (IComponentModel)ServiceProvider.GlobalProvider.GetService(typeof(SComponentModel));
                return componentModel.GetService<VisualStudioWorkspace>();
            }
        }

        internal static Solution GetCurrentSolution() => CurrentWorkspace?.CurrentSolution;
        internal static Project GetCurrentProject() => GetCurrentDocument().Project;

        internal static Document GetDocument(string filePath)
        {
            var project = GetCurrentSolution().Projects.Where(n => n.Documents.Any(m => m.FilePath == filePath)).FirstOrDefault();
            var document = project.Documents.Where(n => n.FilePath == filePath).Single();
            return document;
        }

        internal static Document GetCurrentDocument()
        {
            int startPosition, endPosition;
            string filePath;
            if (TextManager.TryFindDocumentAndPosition(out filePath, out startPosition, out endPosition))
            {
                try
                {
                    return WorkspaceHelpers.GetDocument(filePath);

                }
                catch (NullReferenceException)
                {
                    StatusBar.ShowStatus($"Error accessing the document. Try building the solution.");
                }
            }
            return null;
        }

        // Consider removing this function, and thus a dependency on Microsoft.CodeAnalysis
        internal static async Task<Tuple<SyntaxNode, Document>> GetSelectedSyntaxNode()
        {
            int startPosition, endPosition;
            string filePath;
            if (TextManager.TryFindDocumentAndPosition(out filePath, out startPosition, out endPosition))
            {
                Document document;
                try
                {
                    document = WorkspaceHelpers.GetDocument(filePath);
                }
                catch (NullReferenceException)
                {
                    StatusBar.ShowStatus($"Error accessing the document. Try building the solution.");
                    return null;
                }
                var root = await document.GetSyntaxRootAsync();
                var element = root.FindNode(new Microsoft.CodeAnalysis.Text.TextSpan(startPosition, endPosition - startPosition));
                return Tuple.Create(element, document);
            }
            else
            {
                StatusBar.ShowStatus("To use Roslyn Search, please navigate to C# code.");
                return null;
            }
        }
    }
}
