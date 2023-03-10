/*
(C) 2007 Stephen Kennedy (Kingboyk) http://www.sdk-software.com/

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

/* Some of this is currently only suitable for enwiki. */

using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace WikiFunctions.TalkPages
{
    public enum DEFAULTSORT
    {
        NoChange,
        MoveToTop,
        MoveToBottom
    }

    internal class Processor
    {
        public string DefaultSortKey;
        public bool FoundDefaultSort, FoundSkipToTalk;

        // Match evaluators:
        public string DefaultSortMatchEvaluator(Match match)
        {
            FoundDefaultSort = true;
            if (match.Groups["key"].Captures.Count > 0)
                DefaultSortKey = match.Groups["key"].Captures[0].Value.Trim();
            return "";
        }

        public string SkipTOCMatchEvaluator(Match match)
        {
            FoundSkipToTalk = true;
            return "";
        }
    }

    public static class TalkPageFixes
    {
        /// <summary>
        /// Processes talk pages: moves any talk page header template, moves any default, adds a section heading if missing
        /// </summary>
        /// <param name="articleText">The talk page text</param>
        /// <param name="moveDefaultsort">The action to take over any defaultsort on the page</param>
        /// <returns>The updated talk page text</returns>
        public static bool ProcessTalkPage(ref string articleText, DEFAULTSORT moveDefaultsort)
        {
            Processor pr = new Processor();

            articleText = WikiRegexes.SkipTOCTemplateRegex.Replace(articleText,
                                                                   new MatchEvaluator(pr.SkipTOCMatchEvaluator), 1);

            articleText = WikiProjectBannerShell(articleText);

            // move talk page header to the top
            articleText = MoveTalkHeader(articleText);

            if (pr.FoundSkipToTalk)
                WriteHeaderTemplate("Skip to talk", ref articleText);

            if (moveDefaultsort != DEFAULTSORT.NoChange)
            {
                articleText = WikiRegexes.Defaultsort.Replace(articleText,
                                                              new MatchEvaluator(pr.DefaultSortMatchEvaluator), 1);
                if (pr.FoundDefaultSort)
                {
                    if (!string.IsNullOrEmpty(pr.DefaultSortKey))
                    {
                        articleText = SetDefaultSort(pr.DefaultSortKey, moveDefaultsort, articleText);
                    }
                }
            }

            articleText = AddMissingFirstCommentHeader(articleText);
            
            articleText = WPBiography(articleText);

            articleText = WPSongs(articleText);

            // remove redundant Template: in templates in zeroth section
            string zerothSection = WikiRegexes.ZerothSection.Match(articleText).Value;
            if(zerothSection.Length > 0)
                articleText = articleText.Replace(zerothSection, Parse.Parsers.RemoveTemplateNamespace(zerothSection));

            return pr.FoundSkipToTalk || pr.FoundDefaultSort;
        }

        public static string FormatDefaultSort(string articleText)
        {
            return WikiRegexes.Defaultsort.Replace(articleText, "{{DEFAULTSORT:${key}}}");
        }

        // Helper routines:
        private static string SetDefaultSort(string key, DEFAULTSORT location, string articleText)
        {
            switch (location)
            {
                case DEFAULTSORT.MoveToTop:
                    return "{{DEFAULTSORT:" + key + "}}\r\n" + articleText;

                case DEFAULTSORT.MoveToBottom:
                    return articleText + "\r\n{{DEFAULTSORT:" + key + "}}";
            }

            return "";
        }

        /// <summary>
        /// Writes the input template to the top of the input page; updates the input edit summary
        /// </summary>
        /// <param name="name">template name to write</param>
        /// <param name="articleText">article text to update</param>
        private static void WriteHeaderTemplate(string name, ref string articleText)
        {
            articleText = "{{" + name + "}}\r\n" + articleText;
        }

        /// <summary>
        /// Moves the {{talk header}} template to the top of the talk page
        /// </summary>
        /// <param name="articleText">the talk page text</param>
        /// <returns>the update talk page text</returns>
        private static string MoveTalkHeader(string articleText)
        {
            Match m = WikiRegexes.TalkHeaderTemplate.Match(articleText);

            if (m.Success && m.Index > 0)
            {
                // remove existing talk header – handle case where not at beginning of line
                articleText = articleText.Replace(m.Value, articleText.Contains("\r\n" + m.Value) ? "" : "\r\n");

                // write existing talk header to top
                articleText = m.Value.TrimEnd() + "\r\n" + articleText.TrimStart();

                // ensure template is now named {{talk header}}
                articleText = articleText.Replace(m.Groups[1].Value, "Talk header");
            }

            return articleText;
        }

        private static readonly Regex FirstComment = new Regex(@"^ {0,4}[:\*\w'""](?<!_)", RegexOptions.Compiled | RegexOptions.Multiline);

        /// <summary>
        /// Adds a section 2 heading before the first comment if the talk page does not have one
        /// </summary>
        /// <param name="articleText">The talk page text</param>
        /// <returns>The updated article text</returns>
        private static string AddMissingFirstCommentHeader(string articleText)
        {
            // don't match on lines within templates
            string articleTextTemplatesSpaced = Tools.ReplaceWithSpaces(articleText, WikiRegexes.NestedTemplates.Matches(articleText));
            articleTextTemplatesSpaced = Tools.ReplaceWithSpaces(articleTextTemplatesSpaced, WikiRegexes.UnformattedText.Matches(articleTextTemplatesSpaced));

            if (FirstComment.IsMatch(articleTextTemplatesSpaced))
            {
                int firstCommentIndex = FirstComment.Match(articleTextTemplatesSpaced).Index;

                int firstLevelTwoHeading = WikiRegexes.HeadingLevelTwo.IsMatch(articleText) ? WikiRegexes.HeadingLevelTwo.Match(articleText).Index : 99999999;

                if (firstCommentIndex < firstLevelTwoHeading)
                {
                    // is there a heading level 3? If yes, change to level 2
                    string articletexttofirstcomment = articleText.Substring(0, firstCommentIndex);

                    articleText = WikiRegexes.HeadingLevelThree.IsMatch(articletexttofirstcomment) ? WikiRegexes.HeadingLevelThree.Replace(articleText, @"==$1==", 1) : articleText.Insert(firstCommentIndex, "\r\n==Untitled==\r\n");
                }
            }

            return articleText;
        }

        private static readonly List<string> BannerShellRedirects = new List<string>(new[] { "WikiProject Banners", "WikiProjectBanners", "WPBS", "WPB", "Wpb", "Wpbs" });
        private static readonly List<string> Nos = new List<string>(new[] { "blp", "blpo", "activepol", "collapsed" });
        private static readonly Regex BLPRegex = Tools.NestedTemplateRegex(new[] { "blp", "BLP", "Blpinfo" });
        private static readonly Regex BLPORegex = Tools.NestedTemplateRegex(new[] { "blpo", "BLPO", "BLP others" });
        private static readonly Regex ActivepolRegex = Tools.NestedTemplateRegex(new[] { "activepol", "active politician", "activepolitician" });
        private static readonly Regex WPBiographyR = Tools.NestedTemplateRegex(new[] { "WPBiography", "Wikiproject Biography", "WikiProject Biography", "WPBIO", "Bio" });
        private static readonly Regex WPSongsR = Tools.NestedTemplateRegex(new[] { "WikiProject Songs", "WikiProjectSongs", "WP Songs", "Song", "WPSongs", "Songs", "WikiProject Song" });
        private static readonly Regex SirRegex = Tools.NestedTemplateRegex(new[] { "sir", "Single infobox request" });
        
        /// <summary>
        /// Performs fixes to the WikiProjectBannerShells template:
        /// Add explicit call to first unnamed parameter 1= if missing/has no value
        /// Remove duplicate parameters
        /// Moves any other WikiProjects into WPBS
        /// </summary>
        /// <param name="articletext">The talk page text</param>
        /// <returns>The updated talk page text</returns>
        public static string WikiProjectBannerShell(string articletext)
        {
            if(!Variables.LangCode.Equals("en"))
                return articletext;
            
            articletext = AddWikiProjectBannerShell(articletext);
            
            if (!WikiRegexes.WikiProjectBannerShellTemplate.IsMatch(articletext))
                return articletext;

            // rename redirects
            foreach (string redirect in BannerShellRedirects)
                articletext = Tools.RenameTemplate(articletext, redirect, "WikiProjectBannerShell", false);

            foreach (Match m in WikiRegexes.WikiProjectBannerShellTemplate.Matches(articletext))
            {
                string newValue = m.Value;
                string arg1 = Tools.GetTemplateParameterValue(newValue, "1");
                
                // Add explicit call to first unnamed parameter 1= if missing/has no value
                if (arg1.Length == 0)
                {
                    int argCount = Tools.GetTemplateArgumentCount(newValue);

                    for (int arg = 1; arg <= argCount; arg++)
                    {
                        string argValue = Tools.GetTemplateArgument(newValue, arg);

                        if (argValue.StartsWith(@"{{"))
                        {
                            newValue = newValue.Insert(Tools.GetTemplateArgumentIndex(newValue, arg), "1=");
                            break;
                        }
                    }
                }
                
                // remove duplicate parameters
                newValue = Tools.RemoveDuplicateTemplateParameters(newValue);
                
                // refresh after cleanup
                arg1 = Tools.GetTemplateParameterValue(newValue, "1");

                // clean blp=no, blpo=no, activepol=no, collapsed=no
                foreach (string theNo in Nos)
                {
                    if (Tools.GetTemplateParameterValue(newValue, theNo).Equals("no"))
                        newValue = Tools.RemoveTemplateParameter(newValue, theNo);
                }

                // If {{blpo}} then add blpo=yes to WPBS and remove {{blpo}}
                Match blpom = BLPORegex.Match(articletext);
                if (blpom.Success)
                {
                    newValue = Tools.SetTemplateParameterValue(newValue, "blpo", "yes");
                    articletext = articletext.Replace(blpom.Value, "");
                }

                // If {{BLP}} then add blp=yes to WPBS and remove {{BLP}}
                Match blpm = BLPRegex.Match(articletext);
                if (blpm.Success)
                {
                    newValue = Tools.SetTemplateParameterValue(newValue, "blp", "yes");
                    articletext = articletext.Replace(blpm.Value, "");
                }

                // If {{activepol}} then add activepol=yes to WPBS and remove {{activepol}}
                Match activepolm = ActivepolRegex.Match(articletext);
                if (activepolm.Success)
                {
                    newValue = Tools.SetTemplateParameterValue(newValue, "activepol", "yes");
                    articletext = articletext.Replace(activepolm.Value, "");
                }
                                
                // merge changes to article text
                if (!newValue.Equals(m.Value))
                    articletext = articletext.Replace(m.Value, newValue);
            }
            
            // Move WikiProjects into WPBS
            if(WikiRegexes.WikiProjectBannerShellTemplate.Matches(articletext).Count == 1)
            {
                string WPBS = WikiRegexes.WikiProjectBannerShellTemplate.Match(articletext).Value, newParams = "";
                
                foreach(Match m in WikiRegexes.NestedTemplates.Matches(articletext))
                {
                    if(Tools.GetTemplateName(m.Value).StartsWith("WikiProject ") && !WPBS.Contains(m.Value))
                    {
                        articletext = articletext.Replace(m.Value, "");
                        newParams += Tools.Newline(m.Value);
                    }
                }
                if(newParams.Length > 0)
                    articletext = articletext.Replace(WPBS, Tools.SetTemplateParameterValue(WPBS, "1", Tools.GetTemplateParameterValue(WPBS, "1") + newParams));
            }
            
            // check living, activepol, blpo flags against WPBiography
            foreach (Match m in WikiRegexes.WikiProjectBannerShellTemplate.Matches(articletext))
            {
                string newValue = m.Value;
                string arg1 = Tools.GetTemplateParameterValue(newValue, "1");
                
                Match m2 = WPBiographyR.Match(arg1);

                if (m2.Success)
                {
                    string WPBiographyCall = m2.Value;

                    string livingParam = Tools.GetTemplateParameterValue(WPBiographyCall, "living");
                    if (livingParam.Equals("yes"))
                        newValue = Tools.SetTemplateParameterValue(newValue, "blp", "yes");
                    else if (livingParam.Equals("no"))
                    {
                        if (Tools.GetTemplateParameterValue(newValue, "blp").Equals("yes"))
                            newValue = Tools.RemoveTemplateParameter(newValue, "blp");
                    }

                    string activepolParam = Tools.GetTemplateParameterValue(WPBiographyCall, "activepol");
                    if (activepolParam.Equals("yes"))
                        newValue = Tools.SetTemplateParameterValue(newValue, "activepol", "yes");
                    else if (activepolParam.Equals("no"))
                    {
                        if (Tools.GetTemplateParameterValue(newValue, "activepol").Equals("yes"))
                            newValue = Tools.RemoveTemplateParameter(newValue, "activepol");
                    }

                    if (Tools.GetTemplateParameterValue(WPBiographyCall, "blpo").Equals("yes"))
                        newValue = Tools.SetTemplateParameterValue(newValue, "blpo", "yes");
                }
                                
                // merge changes to article text
                if (!newValue.Equals(m.Value))
                    articletext = articletext.Replace(m.Value, newValue);
            }

            return articletext;
        }
        
        /// <summary>
        /// Moves WPBiography with living=yes above any WikiProject templates per Wikipedia:TPL#Talk_page_layout
        /// Does various fixes to WPBiography
        /// en-wiki only
        /// </summary>
        /// <param name="articletext">The talk page text</param>
        /// <returns>The updated talk page text</returns>
        public static string WPBiography(string articletext)
        {
            if(!Variables.LangCode.Equals("en"))
                return articletext;
            
            Match m = WPBiographyR.Match(articletext);
            
            // remove diacritics from listas
            if(m.Success)
            {
                string newvalue = m.Value;
                string listas = Tools.GetTemplateParameterValue(m.Value, "listas");
                
                newvalue = Tools.SetTemplateParameterValue(newvalue, "listas", Tools.RemoveDiacritics(listas));


                
                // If {{activepol}} then add living=yes, activepol=yes, politician-work-group=yes to WPBiography and remove {{activepol}}
                Match activepolm = ActivepolRegex.Match(articletext);
                if (activepolm.Success)
                {
                    newvalue = Tools.SetTemplateParameterValue(newvalue, "living", "yes");
                    newvalue = Tools.SetTemplateParameterValue(newvalue, "activepol", "yes");
                    newvalue = Tools.SetTemplateParameterValue(newvalue, "politician-work-group", "yes");
                    
                    articletext = ActivepolRegex.Replace(articletext, "");
                }
                
                // If {{BLP}} then add living=yes to WPBiography and remove {{BLP}}
                Match blpm = BLPRegex.Match(articletext);
                if (blpm.Success & !Tools.GetTemplateParameterValue(newvalue, "living").ToLower().StartsWith("n"))
                {
                    newvalue = Tools.SetTemplateParameterValue(newvalue, "living", "yes");
                    articletext = BLPRegex.Replace(articletext, "");
                }
                
                if(newvalue.Length > 0 && !m.Value.Equals(newvalue))
                    articletext = articletext.Replace(m.Value, newvalue);
            }

            // refresh
            m = WPBiographyR.Match(articletext);
            
            if(!m.Success || !Tools.GetTemplateParameterValue(m.Value, "living").ToLower().StartsWith("y"))
                return articletext;
            
            // remove {{blp}} if {{WPBiography|living=yes}}
            articletext = BLPRegex.Replace(articletext, "");
            
            // remove {{activepol}} if {{WPBiography|activepol=yes}}
            articletext = ActivepolRegex.Replace(articletext, "");
            
            // move above any other WikiProject
            if(!WikiRegexes.WikiProjectBannerShellTemplate.IsMatch(articletext))
            {
                foreach(Match n in WikiRegexes.NestedTemplates.Matches(articletext))
                {
                    if(n.Index < m.Index && Tools.GetTemplateName(n.Value).StartsWith("WikiProject "))
                    {
                        articletext = articletext.Replace(m.Value, "");
                        articletext = articletext.Insert(n.Index, m.Value + "\r\n");
                        break;
                    }
                }
            }
            
            return articletext;
        }
        
        
         /// <summary>
        /// Does various fixes to WPSongs
        /// en-wiki only
        /// </summary>
        /// <param name="articletext">The talk page text</param>
        /// <returns>The updated talk page text</returns>
        public static string WPSongs(string articletext)
        {
            if(!Variables.LangCode.Equals("en"))
                return articletext;
            
            Match m = WPSongsR.Match(articletext);
            
            if(m.Success)
            {
                string newvalue = m.Value;
                   
                // Remove needs-infobox=no
                    if (Tools.GetTemplateParameterValue(newvalue, "needs-infobox").Equals("no"))
                        newvalue = Tools.RemoveTemplateParameter(newvalue, "needs-infobox");
                // Remove importance. WPSongs doesn't do importance
                newvalue = Tools.RemoveTemplateParameter(newvalue, "importance");

                articletext = articletext.Replace(m.Value, newvalue);

                // If {{sir}} then add needs-infobox=yes to WPsongs and remove {{sir}}
                Match sirm = SirRegex.Match(articletext);
                if (sirm.Success)
                {
                    newvalue = Tools.SetTemplateParameterValue(newvalue, "needs-infobox", "yes");
                    articletext = articletext.Replace(m.Value, newvalue);
                    articletext = SirRegex.Replace(articletext, "");
                }
                
            }

            return articletext;
        }

        
        private const int WikiProjectsWPBS = 3;
        
        /// <summary>
        /// Adds WikiProjectBannerShell when needed (> 3 WikiProject templates and no WikiProjectBannerShell)
        /// </summary>
        /// <param name="articletext">The talk page text</param>
        /// <returns>The updated talk page text</returns>
        private static string AddWikiProjectBannerShell(string articletext)
        {
            int wikiProjectTemplates = 0;
            string WPBS1 = "", articletextLocal = articletext;
            
            if(!WikiRegexes.WikiProjectBannerShellTemplate.IsMatch(articletextLocal))
            {
                foreach(Match m in WikiRegexes.NestedTemplates.Matches(articletextLocal))
                {
                    if(!Tools.GetTemplateName(m.Value).StartsWith("WikiProject "))
                        continue;
                    
                    wikiProjectTemplates++;
                    WPBS1 += Tools.Newline(m.Value);
                    articletextLocal = articletextLocal.Replace(m.Value, "");
                }
                
                if(wikiProjectTemplates > WikiProjectsWPBS)
                {
                    // add a WikiProjectBannerShell
                    articletext = @"{{WikiProjectBannerShell|1=" + WPBS1 + Tools.Newline(@"}}")
                        + articletextLocal;
                }
            }
            
            return articletext;
        }
    }
}