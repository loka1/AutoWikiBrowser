/*
Copyright (C) 2007 Martin Richards
(C) 2008 Stephen Kennedy, Sam Reed

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

using System;
using System.Text;
using System.Web;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml;
using WikiFunctions.Controls.Lists;

namespace WikiFunctions.Lists
{
    //TODO: normalise usage of FirstToUpperAndRemoveHashOnArray() and alikes

    /// <summary>
    /// Gets a list of pages in Named Categories for the ListMaker (Non-Recursive)
    /// </summary>
    public class CategoryListProvider : CategoryProviderBase
    {
        public override List<Article> MakeList(params string[] searchCriteria)
        {
            List<Article> list = new List<Article>();

            foreach (string page in PrepareCategories(searchCriteria))
            {
                list.AddRange(GetListing(page, list.Count));
            }

            return list;
        }

        public override string DisplayText
        { get { return "Category"; } }
    }

    /// <summary>
    /// Gets a list of pages in Named Categories for the ListMaker (Recursive - Will visit ALL subcategories)
    /// </summary>
    public class CategoryRecursiveListProvider : CategoryProviderBase
    {
        #region Internals
        public const int MaxDepth = 30;

        int m_Depth = MaxDepth;
        /// <summary>
        /// Maximum recursion depth during category scan
        /// </summary>
        public int Depth
        {
            get { return m_Depth; }
            set { m_Depth = Math.Min(value, MaxDepth); }
        }
        #endregion

        public CategoryRecursiveListProvider()
            : this(MaxDepth)
        { }

        public CategoryRecursiveListProvider(int depth)
        {
            Depth = depth;
            Limit = 200000;
        }

        public override List<Article> MakeList(params string[] searchCriteria)
        {
            List<Article> list = new List<Article>();

            lock (Visited)
            {
                Visited.Clear();
                foreach (string page in PrepareCategories(searchCriteria))
                {
                    list.AddRange(RecurseCategory(page, list.Count, Depth));
                }
                Visited.Clear();
            }

            return list;
        }

        public override string DisplayText
        { get { return "Category (recursive)"; } }
    }

    /// <summary>
    /// Gets a list of pages in Named Categories for the ListMaker (Recursive - Will visit 1 level of subcategories)
    /// </summary>
    public class CategoryRecursiveOneLevelListProvider : CategoryRecursiveListProvider
    {
        public CategoryRecursiveOneLevelListProvider()
            : base(1)
        { }

        public override string DisplayText
        {
            get { return "Category (recurse 1 level)"; }
        }
    }

    /// <summary>
    /// Gets a list of pages in Named Categories for the ListMaker (Recursive - Will visit the specified number of levels of subcategories)
    /// </summary>
    public class CategoryRecursiveUserDefinedLevelListProvider : CategoryRecursiveListProvider
    {
        public CategoryRecursiveUserDefinedLevelListProvider()
            : base(0)
        { }

        public override List<Article> MakeList(params string[] searchCriteria)
        {
            int userDepth = Tools.GetNumberFromUser(false);
            if (userDepth < 0)
                return new List<Article>();

            Depth = userDepth;

            return base.MakeList(searchCriteria);
        }

        public override string DisplayText
        {
            get { return "Category (recurse user defined level)"; }
        }
    }

    /// <summary>
    /// Gets a list of pages from a text file
    /// </summary>
    public class TextFileListProvider : IListProvider
    {
        private readonly static Regex RegexFromFile = new Regex("(^[a-z]{2,3}:)|(simple:)", RegexOptions.Compiled);
        private readonly static Regex LoadWikiLink = new Regex(@"\[\[:?(.*?)(?:\]\]|\|)", RegexOptions.Compiled);
        private readonly OpenFileDialog openListDialog = new OpenFileDialog();

        public TextFileListProvider()
        {
            openListDialog.Filter = "Text files|*.txt|Text files (no validation)|*.txt|All files|*.*";
            openListDialog.Multiselect = true;
        }

        public List<Article> MakeList(string searchCriteria)
        {
            return MakeList(searchCriteria.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
        }

        public List<Article> MakeList()
        {
            return MakeList(new string[0]);
        }

        public List<Article> MakeList(params string[] searchCriteria)
        {
            List<Article> list = new List<Article>();
            try
            {
                if (searchCriteria.Length == 0 && openListDialog.ShowDialog() == DialogResult.OK)
                    searchCriteria = openListDialog.FileNames;

                foreach (string fileName in searchCriteria)
                {
                    string pageText, title;

                    using (StreamReader sr = new StreamReader(fileName, Encoding.UTF8))
                    {
                        pageText = sr.ReadToEnd();
                        sr.Close();
                    }

                    switch (openListDialog.FilterIndex)
                    {
                        case 2:
                            foreach (string s in pageText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                list.Add(new Article(Tools.RemoveSyntax(Tools.TurnFirstToUpper(s.Trim()))));
                            }
                            break;
                        default:
                            if (LoadWikiLink.IsMatch(pageText))
                            {
                                foreach (Match m in LoadWikiLink.Matches(pageText))
                                {
                                    title = m.Groups[1].Value;
                                    if (!RegexFromFile.IsMatch(title) && (!(title.StartsWith("#"))))
                                    {
                                        list.Add(new Article(Tools.RemoveSyntax(Tools.TurnFirstToUpper(title))));
                                    }
                                }
                            }
                            else
                            {
                                foreach (string s in pageText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                                {
                                    if (s.Trim().Length == 0 || !Tools.IsValidTitle(s)) continue;
                                    list.Add(new Article(Tools.RemoveSyntax(Tools.TurnFirstToUpper(s.Trim()))));
                                }
                            }
                            break;
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex);
                return list;
            }
        }

        #region ListMaker properties
        public string DisplayText
        { get { return "Text file"; } }

        public string UserInputTextBoxText
        { get { return ""; } }

        public bool UserInputTextBoxEnabled
        { get { return false; } }

        public void Selected() { }

        public bool RunOnSeparateThread
        { get { return false; } }
        #endregion
    }

    /// <summary>
    /// Gets a list of Categories on the specified pages
    /// </summary>
    public class CategoriesOnPageListProvider : ApiListProviderBase
    {
        protected string clshow;

        #region Tags: <categories>/<cl>
        static readonly List<string> pe = new List<string>(new[] { "cl" });
        protected override ICollection<string> PageElements
        {
            get { return pe; }
        }

        static readonly List<string> ac = new List<string>(new[] { "categories" });
        protected override ICollection<string> Actions
        {
            get { return ac; }
        }
        #endregion

        public override List<Article> MakeList(params string[] searchCriteria)
        {
            searchCriteria = Tools.FirstToUpperAndRemoveHashOnArray(searchCriteria);

            List<Article> list = new List<Article>();

            foreach (string page in searchCriteria)
            {
                string url = Variables.URLApi + "?action=query&prop=categories&cllimit=max&titles="
                    + HttpUtility.UrlEncode(page) + "&format=xml&clshow=" + clshow;

                list.AddRange(ApiMakeList(url, list.Count));
            }

            return list;
        }

        #region ListMaker properties
        public override string DisplayText
        { get { return "Categories on page"; } }

        public override string UserInputTextBoxText
        { get { return "Pages"; } }

        public override bool UserInputTextBoxEnabled
        { get { return true; } }

        public override void Selected() { }
        #endregion
    }

    /// <summary>
    /// Gets a List of Categories on a page, excluding hidden categories
    /// </summary>
    public class CategoriesOnPageNoHiddenListProvider : CategoriesOnPageListProvider
    {
        public CategoriesOnPageNoHiddenListProvider()
        {
            clshow = "!hidden";
        }

        public override string DisplayText
        { get { return base.DisplayText + " (no hidden cats)"; } }
    }

    /// <summary>
    /// Gets a List of only hidden Categories on a page
    /// </summary>
    public class CategoriesOnPageOnlyHiddenListProvider : CategoriesOnPageListProvider
    {
        public CategoriesOnPageOnlyHiddenListProvider()
        {
            clshow = "hidden";
        }

        public override string DisplayText
        { get { return base.DisplayText + " (only hidden cats)"; } }
    }

    /// <summary>
    /// Gets a list of pages which link to the Named Pages
    /// </summary>
    public class WhatLinksHereListProvider : ApiListProviderBase, ISpecialPageProvider
    {
        public WhatLinksHereListProvider()
        { }

        public WhatLinksHereListProvider(int limit)
        {
            Limit = limit;
        }

        #region Tags: <backlinks>/<bl>
        static readonly List<string> pe = new List<string>(new[] { "bl" });
        protected override ICollection<string> PageElements
        {
            get { return pe; }
        }

        static readonly List<string> ac = new List<string>(new[] { "backlinks" });
        protected override ICollection<string> Actions
        {
            get { return ac; }
        }
        #endregion

        protected bool IncludeRedirects;

        public List<Article> MakeList(int Namespace, params string[] searchCriteria)
        {
            return MakeList(Namespace.ToString(), searchCriteria);
        }

        public override List<Article> MakeList(params string[] searchCriteria)
        {
            return MakeList(Namespace.Article, searchCriteria);
        }

        protected List<Article> MakeList(string Namespace, params string[] searchCriteria)
        {
            searchCriteria = Tools.FirstToUpperAndRemoveHashOnArray(searchCriteria);

            List<Article> list = new List<Article>();

            foreach (string page in searchCriteria)
            {
                string url = Variables.URLApi + "?action=query&list=backlinks&bltitle="
                    + HttpUtility.UrlEncode(page) + "&format=xml&bllimit=max&blnamespace=" + Namespace;

                if (IncludeRedirects)
                    url += "&blredirect";

                list.AddRange(ApiMakeList(url, list.Count));
            }
            return list;
        }

        #region ListMaker properties
        public override string DisplayText
        { get { return "What links here"; } }

        public override string UserInputTextBoxText
        { get { return "What links to:"; } }

        public override bool UserInputTextBoxEnabled
        { get { return true; } }

        public override void Selected() { }
        #endregion

        #region ISpecialPageProvider Members

        public bool PagesNeeded
        {
            get { return false; }
        }

        public bool NamespacesEnabled
        {
            get { return true; }
        }

        #endregion
    }

    /// <summary>
    /// Gets a list of pages (all ns's) from which link to the Named Pages
    /// </summary>
    public class WhatLinksHereAllNSListProvider : WhatLinksHereListProvider
    {
        public override List<Article> MakeList(params string[] searchCriteria)
        {
            return MakeList("", searchCriteria);
        }

        #region ListMaker properties
        public override string DisplayText
        { get { return "What links here (all NS)"; } }
        #endregion
    }

    /// <summary>
    /// Gets a list of pages (all ns's) from which link to the Named Pages
    /// </summary>
    public class WhatLinksHereAllNSIncludingRedirectsListProvider : WhatLinksHereAllNSListProvider
    {
        public WhatLinksHereAllNSIncludingRedirectsListProvider()
        {
            IncludeRedirects = true;
        }

        public override string DisplayText
        { get { return base.DisplayText + " (inc. Redirects)"; } }
    }

    /// <summary>
    /// Gets a list of pages which link to the Named Pages (including what links to the redirects)
    /// </summary>
    public class WhatLinksHereIncludingRedirectsListProvider : WhatLinksHereListProvider
    {
        public WhatLinksHereIncludingRedirectsListProvider()
        {
            IncludeRedirects = true;
        }

        public override string DisplayText
        { get { return base.DisplayText + " (inc. Redirects)"; } }
    }

    /// <summary>
    /// Gets a list of pages which transclude the Named Pages
    /// </summary>
    public class WhatTranscludesPageListProvider : ApiListProviderBase, ISpecialPageProvider
    {
        #region Tags: <embeddedin>/<ei>
        static readonly List<string> pe = new List<string>(new[] { "ei" });
        protected override ICollection<string> PageElements
        {
            get { return pe; }
        }

        static readonly List<string> ac = new List<string>(new[] { "embeddedin" });
        protected override ICollection<string> Actions
        {
            get { return ac; }
        }
        #endregion

        public override List<Article> MakeList(params string[] searchCriteria)
        {
            return MakeList(Namespace.Article, searchCriteria);
        }

        public virtual List<Article> MakeList(int Namespace, params string[] searchCriteria)
        {
            return MakeList(Namespace.ToString(), searchCriteria);
        }

        protected List<Article> MakeList(string Namespace, params string[] searchCriteria)
        {
            List<Article> list = new List<Article>();

            foreach (string page in searchCriteria)
            {
                string url = Variables.URLApi + "?action=query&list=embeddedin&eititle="
                    + HttpUtility.UrlEncode(page) + "&eilimit=max&format=xml&einamespace=" + Namespace;

                list.AddRange(ApiMakeList(url, list.Count));
            }

            return list;
        }

        #region ListMaker properties
        public override string DisplayText
        { get { return "What transcludes page"; } }

        public override string UserInputTextBoxText
        { get { return "What embeds:"; } }

        public override bool UserInputTextBoxEnabled
        { get { return true; } }

        public override void Selected()
        { }
        #endregion

        public virtual bool PagesNeeded
        { get { return true; } }

        public bool NamespacesEnabled
        { get { return true; } }
    }

    /// <summary>
    /// Gets a list of pages (all ns's) which transclude the Named Pages
    /// </summary>
    public class WhatTranscludesPageAllNSListProvider : WhatTranscludesPageListProvider
    {
        public override List<Article> MakeList(params string[] searchCriteria)
        {
            return MakeList("", searchCriteria);
        }

        public override string DisplayText
        { get { return "What transcludes page (all NS)"; } }
    }

    /// <summary>
    /// Gets a list of all links on the Named Pages
    /// </summary>
    public class LinksOnPageListProvider : ApiListProviderBase
    {
        #region Tags: <links>/<pl>
        static readonly List<string> pe = new List<string>(new[] { "pl" });
        protected override ICollection<string> PageElements
        {
            get { return pe; }
        }

        static readonly List<string> ac = new List<string>(new[] { "links" });
        protected override ICollection<string> Actions
        {
            get { return ac; }
        }
        #endregion

        public override List<Article> MakeList(params string[] searchCriteria)
        {
            searchCriteria = Tools.FirstToUpperAndRemoveHashOnArray(searchCriteria);

            List<Article> list = new List<Article>();

            foreach (string page in searchCriteria)
            {
                string url = Variables.URLApi + "?action=query&prop=links&titles="
                    + HttpUtility.UrlEncode(page) + "&pllimit=max&format=xml";

                list.AddRange(ApiMakeList(url, list.Count));
            }

            return list;
        }

        #region ListMaker properties
        public override string DisplayText
        { get { return "Links on page"; } }

        public override string UserInputTextBoxText
        { get { return "Links on:"; } }

        public override bool UserInputTextBoxEnabled
        { get { return true; } }

        public override void Selected() { }
        #endregion
    }

    /// <summary>
    /// Gets a list of all non-redlink links on the Named Pages
    /// </summary>
    public class LinksOnPageExcludingRedLinksListProvider : ApiListProviderBase
    {
        public LinksOnPageExcludingRedLinksListProvider()
        {
            Limit = 5000; //Cant imagine a page having more than 5000 links...
        }

        #region Tags: <pages>/<page>
        static readonly List<string> pe = new List<string>(new[] { "page" });
        protected override ICollection<string> PageElements
        {
            get { return pe; }
        }

        static readonly List<string> ac = new List<string>(new[] { "pages" });
        protected override ICollection<string> Actions
        {
            get { return ac; }
        }
        #endregion

        public override List<Article> MakeList(params string[] searchCriteria)
        {
            searchCriteria = Tools.FirstToUpperAndRemoveHashOnArray(searchCriteria);

            List<Article> list = new List<Article>();

            foreach (string page in searchCriteria)
            {
                string url = Variables.URLApi + "?action=query&generator=links&titles="
                             + HttpUtility.UrlEncode(page) + "&gpllimit=max&format=xml";

                list.AddRange(ApiMakeList(url, list.Count));
            }

            return list;
        }

        protected override bool EvaluateXmlElement(XmlTextReader xml)
        {
            return !xml.MoveToAttribute("missing");
        }

        public override string DisplayText
        { get { return "Links on page (no redlinks)"; } }

        public override string UserInputTextBoxText
        { get { return "Links on"; } }

        public override bool UserInputTextBoxEnabled
        { get { return true; } }

        public override void Selected() { }
    }

    /// <summary>
    /// Gets a list of all Images on the Named Pages
    /// </summary>
    public class ImagesOnPageListProvider : ApiListProviderBase
    {
        #region Tags: <images>/<im>
        static readonly List<string> pe = new List<string>(new[] { "im" });
        protected override ICollection<string> PageElements
        {
            get { return pe; }
        }

        static readonly List<string> ac = new List<string>(new[] { "images" });
        protected override ICollection<string> Actions
        {
            get { return ac; }
        }
        #endregion

        public override List<Article> MakeList(params string[] searchCriteria)
        {
            searchCriteria = Tools.FirstToUpperAndRemoveHashOnArray(searchCriteria);

            List<Article> list = new List<Article>();

            foreach (string page in searchCriteria)
            {
                string url = Variables.URLApi + "?action=query&prop=images&titles="
                    + HttpUtility.UrlEncode(page) + "&imlimit=max&format=xml";

                list.AddRange(ApiMakeList(url, list.Count));
            }
            return list;
        }

        #region ListMaker properties
        public override string DisplayText
        { get { return "Images on page"; } }

        public override string UserInputTextBoxText
        { get { return "Images on:"; } }

        public override bool UserInputTextBoxEnabled
        { get { return true; } }

        public override void Selected() { }
        #endregion
    }

    /// <summary>
    /// Gets a list of all the transclusions on the Named Pages
    /// </summary>
    public class TransclusionsOnPageListProvider : ApiListProviderBase
    {
        #region Tags: <templates>/<tl>
        static readonly List<string> pe = new List<string>(new[] { "tl" });
        protected override ICollection<string> PageElements
        {
            get { return pe; }
        }

        static readonly List<string> ac = new List<string>(new[] { "templates" });
        protected override ICollection<string> Actions
        {
            get { return ac; }
        }
        #endregion

        public override List<Article> MakeList(params string[] searchCriteria)
        {
            searchCriteria = Tools.FirstToUpperAndRemoveHashOnArray(searchCriteria);

            List<Article> list = new List<Article>();

            foreach (string page in searchCriteria)
            {
                string url = Variables.URLApi + "?action=query&prop=templates&titles="
                    + HttpUtility.UrlEncode(page) + "&tllimit=max&format=xml";

                list.AddRange(ApiMakeList(url, list.Count));
            }
            return list;
        }

        #region ListMaker properties
        public override string DisplayText
        { get { return "Transclusions on page"; } }

        public override string UserInputTextBoxText
        { get { return "Transclusions on:"; } }

        public override bool UserInputTextBoxEnabled
        { get { return true; } }

        public override void Selected() { }
        #endregion
    }

    /// <summary>
    /// Gets a list of google results based on the named pages
    /// </summary>
    public class GoogleSearchListProvider : IListProvider
    {
        private static readonly Regex regexGoogle = new Regex("href\\s*=\\s*(?:\"(?<1>[^\"]*)\"|(?<1>\\S+) class=l)",
                                                              RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public List<Article> MakeList(params string[] searchCriteria)
        {
            List<Article> list = new List<Article>();

            foreach (string g in searchCriteria)
            {
                int intStart = 0;
                string google = HttpUtility.UrlEncode(g);
                string url = "http://www.google.com/search?q=" + google + "+site:" + Variables.URL +
                             "&num=100&hl=en&lr=&start=0&sa=N&filter=0";

                do
                {
                    string googleText = Tools.GetHTML(url, Encoding.Default);

                    //Find each match to the pattern
                    foreach (Match m in regexGoogle.Matches(googleText))
                    {
                        string title = Tools.GetTitleFromURL(m.Groups[1].Value);

                        if (!string.IsNullOrEmpty(title))
                            list.Add(new Article(title));
                    }

                    if (!googleText.Contains("img src=\"nav_next.gif\""))
                        break;

                    intStart += 100;
                    url = "http://www.google.com/search?q=" + google + "+site:" + Variables.URL +
                          "&num=100&hl=en&lr=&start=" + intStart + "&sa=N&filter=0";

                } while (true);
            }
            return Tools.FilterSomeArticles(list);
        }

        #region ListMaker properties
        public string DisplayText
        { get { return "Google search"; } }

        public string UserInputTextBoxText
        { get { return "Google search:"; } }

        public bool UserInputTextBoxEnabled
        { get { return true; } }

        public void Selected() { }

        public bool RunOnSeparateThread
        { get { return true; } }
        #endregion
    }

    /// <summary>
    /// Gets the user contribs of the Named Users
    /// </summary>
    public class UserContribsListProvider : ApiListProviderBase
    {
        #region Tags: <usercontribs>/<item>
        static readonly List<string> pe = new List<string>(new[] { "item" });
        protected override ICollection<string> PageElements
        {
            get { return pe; }
        }

        static readonly List<string> ac = new List<string>(new[] { "usercontribs" });
        protected override ICollection<string> Actions
        {
            get { return ac; }
        }
        #endregion

        protected string uclimit = "max";

        public override List<Article> MakeList(params string[] searchCriteria)
        {
            searchCriteria = Tools.FirstToUpperAndRemoveHashOnArray(searchCriteria);

            List<Article> list = new List<Article>();

            foreach (string page in searchCriteria)
            {
                string url = Variables.URLApi + "?action=query&list=usercontribs&ucuser=" +
                    Tools.WikiEncode(
                    Regex.Replace(page, Variables.NamespacesCaseInsensitive[Namespace.Category], ""))
                    + "&uclimit=" + uclimit + "&format=xml";

                list.AddRange(ApiMakeList(url, list.Count));
            }

            return list;
        }

        #region ListMaker properties
        public override string DisplayText
        { get { return "User contribs"; } }

        public override string UserInputTextBoxText
        { get { return Variables.Namespaces[Namespace.User]; } }

        public override bool UserInputTextBoxEnabled
        { get { return true; } }

        public override void Selected() { }

        public override bool RunOnSeparateThread
        { get { return true; } }
        #endregion
    }

    /// <summary>
    /// Gets the specified number of user contribs for the Named Users
    /// </summary>
    public class UserContribUserDefinedNumberListProvider : UserContribsListProvider
    {
        public override List<Article> MakeList(params string[] searchCriteria)
        {
            Limit = Tools.GetNumberFromUser(true);
            if (Limit < 500)
                uclimit = Limit.ToString();

            return base.MakeList(searchCriteria);
        }

        #region ListMaker properties
        public override string DisplayText
        { get { return "User contribs (user defined number)"; } }
        #endregion
    }

    /// <summary>
    /// Gets a list of pages which link to the Named Images
    /// </summary>
    public class ImageFileLinksListProvider : ApiListProviderBase
    {
        #region Tags: <imageusage>/<iu>
        static readonly List<string> pe = new List<string>(new[] { "iu" });
        protected override ICollection<string> PageElements
        {
            get { return pe; }
        }

        static readonly List<string> ac = new List<string>(new[] { "imageusage" });
        protected override ICollection<string> Actions
        {
            get { return ac; }
        }
        #endregion

        public override List<Article> MakeList(params string[] searchCriteria)
        {
            searchCriteria = Tools.FirstToUpperAndRemoveHashOnArray(searchCriteria);

            List<Article> list = new List<Article>();

            foreach (string page in searchCriteria)
            {
                string image = Regex.Replace(page, "^" + Variables.Namespaces[Namespace.File],
                    "", RegexOptions.IgnoreCase);
                image = HttpUtility.UrlEncode(image);

                string url = Variables.URLApi + "?action=query&list=imageusage&iutitle=Image:"
                    + image + "&iulimit=max&format=xml";

                list.AddRange(ApiMakeList(url, list.Count));
            }
            return list;
        }

        #region ListMaker properties
        public override string DisplayText
        { get { return "Image file links"; } }

        public override string UserInputTextBoxText
        { get { return Variables.Namespaces[Namespace.File]; } }

        public override bool UserInputTextBoxEnabled
        { get { return true; } }

        public override void Selected() { }
        #endregion
    }

    /// <summary>
    /// Gets a list of pages which are returned from a wiki search of the Named Pages
    /// </summary>
    public class WikiSearchListProvider : ApiListProviderBase
    {
        protected string srwhat = "text";

        #region Tags: <search>/<p>
        static readonly List<string> pe = new List<string>(new[] { "p" });
        protected override ICollection<string> PageElements
        {
            get { return pe; }
        }

        static readonly List<string> ac = new List<string>(new[] { "search" });
        protected override ICollection<string> Actions
        {
            get { return ac; }
        }
        #endregion

        public WikiSearchListProvider()
        {
            Limit = 1000; // slow query
        }

        public override List<Article> MakeList(params string[] searchCriteria)
        {
            List<Article> list = new List<Article>();

            StringBuilder nsStringBuilder = new StringBuilder("&srnamespace=0|");

            // explicitly add available namespaces to search options
            foreach (int k in Variables.Namespaces.Keys)
            {
                if (k <= 0) continue;
                nsStringBuilder.Append(k + "|");
            }

            nsStringBuilder.Remove((nsStringBuilder.Length - 1), 1);
            string ns = nsStringBuilder.ToString();

            foreach (string page in searchCriteria)
            {
                string url = Variables.URLApi + "?action=query&list=search&srwhat=" + srwhat + "&srsearch="
                    + page + "&srlimit=max&format=xml" + ns;

                list.AddRange(ApiMakeList(url, list.Count));
            }
            return list;
        }

        #region ListMaker properties
        public override string DisplayText
        { get { return "Wiki search (text)"; } }

        public override string UserInputTextBoxText
        { get { return "Wiki search:"; } }

        public override bool UserInputTextBoxEnabled
        { get { return true; } }

        public override void Selected() { }
        #endregion
    }

    /// <summary>
    /// Gets a list of pages which are returned from a title wiki search of the Named Pages
    /// </summary>
    public class WikiTitleSearchListProvider : WikiSearchListProvider
    {
        public WikiTitleSearchListProvider()
        {
            srwhat = "title";
        }

        public override string DisplayText
        { get { return "Wiki search (title)"; } }
    }

    /// <summary>
    /// Gets a list of pages which redirect to the Named Pages
    /// </summary>
    public class RedirectsListProvider : ApiListProviderBase
    {
        #region Tags: <backlinks>/<bl>
        static readonly List<string> pe = new List<string>(new[] { "bl" });
        protected override ICollection<string> PageElements
        {
            get { return pe; }
        }

        static readonly List<string> ac = new List<string>(new[] { "backlinks" });
        protected override ICollection<string> Actions
        {
            get { return ac; }
        }
        #endregion

        public override List<Article> MakeList(params string[] searchCriteria)
        {
            searchCriteria = Tools.FirstToUpperAndRemoveHashOnArray(searchCriteria);

            List<Article> list = new List<Article>();

            foreach (string page in searchCriteria)
            {
                string url = Variables.URLApi + "?action=query&list=backlinks&bltitle="
                    + HttpUtility.UrlEncode(page) + "&bllimit=max&blfilterredir=redirects&format=xml";

                list.AddRange(ApiMakeList(url, list.Count));
            }
            return list;
        }

        #region ListMaker properties
        public override string DisplayText
        { get { return UserInputTextBoxText; } }

        public override string UserInputTextBoxText
        { get { return "Redirects to"; } }

        public override bool UserInputTextBoxEnabled
        { get { return true; } }

        public override void Selected() { }
        #endregion
    }

    /// <summary>
    /// Gets all the pages from the current user's watchlist
    /// </summary>
    /// TODO:Change for list=watchlistraw when available
    public class MyWatchlistListProvider : IListProvider
    {
        public List<Article> MakeList(params string[] searchCriteria)
        {
            Browser.WebControl webbrowser = new Browser.WebControl();
            webbrowser.ScriptErrorsSuppressed = true;
            webbrowser.Navigate(Variables.URLIndex + "?title=Special:Watchlist&action=raw");
            webbrowser.Wait();

            List<Article> list = new List<Article>();

            if (webbrowser.Document == null)
                return list;

            HtmlElement textarea = webbrowser.Document.GetElementById("titles");
            string html;
            if (textarea == null || (html = textarea.InnerText) == null) return list;

            try
            {
                string[] splitter = { "\r\n" };
                foreach (string entry in html.Split(splitter, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (entry.Length > 0)
                        list.Add(new Article(entry));
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex);
            }

            return list;
        }

        #region ListMaker properties
        public string DisplayText
        { get { return "My Watchlist"; } }

        public string UserInputTextBoxText
        { get { return ""; } }

        public bool UserInputTextBoxEnabled
        { get { return false; } }

        public void Selected() { }

        public bool RunOnSeparateThread
        { get { return true; } }
        #endregion
    }

    ///// <summary>
    ///// Gets all the pages from the current user's watchlist
    ///// </summary>
    //public class MyWatchListApiListProvider : ApiListProviderBase
    //{
    //    #region Tags: <watchlistraw>/<wr>
    //    static readonly List<string> pe = new List<string>(new[] { "wr" });
    //    protected override ICollection<string> PageElements
    //    {
    //        get { return pe; }
    //    }

    //    static readonly List<string> ac = new List<string>(new[] { "watchlistraw" });
    //    protected override ICollection<string> Actions
    //    {
    //        get { return ac; }
    //    }
    //    #endregion

    //    public override List<Article> MakeList(string[] searchCriteria)
    //    {
    //        string url = Variables.URLApi + "?action=query&list=watchlistraw&wrlimit=max&format=xml";

    //        return ApiMakeList(url, 0);
    //    }

    //    #region ListMaker properties
    //    public override string DisplayText
    //    { get { return "My Watchlist (Api Raw)"; } }

    //    public override string UserInputTextBoxText
    //    { get { return ""; } }

    //    public override bool UserInputTextBoxEnabled
    //    { get { return false; } }

    //    public override void Selected() { }
    //    #endregion
    //}

    /// <summary>
    /// Runs the Database Scanner
    /// </summary>
    public class DatabaseScannerListProvider : IListProvider
    {
        private readonly ListMaker listMaker;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="lm">ListMaker for DBScanner to add articles to</param>
        public DatabaseScannerListProvider(ListMaker lm)
        {
            listMaker = lm;
        }

        public List<Article> MakeList(params string[] searchCriteria)
        {
            new DBScanner.DatabaseScanner(listMaker).Show();
            return null;
        }

        #region ListMaker properties
        public string DisplayText
        { get { return "Database dump"; } }

        public string UserInputTextBoxText
        { get { return ""; } }

        public bool UserInputTextBoxEnabled
        { get { return false; } }

        public void Selected() { }

        public bool RunOnSeparateThread
        { get { return false; } }
        #endregion
    }

    /// <summary>
    /// Gets 100 random articles
    /// </summary>
    public class RandomPagesSpecialPageProvider : ApiListProviderBase, ISpecialPageProvider
    {
        protected string extra;
        public RandomPagesSpecialPageProvider()
        {
            Limit = 100;
        }

        #region Tags: <random>/<page>
        static readonly List<string> pe = new List<string>(new[] { "page" });
        protected override ICollection<string> PageElements
        {
            get { return pe; }
        }

        static readonly List<string> ac = new List<string>(new[] { "random" });
        protected override ICollection<string> Actions
        {
            get { return ac; }
        }
        #endregion

        public override List<Article> MakeList(params string[] searchCriteria)
        {
            return MakeList(Namespace.Article, searchCriteria);
        }

        public List<Article> MakeList(int Namespace, string[] searchCriteria)
        {
            List<Article> list = new List<Article>();

            string url = Variables.URLApi + "?action=query&list=random&rnnamespace=" + Namespace +
                         "&rnlimit=max&format=xml" + extra;

            list.AddRange(ApiMakeList(url, list.Count));
            return list;
        }

        #region ListMaker properties
        public override string DisplayText
        { get { return "Random Pages"; } }

        public override string UserInputTextBoxText
        { get { return ""; } }

        public override bool UserInputTextBoxEnabled
        { get { return false; } }

        public override void Selected() { }
        #endregion

        public bool PagesNeeded
        { get { return false; } }

        public bool NamespacesEnabled
        { get { return true; } }
    }

    /// <summary>
    /// Gets 100 random redirects
    /// </summary>
    public class RandomRedirectsSpecialPageProvider : RandomPagesSpecialPageProvider
    {
        public RandomRedirectsSpecialPageProvider()
        {
            extra = "&rnredirect";
        }

        public override string DisplayText
        { get { return "Random Redirects"; } }
    }

    /// <summary>
    /// Returns a list of "all pages"
    /// </summary>
    public class AllPagesSpecialPageProvider : ApiListProviderBase, ISpecialPageProvider
    {
        #region Tags: <allpages>/<p>
        static readonly List<string> pe = new List<string>(new[] { "p" });
        protected override ICollection<string> PageElements
        {
            get { return pe; }
        }

        static readonly List<string> ac = new List<string>(new[] { "allpages" });
        protected override ICollection<string> Actions
        {
            get { return ac; }
        }
        #endregion

        protected string from = "apfrom", extra;

        #region ISpecialPageProvider Members

        public override List<Article> MakeList(params string[] searchCriteria)
        {
            return MakeList(Namespace.Article, searchCriteria);
        }

        public virtual List<Article> MakeList(int Namespace, params string[] searchCriteria)
        {
            List<Article> list = new List<Article>();

            foreach (string page in searchCriteria)
            {
                string url = Variables.URLApi + "?action=query&list=allpages&" + from + "=" +
                             HttpUtility.UrlEncode(page) + "&apnamespace=" + Namespace + "&aplimit=max&format=xml" + extra;

                list.AddRange(ApiMakeList(url, list.Count));
            }
            return list;
        }

        public override string UserInputTextBoxText
        { get { return "All Pages"; } }

        public virtual bool PagesNeeded
        { get { return false; } }
        #endregion

        public override bool UserInputTextBoxEnabled
        { get { return true; } }

        public override void Selected() { }

        public override string DisplayText
        { get { return UserInputTextBoxText; } }

        public virtual bool NamespacesEnabled
        { get { return true; } }
    }

    /// <summary>
    /// Returns a list of "all categories"
    /// </summary>
    public class AllCategoriesSpecialPageProvider : AllPagesSpecialPageProvider
    {
        public override List<Article> MakeList(params string[] searchCriteria)
        {
            return MakeList(Namespace.Category, searchCriteria);
        }

        public override string DisplayText
        { get { return "All Categories"; } }

        public override string UserInputTextBoxText
        { get { return "Start Cat."; } }

        public override bool NamespacesEnabled
        { get { return false; } }
    }

    /// <summary>
    /// Returns a list of "all files"
    /// </summary>
    public class AllFilesSpecialPageProvider : AllPagesSpecialPageProvider
    {
        public override List<Article> MakeList(params string[] searchCriteria)
        {
            return MakeList(Namespace.File, searchCriteria);
        }

        public override string DisplayText
        { get { return "All Files"; } }

        public override string UserInputTextBoxText
        { get { return "Start File"; } }

        public override bool NamespacesEnabled
        { get { return false; } }
    }

    /// <summary>
    /// Returns a list of "all redirects"
    /// </summary>
    public class AllRedirectsSpecialPageProvider : AllPagesSpecialPageProvider
    {
        public AllRedirectsSpecialPageProvider()
        {
            extra = "&apfilterredir=redirects";
        }

        public override string DisplayText
        { get { return "All Redirect"; } }

        public override string UserInputTextBoxText
        { get { return "Start Redirect"; } }
    }

    /// <summary>
    /// Returns a list of protected pages
    /// </summary>
    public class ProtectedPagesSpecialPageProvider : AllPagesSpecialPageProvider
    {
        private readonly ProtectionLevel protlevel = new ProtectionLevel();

        public override List<Article> MakeList(params string[] searchCriteria)
        {
            return MakeList(Namespace.Article, searchCriteria);
        }

        public override List<Article> MakeList(int Namespace, params string[] searchCriteria)
        {
            protlevel.ShowDialog();
            extra = "&apprtype=" + protlevel.Type + "&apprlevel=" + protlevel.Level;
            return base.MakeList(Namespace, searchCriteria);
        }

        public override string DisplayText
        { get { return "Protected Pages"; } }

        public override string UserInputTextBoxText
        { get { return "Pages"; } }
    }

    /// <summary>
    /// Returns a list of pages without language links
    /// </summary>
    public class PagesWithoutLanguageLinksSpecialPageProvider : AllPagesSpecialPageProvider
    {
        public PagesWithoutLanguageLinksSpecialPageProvider()
        {
            extra = "&apfilterlanglinks=withoutlanglinks";
        }

        public override string DisplayText
        { get { return "Pages without Language Links"; } }

        public override string UserInputTextBoxText
        { get { return "Pages"; } }
    }

    /// <summary>
    /// Returns a list of subpages for the specified page
    /// </summary>
    public class PrefixIndexSpecialPageProvider : AllPagesSpecialPageProvider
    {
        public PrefixIndexSpecialPageProvider()
        {
            from = "apprefix";
        }

        public override string DisplayText
        { get { return "All Pages with prefix (Prefixindex)"; } }

        public override bool PagesNeeded
        { get { return true; } }
    }

    /// <summary>
    /// Returns a list of recent changes, by default in the 0 namespace
    /// </summary>
    public class RecentChangesSpecialPageProvider : ApiListProviderBase, ISpecialPageProvider
    {
        #region Tags: <recentchanges>/<rc>
        static readonly List<string> pe = new List<string>(new[] { "rc" });
        protected override ICollection<string> PageElements
        {
            get { return pe; }
        }

        static readonly List<string> ac = new List<string>(new[] { "recentchanges" });
        protected override ICollection<string> Actions
        {
            get { return ac; }
        }
        #endregion

        #region ISpecialPageProvider Members
        public override List<Article> MakeList(params string[] searchCriteria)
        {
            return MakeList(Namespace.Article, searchCriteria);
        }

        public List<Article> MakeList(int Namespace, params string[] searchCriteria)
        {
            List<Article> list = new List<Article>();

            foreach (string page in searchCriteria)
            {
                string url = Variables.URLApi + "?action=query&list=recentchanges&rctitles=" + HttpUtility.UrlEncode(page) + "&rcnamespace=" + Namespace + "&rclimit=max&format=xml";

                list.AddRange(ApiMakeList(url, list.Count));
            }
            return list;
        }

        public override string DisplayText
        { get { return "Recent Changes"; } }

        public bool PagesNeeded
        { get { return false; } }
        #endregion

        public override string UserInputTextBoxText
        { get { return DisplayText; } }

        public override bool UserInputTextBoxEnabled
        { get { return false; } }

        public override void Selected() { }

        public bool NamespacesEnabled
        { get { return true; } }
    }

    /// <summary>
    /// Returns a list of new pages, by default in the 0 namespace
    /// </summary>
    public class NewPagesListProvider : ApiListProviderBase, ISpecialPageProvider
    {
        public NewPagesListProvider()
        {
            Limit = 100; // slow query
        }

        #region Tags: <recentchanges>/<rc>
        static readonly List<string> pe = new List<string>(new[] { "rc" });
        protected override ICollection<string> PageElements
        {
            get { return pe; }
        }

        static readonly List<string> ac = new List<string>(new[] { "recentchanges" });
        protected override ICollection<string> Actions
        {
            get { return ac; }
        }
        #endregion

        public override List<Article> MakeList(params string[] searchCriteria)
        {
            return MakeList(Namespace.Article, searchCriteria);
        }

        public List<Article> MakeList(int Namespace, params string[] searchCriteria)
        {
            List<Article> list = new List<Article>();

            string url = Variables.URLApi + "?action=query&list=recentchanges"
                + "&rclimit=100&rctype=new&rcshow=!redirect&rcnamespace=" + Namespace + "&format=xml";

            list.AddRange(ApiMakeList(url, list.Count));

            return list;
        }

        #region ListMaker properties
        public override string DisplayText
        { get { return "New pages"; } }

        public override string UserInputTextBoxText
        { get { return ""; } }

        public override bool UserInputTextBoxEnabled
        { get { return false; } }

        public override void Selected() { }
        #endregion

        public bool PagesNeeded
        { get { return false; } }

        public bool NamespacesEnabled
        { get { return true; } }
    }

    /// <summary>
    /// Returns a list of pages that contain the specified URL
    /// </summary>
    public class LinkSearchSpecialPageProvider : ApiListProviderBase, ISpecialPageProvider
    {
        #region Tags: <exturlusage>/<eu>
        static readonly List<string> pe = new List<string>(new[] { "eu" });
        protected override ICollection<string> PageElements
        {
            get { return pe; }
        }

        static readonly List<string> ac = new List<string>(new[] { "exturlusage" });
        protected override ICollection<string> Actions
        {
            get { return ac; }
        }
        #endregion

        public override List<Article> MakeList(params string[] searchCriteria)
        {
            return MakeList(Namespace.Article, searchCriteria);
        }

        public List<Article> MakeList(int Namespace, params string[] searchCriteria)
        {
            List<Article> list = new List<Article>();

            foreach (string page in searchCriteria)
            {
                string url = Variables.URLApi + "?action=query&list=exturlusage&euquery=" +
                             HttpUtility.UrlEncode(page.Replace("http://", "")) + "&eunamespace=" + Namespace +
                             "&eulimit=max&format=xml";

                list.AddRange(ApiMakeList(url, list.Count));
            }

            return list;
        }

        #region ListMaker properties
        public override string DisplayText
        { get { return "Link search"; } }

        public override string UserInputTextBoxText
        { get { return "URL"; } }

        public override bool UserInputTextBoxEnabled
        { get { return true; } }

        public override void Selected() { }
        #endregion

        public bool PagesNeeded
        { get { return true; } }

        public bool NamespacesEnabled
        { get { return true; } }
    }

    /// <summary>
    /// Returns a list of disambiguation pages
    /// </summary>
    public class DisambiguationPagesSpecialPageProvider : WhatTranscludesPageListProvider
    {
        public override List<Article> MakeList(params string[] searchCriteria)
        {
            return base.MakeList(Namespace.Article, new[] { "Template:Disambiguation" });
        }

        public override List<Article> MakeList(int Namespace, params string[] searchCriteria)
        {
            return base.MakeList(Namespace, new[] { "Template:Disambiguation" });
        }

        public override string DisplayText
        { get { return "Disambiguation Pages"; } }

        public override string UserInputTextBoxText
        { get { return ""; } }

        public override bool UserInputTextBoxEnabled
        { get { return false; } }

        public override bool PagesNeeded
        { get { return false; } }
    }

    /// <summary>
    /// Returns a list of new files
    /// </summary>
    public class GalleryNewFilesSpecialPageProvider : ApiListProviderBase, ISpecialPageProvider
    {
        #region Tags: <logevents>/<item>
        static readonly List<string> pe = new List<string>(new[] { "item" });
        protected override ICollection<string> PageElements
        {
            get { return pe; }
        }

        static readonly List<string> ac = new List<string>(new[] { "logevents" });
        protected override ICollection<string> Actions
        {
            get { return ac; }
        }
        #endregion

        public GalleryNewFilesSpecialPageProvider()
        {
            Limit = 1000; // slow query
        }

        public override List<Article> MakeList(params string[] searchCriteria)
        {
            List<Article> list = new List<Article>();

            string url = Variables.URLApi + "?action=query&list=logevents&letype=upload&lelimit=max&format=xml";

            list.AddRange(ApiMakeList(url, list.Count));

            return list;
        }

        public List<Article> MakeList(int Namespace, string[] searchCriteria)
        {
            return MakeList("");
        }

        #region ListMaker properties
        public override string DisplayText
        { get { return "New files"; } }

        public override string UserInputTextBoxText
        { get { return ""; } }

        public override bool UserInputTextBoxEnabled
        { get { return false; } }

        public override void Selected() { }
        #endregion

        public bool PagesNeeded
        { get { return false; } }

        public bool NamespacesEnabled
        { get { return false; } }
    }
}