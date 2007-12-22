/*
Autowikibrowser
Copyright (C) 2007 Martin Richards
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

using System;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;
using WikiFunctions;
using WikiFunctions.Plugin;
using WikiFunctions.AWBSettings;
using AutoWikiBrowser.Plugins;

namespace AutoWikiBrowser
{
    partial class MainForm
    {
        private void saveAsDefaultToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to save these settings as the default settings?", "Save as default?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                SavePrefs();
        }

        private void saveSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(SettingsFile))
            {
                if ((!System.IO.File.Exists(SettingsFile)) || MessageBox.Show("Replace existing file?", "File exists - " + SettingsFile,
            MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
                    SavePrefs(SettingsFile);
            }
            else if (MessageBox.Show("No settings file currently loaded. Save as Default?", "Save current settings as Default?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                SavePrefs();
            else
            {
                saveCurrentSettingsToolStripMenuItem_Click(null, null);
            }
        }        

        private void loadSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadSettingsDialog();
        }

        private void loadDefaultSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Would you really like to load the original default settings?", "Reset settings to default?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                ResetSettings();
        }

        private void saveCurrentSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveXML.FileName = SettingsFile;
            if (saveXML.ShowDialog() != DialogResult.OK)
                return;

            SavePrefs(saveXML.FileName);
            SettingsFile = saveXML.FileName;
        }

        private void ResetSettings()
        {
            findAndReplace.Clear();
            replaceSpecial.Clear();
            substTemplates.Clear();

            listMaker1.SelectedSource = 0;
            listMaker1.SourceText = "";
            listMaker1.Clear();

            chkGeneralFixes.Checked = true;
            chkAutoTagger.Checked = true;
            chkUnicodifyWhole.Checked = true;

            chkFindandReplace.Checked = false;
            chkSkipWhenNoFAR.Checked = true;
            findAndReplace.ignoreLinks = false;
            findAndReplace.ignoreMore = false;
            findAndReplace.AppendToSummary = true;
            findAndReplace.AfterOtherFixes = false;

            cmboCategorise.SelectedIndex = 0;
            txtNewCategory.Text = "";
            txtNewCategory2.Text = "";

            chkSkipNoCatChange.Checked = false;
            chkSkipNoImgChange.Checked = false;

            chkSkipIfContains.Checked = false;
            chkSkipIfNotContains.Checked = false;
            chkSkipIsRegex.Checked = false;
            chkSkipCaseSensitive.Checked = false;
            txtSkipIfContains.Text = "";
            txtSkipIfNotContains.Text = "";
            Skip.SelectedItem = "0";

            chkAppend.Checked = false;
            rdoAppend.Checked = true;
            txtAppendMessage.Text = "";

            cmboImages.SelectedIndex = 0;
            txtImageReplace.Text = "";
            txtImageWith.Text = "";

            chkRegExTypo.Checked = false;
            chkSkipIfNoRegexTypo.Checked = false;

            txtFind.Text = "";
            chkFindRegex.Checked = false;
            chkFindCaseSensitive.Checked = false;

            cmboEditSummary.SelectedIndex = 0;
            cmboEditSummary.Items.Clear();
            LoadDefaultEditSummaries();

            wordWrapToolStripMenuItem1.Checked = true;
            panel1.Show();
            EnableToolBar = false;
            bypassRedirectsToolStripMenuItem.Checked = true;
            chkSkipNonExistent.Checked = true;
            chkSkipExistent.Checked = false;
            doNotAutomaticallyDoAnythingToolStripMenuItem.Checked = false;
            chkSkipNoChanges.Checked = false;
            chkSkipSpamFilter.Checked = false;
            chkSkipIfInuse.Checked = false;
            toolStripComboOnLoad.SelectedIndex = 0;
            ignoreNoBotsToolStripMenuItem.Checked = false;
            markAllAsMinorToolStripMenuItem.Checked = false;
            addAllToWatchlistToolStripMenuItem.Checked = false;
            showTimerToolStripMenuItem.Checked = false;
            ShowTimer();
            alphaSortInterwikiLinksToolStripMenuItem.Checked = true;
            addIgnoredToLogFileToolStripMenuItem.Checked = false;
            showHideEditToolbarToolStripMenuItem.Checked = true;
            EditToolBarVisible = true;

            PasteMore1.Text = "";
            PasteMore2.Text = "";
            PasteMore3.Text = "";
            PasteMore4.Text = "";
            PasteMore5.Text = "";
            PasteMore6.Text = "";
            PasteMore7.Text = "";
            PasteMore8.Text = "";
            PasteMore9.Text = "";
            PasteMore10.Text = "";

            chkAutoMode.Checked = false;
            chkQuickSave.Checked = false;
            nudBotSpeed.Value = 15;

            //preferences
            System.Drawing.Font f = new System.Drawing.Font("Courier New", 10F, System.Drawing.FontStyle.Regular);
            txtEdit.Font = f;
            LowThreadPriority = false;
            Flash = true;
            Beep = true;
            Minimize = true;
            TimeOut = 30;
            SaveArticleList = true;
            chkLock.Checked = false;

            AutoSaveEditBoxEnabled = false;
            AutoSaveEditBoxFile = "Edit Box.txt";
            AutoSaveEditBoxPeriod = 60;

            SupressUsingAWB = false;
                        
            chkEnableDab.Checked = false;
            txtDabLink.Text = "";
            txtDabVariants.Text = "";
            chkSkipNoDab.Checked = false;
            udContextChars.Value = 20;

            loggingSettings1.Reset();

            try
            {
                foreach (KeyValuePair<string, IAWBPlugin> a in Plugin.Items)
                    a.Value.Reset();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Problem reseting plugin\r\n\r\n" + ex.Message);
            }

            cModule.ModuleEnabled = false;
            this.Text = "AutoWikiBrowser - Default.xml";
            lblStatusText.Text = "Default settings loaded.";
        }

        private void LoadDefaultEditSummaries()
        {
            cmboEditSummary.Items.Add("clean up");
            cmboEditSummary.Items.Add("re-categorisation per [[WP:CFD|CFD]]");
            cmboEditSummary.Items.Add("clean up and re-categorisation per [[WP:CFD|CFD]]");
            cmboEditSummary.Items.Add("removing category per [[WP:CFD|CFD]]");
            cmboEditSummary.Items.Add("[[Wikipedia:Template substitution|subst:'ing]]");
            cmboEditSummary.Items.Add("[[Wikipedia:WikiProject Stub sorting|stub sorting]]");
            cmboEditSummary.Items.Add("[[WP:AWB/T|Typo fixing]]");
            cmboEditSummary.Items.Add("bad link repair");
            cmboEditSummary.Items.Add("Fixing [[Wikipedia:Disambiguation pages with links|links to disambiguation pages]]");
            cmboEditSummary.Items.Add("Unicodifying");
        }

        private void LoadSettingsDialog()
        {
            if (openXML.ShowDialog() != DialogResult.OK)
                return;

            LoadPrefs(openXML.FileName);
            SettingsFile = openXML.FileName;

            listMaker1.removeListDuplicates();
        }

        public void LoadRecentSettingsList()
        {
            string s;

            try
            {
                Microsoft.Win32.RegistryKey reg = Microsoft.Win32.Registry.CurrentUser.
                    OpenSubKey("Software\\Wikipedia\\AutoWikiBrowser");

                s = reg.GetValue("RecentList", "").ToString();
            }
            catch { return; }
            UpdateRecentList(s.Split('|'));
        }

        public void UpdateRecentList(string[] list)
        {
            RecentList.Clear();
            RecentList.AddRange(list);
            UpdateRecentSettingsMenu();
        }

        public void UpdateRecentList(string s)
        {
            int i = RecentList.IndexOf(s);

            if (i >= 0) RecentList.RemoveAt(i);

            RecentList.Insert(0, s);
            UpdateRecentSettingsMenu();
        }

        private void UpdateRecentSettingsMenu()
        {
            while (RecentList.Count > 5)
                RecentList.RemoveAt(5);

            recentToolStripMenuItem.DropDown.Items.Clear();
            int i = 1;
            foreach (string filename in RecentList)
            {
                if (i != RecentList.Count)
                {
                    i++;
                    ToolStripItem item = recentToolStripMenuItem.DropDownItems.Add(filename);
                    item.Click += RecentSettingsClick;
                }
            }
        }

        public void SaveRecentSettingsList()
        {
            Microsoft.Win32.RegistryKey reg = Microsoft.Win32.Registry.CurrentUser.
                    CreateSubKey("Software\\Wikipedia\\AutoWikiBrowser");

            string list = "";
            foreach (string s in RecentList)
            {
                if (!string.IsNullOrEmpty(list)) list += "|";
                list += s;
            }

            reg.SetValue("RecentList", list);
        }

        private void RecentSettingsClick(object sender, EventArgs e)
        {
            LoadPrefs((sender as ToolStripItem).Text);
            SettingsFile = (sender as ToolStripItem).Text;
            listMaker1.removeListDuplicates();
        }

        //new methods, using serialization

        /// <summary>
        /// Make preferences object from current settings
        /// </summary>
        private UserPrefs MakePrefs()
        {
            //p.Editprefs.Append = rdoAppend.Checked;

            return new UserPrefs(new FaRPrefs(chkFindandReplace.Checked, findAndReplace, replaceSpecial,
                substTemplates.TemplateList, substTemplates.ExpandRecursively, substTemplates.IgnoreUnformatted,
                substTemplates.IncludeComment), new EditPrefs(chkGeneralFixes.Checked, chkAutoTagger.Checked,
                chkUnicodifyWhole.Checked, cmboCategorise.SelectedIndex, txtNewCategory.Text,
                txtNewCategory2.Text, cmboImages.SelectedIndex, txtImageReplace.Text, txtImageWith.Text,
                chkSkipNoCatChange.Checked, chkSkipNoImgChange.Checked, chkAppend.Checked, !rdoPrepend.Checked,
                txtAppendMessage.Text, (int)udNewlineChars.Value, (int)nudBotSpeed.Value, chkQuickSave.Checked, chkSuppressTag.Checked,
                chkRegExTypo.Checked), new ListPrefs(listMaker1, SaveArticleList),
                new SkipPrefs(chkSkipNonExistent.Checked, chkSkipExistent.Checked, chkSkipNoChanges.Checked, chkSkipSpamFilter.Checked,
                chkSkipIfInuse.Checked, chkSkipIfContains.Checked, chkSkipIfNotContains.Checked, txtSkipIfContains.Text,
                txtSkipIfNotContains.Text, chkSkipIsRegex.Checked, chkSkipCaseSensitive.Checked,
                chkSkipWhenNoFAR.Checked, chkSkipIfNoRegexTypo.Checked, chkSkipNoDab.Checked, Skip.SelectedItem),
                new GeneralPrefs(SaveArticleList, ignoreNoBotsToolStripMenuItem.Checked, cmboEditSummary.Items,
                cmboEditSummary.Text, new string[10] {PasteMore1.Text, PasteMore2.Text, PasteMore3.Text, 
                PasteMore4.Text, PasteMore5.Text, PasteMore6.Text, PasteMore7.Text, PasteMore8.Text,
                PasteMore9.Text, PasteMore10.Text}, txtFind.Text, chkFindRegex.Checked,
                chkFindCaseSensitive.Checked, wordWrapToolStripMenuItem1.Checked, EnableToolBar,
                bypassRedirectsToolStripMenuItem.Checked, doNotAutomaticallyDoAnythingToolStripMenuItem.Checked,
                toolStripComboOnLoad.SelectedIndex, chkMinor.Checked, addAllToWatchlistToolStripMenuItem.Checked,
                showTimerToolStripMenuItem.Checked, sortAlphabeticallyToolStripMenuItem.Checked,
                addIgnoredToLogFileToolStripMenuItem.Checked, (int)txtEdit.Font.Size, txtEdit.Font.Name,
                LowThreadPriority, Beep, Flash, Minimize, TimeOut, AutoSaveEditBoxEnabled, AutoSaveEditBoxPeriod,
                AutoSaveEditBoxFile, CustomWikis, chkLock.Checked, EditToolBarVisible, SupressUsingAWB, filterOutNonMainSpaceToolStripMenuItem.Checked), new DabPrefs(chkEnableDab.Checked,
                txtDabLink.Text, txtDabVariants.Lines, (int)udContextChars.Value), new ModulePrefs(
                cModule.ModuleEnabled, cModule.Language, cModule.Code), loggingSettings1.SerialisableSettings, 
                Plugin.Items);
        }

        /// <summary>
        /// Load preferences object
        /// </summary>
        private void LoadPrefs(UserPrefs p)
        {
            SetProject(p.LanguageCode, p.Project, p.CustomProject);

            chkFindandReplace.Checked = p.FindAndReplace.Enabled;
            findAndReplace.ignoreLinks = p.FindAndReplace.IgnoreSomeText;
            findAndReplace.ignoreMore = p.FindAndReplace.IgnoreMoreText;
            findAndReplace.AppendToSummary = p.FindAndReplace.AppendSummary;
            findAndReplace.AfterOtherFixes = p.FindAndReplace.AfterOtherFixes;
            findAndReplace.AddNew(p.FindAndReplace.Replacements);
            replaceSpecial.AddNewRule(p.FindAndReplace.AdvancedReps);

            substTemplates.TemplateList = p.FindAndReplace.SubstTemplates;
            substTemplates.ExpandRecursively = p.FindAndReplace.ExpandRecursively;
            substTemplates.IgnoreUnformatted = p.FindAndReplace.IgnoreUnformatted;
            substTemplates.IncludeComment = p.FindAndReplace.IncludeComments;

            findAndReplace.MakeList();

            listMaker1.SourceText = p.List.ListSource;
            listMaker1.SelectedSource = p.List.Source;

            SaveArticleList = p.General.SaveArticleList;

            ignoreNoBotsToolStripMenuItem.Checked = p.General.IgnoreNoBots;

            listMaker1.Add(p.List.ArticleList);
            
            chkGeneralFixes.Checked = p.Editprefs.GeneralFixes;
            chkAutoTagger.Checked = p.Editprefs.Tagger;
            chkUnicodifyWhole.Checked = p.Editprefs.Unicodify;

            cmboCategorise.SelectedIndex = p.Editprefs.Recategorisation;
            txtNewCategory.Text = p.Editprefs.NewCategory;
            txtNewCategory2.Text = p.Editprefs.NewCategory2;
            
            cmboImages.SelectedIndex = p.Editprefs.ReImage;
            txtImageReplace.Text = p.Editprefs.ImageFind;
            txtImageWith.Text = p.Editprefs.Replace;

            chkSkipNoCatChange.Checked = p.Editprefs.SkipIfNoCatChange;
            chkSkipNoImgChange.Checked = p.Editprefs.SkipIfNoImgChange;

            chkAppend.Checked = p.Editprefs.AppendText;
            rdoAppend.Checked = p.Editprefs.Append;
            rdoPrepend.Checked = !p.Editprefs.Append;
            txtAppendMessage.Text = p.Editprefs.Text;
            udNewlineChars.Value = p.Editprefs.Newlines;

            nudBotSpeed.Value = p.Editprefs.AutoDelay;
            chkQuickSave.Checked = p.Editprefs.QuickSave;
            chkSuppressTag.Checked = p.Editprefs.SuppressTag;

            chkRegExTypo.Checked = p.Editprefs.RegexTypoFix;
            
            chkSkipNonExistent.Checked = p.SkipOptions.SkipNonexistent;
            chkSkipExistent.Checked = p.SkipOptions.Skipexistent;
            chkSkipNoChanges.Checked = p.SkipOptions.SkipWhenNoChanges;
            chkSkipSpamFilter.Checked = p.SkipOptions.SkipSpamFilterBlocked;
            chkSkipIfInuse.Checked = p.SkipOptions.SkipInuse;

            chkSkipIfContains.Checked = p.SkipOptions.SkipDoes;
            chkSkipIfNotContains.Checked = p.SkipOptions.SkipDoesNot;

            txtSkipIfContains.Text = p.SkipOptions.SkipDoesText;
            txtSkipIfNotContains.Text = p.SkipOptions.SkipDoesNotText;

            chkSkipIsRegex.Checked = p.SkipOptions.Regex;
            chkSkipCaseSensitive.Checked = p.SkipOptions.CaseSensitive;

            chkSkipWhenNoFAR.Checked = p.SkipOptions.SkipNoFindAndReplace;
            chkSkipIfNoRegexTypo.Checked = p.SkipOptions.SkipNoRegexTypoFix;
            Skip.SelectedItem = p.SkipOptions.GeneralSkip;
            chkSkipNoDab.Checked = p.SkipOptions.SkipNoDisambiguation;

            cmboEditSummary.Items.Clear();
            foreach (string s in p.General.Summaries)
            {
                cmboEditSummary.Items.Add(s);
            }

            chkLock.Checked = p.General.LockSummary;
            EditToolBarVisible = p.General.EditToolbarEnabled;

            cmboEditSummary.Text = p.General.SelectedSummary;

            if (chkLock.Checked)
                lblSummary.Text = p.General.SelectedSummary;

            PasteMore1.Text = p.General.PasteMore[0];
            PasteMore2.Text = p.General.PasteMore[1];
            PasteMore3.Text = p.General.PasteMore[2];
            PasteMore4.Text = p.General.PasteMore[3];
            PasteMore5.Text = p.General.PasteMore[4];
            PasteMore6.Text = p.General.PasteMore[5];
            PasteMore7.Text = p.General.PasteMore[6];
            PasteMore8.Text = p.General.PasteMore[7];
            PasteMore9.Text = p.General.PasteMore[8];
            PasteMore10.Text = p.General.PasteMore[9];

            txtFind.Text = p.General.FindText;
            chkFindRegex.Checked = p.General.FindRegex;
            chkFindCaseSensitive.Checked = p.General.FindCaseSensitive;

            wordWrapToolStripMenuItem1.Checked = p.General.WordWrap;
            EnableToolBar = p.General.ToolBarEnabled;
            bypassRedirectsToolStripMenuItem.Checked = p.General.BypassRedirect;
            doNotAutomaticallyDoAnythingToolStripMenuItem.Checked = p.General.NoAutoChanges;
            toolStripComboOnLoad.SelectedIndex = p.General.OnLoadAction;
            chkMinor.Checked = p.General.Minor;
            addAllToWatchlistToolStripMenuItem.Checked = p.General.Watch;
            showTimerToolStripMenuItem.Checked = p.General.TimerEnabled;
            ShowTimer();
            sortAlphabeticallyToolStripMenuItem.Checked = p.General.SortInterwikiOrder;
            addIgnoredToLogFileToolStripMenuItem.Checked = p.General.AddIgnoredToLog;

            AutoSaveEditBoxEnabled = p.General.AutoSaveEdit.Enabled;
            AutoSaveEditBoxPeriod = p.General.AutoSaveEdit.SavePeriod;
            AutoSaveEditBoxFile = p.General.AutoSaveEdit.SaveFile;

            SupressUsingAWB = p.General.SupressUsingAWB;
            filterOutNonMainSpaceToolStripMenuItem.Checked = p.General.filterNonMainSpace;

            CustomWikis = p.General.CustomWikis;

            txtEdit.Font = new System.Drawing.Font(p.General.TextBoxFont, p.General.TextBoxSize);

            LowThreadPriority = p.General.LowThreadPriority;
            Flash = p.General.Flash;
            Beep = p.General.Beep;

            Minimize = p.General.Minimize;
            TimeOut = p.General.TimeOutLimit;
            webBrowserEdit.TimeoutLimit = int.Parse(TimeOut.ToString());
            
            chkEnableDab.Checked = p.Disambiguation.Enabled;
            txtDabLink.Text = p.Disambiguation.Link;
            txtDabVariants.Lines = p.Disambiguation.Variants;
            udContextChars.Value = p.Disambiguation.ContextChars;

            loggingSettings1.SerialisableSettings = p.Logging;

            cModule.ModuleEnabled = p.Module.Enabled;
            cModule.Language = p.Module.Language;
            cModule.Code = p.Module.Code.Replace("\n", "\r\n");
            if (cModule.ModuleEnabled) cModule.MakeModule();

            foreach (PluginPrefs pp in p.Plugin)
            {
                if (Plugin.Items.ContainsKey(pp.Name))
                    Plugin.Items[pp.Name].LoadSettings(pp.PluginSettings);
            }
        }

        /// <summary>
        /// Save preferences as default
        /// </summary>
        private void SavePrefs()
        {
            SavePrefs("Default.xml");
        }

        /// <summary>
        /// Save preferences to file
        /// </summary>
        private void SavePrefs(string path)
        {
            try
            {
                using (FileStream fStream = new FileStream(path, FileMode.Create))
                {
                    UserPrefs p = MakePrefs();
                    List<System.Type> types = SavePluginSettings(p);
                    
                    XmlSerializer xs = new XmlSerializer(typeof(UserPrefs), types.ToArray());
                    xs.Serialize(fStream, p);
                    UpdateRecentList(path);
                    SettingsFile = path.Remove(0, path.LastIndexOf("\\") + 1);
                    UpdateSettingsFile();
                }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error saving settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private List<System.Type> SavePluginSettings(UserPrefs Prefs)
        {
            List<System.Type> types = new List<Type>();
            /* Find out what types the plugins are using for their settings so we can 
                      * add them to the Serializer. The plugin author must ensure s(he) is using
                      * serializable types.
                      */

            foreach (PluginPrefs pl in Prefs.Plugin)
            {
                if ((pl.PluginSettings != null) && (pl.PluginSettings.Length >= 1))
                {
                    foreach (object pl2 in pl.PluginSettings)
                    {
                        types.Add(pl2.GetType());
                    }
                }
            }
            return types;
        }

        /// <summary>
        /// Load default preferences
        /// </summary>
        private void LoadPrefs()
        {
            if (File.Exists("Default.xml"))
                SettingsFile = "Default.xml";

            if (!string.IsNullOrEmpty(SettingsFile))
                LoadPrefs(SettingsFile);
            else
                return;
        }

        /// <summary>
        /// Load preferences from file
        /// </summary>
        private void LoadPrefs(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                    return;

                findAndReplace.Clear();
                replaceSpecial.Clear();
                substTemplates.Clear();

                string settings;

                using (StreamReader f = new StreamReader(path, Encoding.UTF8))
                {
                    settings = f.ReadToEnd();
                }

                //test to see if it is an old AWB file
                if (settings.Contains("<projectlang proj="))
                {
                    MessageBox.Show("This file uses old settings format unsupported by this version of AWB.");
                    return;
                }

                // fix for format regression
                settings = settings.Replace("RegularExpressinonOptions>", "RegularExpressionOptions>");

                UserPrefs p;
                XmlSerializer xs = new XmlSerializer(typeof(UserPrefs));
                p = (UserPrefs)xs.Deserialize(new StringReader(settings));
                LoadPrefs(p);


                SettingsFile = path.Remove(0, path.LastIndexOf("\\") + 1);
                UpdateSettingsFile();
                lblStatusText.Text = "Settings successfully loaded";
                UpdateRecentList(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error loading settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateSettingsFile()
        {
            this.Text = "AutoWikiBrowser - " + SettingsFile;
            UpdateNotifyIconTooltip();
        }
    }
}
