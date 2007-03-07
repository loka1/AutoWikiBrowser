using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using WikiFunctions;
using System.Text.RegularExpressions;
using WikiFunctions.Disambiguation;

namespace AutoWikiBrowser
{
    public partial class DabForm : Form
    {
        public DabForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// if true, all processing should be immediately halted
        /// </summary>
        public bool Abort = false;

        List<string> Variants = new List<string>();
        string DabLink;
        string ArticleText;
        string ArticleTitle;
        Regex Search;
        MatchCollection Matches;

        List<DabControl> Dabs = new List<DabControl>();

        static int SavedWidth = 0;
        static int SavedHeight = 0;
        static int SavedLeft = 0;
        static int SavedTop = 0;
        bool NoSave = true;

        private void btnCancel_Click(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// displays form that promts user for disambiguation
        /// if no disambihuation needed, immediately returns
        /// </summary>
        /// <param name="articleText"></param>
        /// <param name="articleTitle"></param>
        /// <param name="dabLink">link to be disambiguated</param>
        /// <param name="dabVariants">variants of disambiguation</param>
        /// <param name="Skip">returns true when no disambiguation made</param>
        /// <returns></returns>
        public string Disambiguate(string articleText, string articleTitle, string dabLink,
            string[] dabVariants, out bool Skip)
        {
            Skip = true;


            DabLink = dabLink;
            //dabLink = Regex.Escape(dabLink.Replace('|', '⌊')).Replace('⌊', '|').Trim(new char[] { '|' });
            if (dabLink.Contains("|"))
            {
                string sum = "";
                foreach (string s in dabLink.Split(new char[] { '|' }))
                {
                    if (s.Trim() == "") continue;
                    sum += "|" + Tools.CaseInsensitive(Regex.Escape(s.Trim()));
                }
                if (sum.Length > 0 && sum[0] == '|') sum = sum.Remove(0, 1);
                if (sum.Contains("|")) sum = "(?:" + sum + ")";
                dabLink = sum;
            }
            else dabLink = Tools.CaseInsensitive(dabLink.Trim());
            ArticleText = articleText;
            ArticleTitle = articleTitle;

            foreach (string s in dabVariants)
            {
                if (s.Trim() == "") continue;
                Variants.Add(s.Trim());
            }

            if (Variants.Count == 0) return articleText;

            Search = new Regex(@"\[\[\s*(" + dabLink + 
                @")\s*(?:|#[^\|\]]*)(|\|[^\]]*)\]\]");

            Matches = Search.Matches(articleText);

            if (Matches.Count == 0) return articleText;

            foreach (Match m in Matches)
            {
                DabControl c = new DabControl(articleText, dabLink, m, Variants);
                c.Changed += new EventHandler(OnUserInput);
                tableLayout.Controls.Add(c);
                Dabs.Add(c);
            }

            DialogResult r = ShowDialog(Application.OpenForms[0]);

            switch (r)
            {
                case DialogResult.OK:
                    break; // proceed further
                case DialogResult.Abort:
                    Abort = true;
                    return articleText;
                case DialogResult.Cancel:
                    Skip = true;
                    return articleText; 
                    break;
                default:
                    return articleText;
            }

            int adjust = 0;
            foreach (DabControl d in Dabs)
            {
                ArticleText = ArticleText.Remove(d.SurroundingsStart + adjust, d.Surroundings.Length);
                ArticleText = ArticleText.Insert(d.SurroundingsStart + adjust, d.Result);
                adjust += d.Result.Length - d.Surroundings.Length;
            }

            if (ArticleText != articleText) Skip = false;

            return ArticleText;
        }

        private void btnResetAll_Click(object sender, EventArgs e)
        {
            foreach (DabControl d in Dabs)
            {
                d.Reset();
            }
        }

        private void btnUndoAll_Click(object sender, EventArgs e)
        {
            foreach (DabControl d in Dabs)
            {
                d.Undo();
            }
        }

        private void OnUserInput(object sender, EventArgs e)
        {
            bool l = true;
            foreach (DabControl d in Dabs)
            {
                l &= d.CanSave;
            }
            btnDone.Enabled = l;
        }

        private void btnOpenInBrowser_Click(object sender, EventArgs e)
        {
            if (Variables.Project == ProjectEnum.custom)
                System.Diagnostics.Process.Start(Variables.URLLong + "index.php?title=" + ArticleTitle);
            else System.Diagnostics.Process.Start(Variables.URL + "/wiki/" + ArticleTitle);
        }

        private void DabForm_Load(object sender, EventArgs e)
        {
            Text += " — " + ArticleTitle;
            if (SavedWidth != 0)
            {
                Width = SavedWidth;
                Height = SavedHeight;
            }
            if (SavedLeft != 0)
            {
                
                Left = SavedLeft;
                Top = SavedTop;
            }
            NoSave = false;
            Dabs[0].Select();
        }

        private void DabForm_Move(object sender, EventArgs e)
        {
            if (!NoSave)
            {
                SavedLeft = Left;
                SavedTop = Top;
            }
        }

        private void DabForm_Resize(object sender, EventArgs e)
        {
            if (!NoSave)
            {
                SavedWidth = Width;
                SavedHeight = Height;
            }
        }
    }
}