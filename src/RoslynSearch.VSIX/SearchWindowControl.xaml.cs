//------------------------------------------------------------------------------
// <copyright file="SearchWindowControl.xaml.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace RoslynSearch.VSIX
{
    using Engine;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading;
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
        static CancellationTokenSource tokenSource = null;
        bool _searching;

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

            BeginSearch();

            tokenSource?.Cancel();
            tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            try
            {
                SearchEngine.Search(Query.Text, source, ExcludedFiles.Text, SearchQuery.IsChecked == true, token, handleResults);
                OutputWindow.WriteLine("---");
                OutputWindow.WriteLine($"Search finished. Processed {SearchEngine.Progress} files.");
            }
            catch (Exception ex)
            {
                OutputWindow.WriteLine("---");
                OutputWindow.WriteLine(ex.ToString());
            }

            EndSearch();
        }

        private void handleResults(IEnumerable<SearchResult> partialResults)
        {
            foreach (var result in partialResults)
            {
                OutputWindow.WriteLine(result.ToString());
            }
            Dispatcher.BeginInvoke(new Action(() => updateUI()));
        }

        private void uiTimerTick(object sender, EventArgs e)
        {
            updateUI();
        }

        private void updateUI()
        {
            Progress.Maximum = SearchEngine.ProgressMax;
            Progress.Value = SearchEngine.Progress;
            if (!_searching || Progress.Value == 0 || Progress.Value == Progress.Maximum)
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

        private void BeginSearch()
        {
            _searching = true;
            OutputWindow.Activate();
            _uiTimer.Start();
            Progress.Value = 0;
            StopButton.Visibility = Visibility.Visible;
            SearchButton.Visibility = Visibility.Collapsed;
        }

        private void EndSearch()
        {
            _searching = false;
            OutputWindow.Activate();
            _uiTimer.Stop();
            Progress.Visibility = Visibility.Hidden;
            Progress.Value = 0;
            StopButton.Visibility = Visibility.Collapsed;
            SearchButton.Visibility = Visibility.Visible;
        }

        private void SearchButtonClick(object sender, RoutedEventArgs e)
        {
            Search();
        }

        private void StopButtonClick(object sender, RoutedEventArgs e)
        {
            tokenSource?.Cancel();
            EndSearch();
            return;
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