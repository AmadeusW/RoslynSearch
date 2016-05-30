//------------------------------------------------------------------------------
// <copyright file="SearchWindowControl.xaml.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace RoslynSearch.VSIX
{
    using Engine;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Threading;
    /// <summary>
    /// Interaction logic for SearchWindowControl.
    /// </summary>
    public partial class SearchWindowControl : UserControl
    {
        DispatcherTimer _uiTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100),
        };
        
        public SearchWindowControl()
        {
            this.InitializeComponent();
            _uiTimer.Tick += uiTimerTick;
        }

        private void Search()
        {
            SearchSource source = default(SearchSource);

            if (SearchSolution.IsChecked == true)
                source = SearchSource.EntireSolution;
            else if (SearchProject.IsChecked == true)
                source = SearchSource.CurrentProject;
            else if (SearchDocument.IsChecked == true)
                source = SearchSource.CurrentDocument;

            BeginTrackingProgress();

            try
            {
                foreach (var result in SearchEngine.Search(Query.Text, source, usingScript: SearchQuery.IsChecked == true))
                {
                    OutputWindow.WriteLine(result.ToString());
                }
            }
            catch (Exception ex)
            {
                OutputWindow.WriteLine(ex.ToString());
            }

            EndTrackingProgress();
        }

        private void uiTimerTick(object sender, EventArgs e)
        {
            Progress.Maximum = SearchEngine.ProgressMax;
            Progress.Value = SearchEngine.Progress;
            if (Progress.Value == 0 || Progress.Value == Progress.Maximum)
                Progress.Visibility = Visibility.Hidden;
            else
                Progress.Visibility = Visibility.Visible;
        }

        private void SearchWithScriptChecked(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(Query.Text))
            {
                Query.Text = "root.DescendantNodes().OfType<LiteralExpressionSyntax>().Where(n => n.IsKind(SyntaxKind.StringLiteralExpression)).Where(n => n.ToString().Contains(query))";
            }
        }

        private void BeginTrackingProgress()
        {
            _uiTimer.Start();
        }

        private void EndTrackingProgress()
        {
            _uiTimer.Stop();
        }

        private void SearchButtonClick(object sender, RoutedEventArgs e)
        {
            Search();
        }

        private void QueryBoxKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                Search();
            }
        }
    }
}