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

    /// <summary>
    /// Interaction logic for SearchWindowControl.
    /// </summary>
    public partial class SearchWindowControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SearchWindowControl"/> class.
        /// </summary>
        public SearchWindowControl()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        private void SearchButtonClick(object sender, RoutedEventArgs e)
        {
            SearchSource source = default(SearchSource);

            if (SearchSolution.IsChecked == true)
                source = SearchSource.EntireSolution;
            else if (SearchProject.IsChecked == true)
                source = SearchSource.CurrentProject;
            else if (SearchDocument.IsChecked == true)
                source = SearchSource.CurrentDocument;

            Results.Text = String.Join("\n", SearchEngine.Search(Query.Text, source).Select(n => n.ToString()));
        }
    }
}