using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoslynSearch.VSIX
{
    public static class OutputWindow
    {
        private static IVsOutputWindowPane _customOutputPane;

        static Guid OutputWindowGuid = new Guid("706EBFDF-4F59-4E67-BBB8-B169556CF906");

        internal static void Initialize(Package searchWindowPackage)
        {
            var outputWindow = ServiceProvider.GlobalProvider.GetService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            string customTitle = "Roslyn Search Results";
            outputWindow.CreatePane(ref OutputWindowGuid, customTitle, 1, 1);

            IVsOutputWindowPane customPane;
            outputWindow.GetPane(ref OutputWindowGuid, out customPane);
            _customOutputPane = customPane;
        }

        public static void WriteLine(string message)
        {
            if (String.IsNullOrEmpty(message))
            {
                return;
            }

            if (_customOutputPane == null)
            {
                // There is no output pane associated with unit tests. Use debug output instead.
                System.Diagnostics.Debug.WriteLine(message);
                return;
            }
            _customOutputPane.OutputStringThreadSafe(message + Environment.NewLine);
        }

        /// <summary>
        /// Brings user's attention to the Alive output pane.
        /// </summary>
        public static void Activate()
        {
            _customOutputPane?.Activate();
        }
    }
}
