using WikiFunctions;
using WikiFunctions.TalkPages;
using WikiFunctions.Parse;
using WikiFunctions.ReplaceSpecial;
using NUnit.Framework;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using WikiFunctions.Controls.Lists;

namespace UnitTests
{
    [TestFixture]
    public class HideTextTests : RequiresInitialization
    {
        #region Helpers
        const string Hidden = @"⌊⌊⌊⌊M?\d+⌋⌋⌋⌋",
        AllHidden = @"^(⌊⌊⌊⌊M?\d+⌋⌋⌋⌋)*$";
        HideText Hider;

        private string HideMore(string text, bool hideExternalLinks, bool leaveMetaHeadings, bool hideImages)
        {
            Hider = new HideText(hideExternalLinks, leaveMetaHeadings, hideImages);
            string s = Hider.HideMore(text);
            Assert.AreEqual(text, Hider.AddBackMore(s));
            return s;
        }

        private string HideMore(string text)
        {
            return HideMore(text, false);
        }

        private string HideMore(string text, bool hideOnlyTargetOfWikilink)
        {
            Hider = new HideText();
            string s = Hider.HideMore(text, hideOnlyTargetOfWikilink);
            Assert.AreEqual(text, Hider.AddBackMore(s));
            return s;
        }

        private void AssertHiddenMore(string text)
        {
            RegexAssert.IsMatch(Hidden, HideMore(text));
        }

        private void AssertAllHiddenMore(string text)
        {
            RegexAssert.IsMatch(AllHidden, HideMore(text));
        }

        private void AssertAllHiddenMore(string text, bool hideExternalLinks)
        {
            RegexAssert.IsMatch(AllHidden, HideMore(text, hideExternalLinks, false, true));
        }

        private string Hide(string text)
        {
            return Hide(text, true, false, true);
        }

        private string Hide(string text, bool hideExternalLinks, bool leaveMetaHeadings, bool hideImages)
        {
            Hider = new HideText(hideExternalLinks, leaveMetaHeadings, hideImages);
            string s = Hider.Hide(text);
            Assert.AreEqual(text, Hider.AddBack(s));
            return s;
        }

        private void AssertHidden(string text, bool hideExternalLinks, bool leaveMetaHeadings, bool hideImages)
        {
            RegexAssert.IsMatch(Hidden, Hide(text, hideExternalLinks, leaveMetaHeadings, hideImages));
        }

        private void AssertHidden(string text)
        {
            AssertHidden(text, true, false, true);
        }

        private void AssertAllHidden(string text)
        {
            RegexAssert.IsMatch(AllHidden, Hide(text));
        }
        private void AssertBothHidden(string text)
        {
            AssertBothHidden(text, true, false, true);
        }

        private void AssertBothHidden(string text, bool hideExternalLinks, bool leaveMetaHeadings, bool hideImages)
        {
            Hider = new HideText(hideExternalLinks, leaveMetaHeadings, hideImages);
            AssertAllHidden(text);
            AssertAllHiddenMore(text);
        }

        private void AssertPartiallyHidden(string expected, string text)
        {
            Assert.AreEqual(expected, Hide(text));
        }

        private void AssertPartiallyHiddenMore(string expected, string text, bool hideOnlyTargetOfWikilink)
        {
            Assert.AreEqual(expected, HideMore(text, hideOnlyTargetOfWikilink));
        }

        private void AssertPartiallyHiddenMore(string expected, string text)
        {
            AssertPartiallyHiddenMore(expected, text, false);
        }

        #endregion

        [Test]
        public void AcceptEmptyStrings()
        {
            Assert.AreEqual("", Hide(""));
            Assert.AreEqual("", HideMore(""));
        }

        [Test]
        public void HideLinks()
        {
            AssertHiddenMore("[[foo]]");
            AssertHiddenMore("[[foo|bar]]");
        }

        [Test]
        public void LeaveLinkFace()
        {
            StringAssert.Contains("bar", HideMore("[[foo|bar]]", true));
        }
        
        [Test]
        public void HideItalics()
        {
            StringAssert.DoesNotContain("text", HideMore("Now ''text'' was"));
        }

        [Test]
        public void HideTemplates()
        {
            AssertAllHiddenMore("{{foo}}");
            AssertAllHiddenMore("{{{foo}}}");
            AssertAllHiddenMore("{{foo|}}");
            AssertAllHiddenMore("{{foo|bar}}");
            RegexAssert.IsMatch("123" + Hidden + "123", HideMore("123{{foo}}123"));
            AssertAllHiddenMore("{{foo|{{bar}}}}");
            AssertAllHiddenMore(@"{{foo|
abc={{bar}}
|def=hello}}");
            AssertAllHiddenMore("{{foo|{{bar|{{{1|}}}}}}}");
            AssertAllHiddenMore("{{foo|\r\nbar= {blah} blah}}");
            AssertAllHiddenMore("{{foo|\r\nbar= {blah} {{{1|{{blah}}}}}}}");
        }

        [Test]
        public void HidePstyles()
        {
            AssertAllHiddenMore(@"<p style=""margin:0px;font-size:100%""><span style=""color:#00ff00"">▪</span> <small>Francophone minorities</small></p>");
            AssertAllHiddenMore(@"<p style=""font-family:monospace; line-height:130%"">hello</p>");
            AssertAllHiddenMore(@"<p style=""font-family:monospace; line-height:130%"">hello</P>");
            AssertAllHiddenMore(@"<p style=""font-family:monospace; line-height:130%"">hello
</P>");
        }

        const string Caption1 = @"|image_caption=London is a European Parliament constituency. It has water. |",
        Caption2 = @"|image_caption= some load of text here. Some more there.}}",
        Caption3 = @"|image_caption=some load of text here. Some more there.   }}",
        Caption4 = @"|image_caption= some load of text here. Some more there.|",
        Caption5 = @"|image_caption
            = some load of text here. Some more there.
            |",
        Field1 = @"field = value |";

        [Test]
        public void HideImages()
        {
            AssertAllHidden(@"[[File:foo.jpg]]");
            AssertAllHidden(@"[[File:foo with space and 0004.jpg]]");
            AssertAllHidden(@"[[File:foo.jpeg]]");
            AssertAllHidden(@"[[File:foo.JPEG]]");
            AssertAllHidden(@"[[Image:foo with space and 0004.jpeg]]");
            AssertAllHidden(@"[[Image:foo.jpeg]]");
            AssertAllHidden(@"[[Image:foo with space and 0004.jpg]]");
            AssertAllHidden(@"[[File:foo.jpg|");
            AssertAllHidden(@"[[File:foo with space and 0004.jpg|");
            AssertAllHidden(@"[[File:foo.jpeg|");
            AssertAllHidden(@"[[Image:foo with space and 0004.jpeg|");
            AssertAllHidden(@"[[Image:foo.jpeg|");
            AssertAllHidden(@"[[Image:foo with SPACE() and 0004.jpg|");
            AssertAllHidden(@"[[File:foo.gif|");
            AssertAllHidden(@"[[Image:foo with space and 0004.gif|");
            AssertAllHidden(@"[[Image:foo.gif|");
            AssertAllHidden(@"[[Image:foo with SPACE() and 0004.gif|");
            AssertAllHidden(@"[[File:foo.png|");
            AssertAllHidden(@"[[Image:foo with space and 0004.png|");
            AssertAllHidden(@"[[Image:foo_here.png|");
            AssertAllHidden(@"[[Image:foo with SPACE() and 0004.png|");
            AssertAllHidden(@"[[Image:westminster.tube.station.jubilee.arp.jpg|");
            
            AssertAllHidden(@"<imagemap>
File:Blogs001.jpeg|Description
File:Blogs002.jpeg|Description
</imagemap>");
        }
        
        [Test]
        public void HideImagesLimits()
        {
            // in these ones all but the last | is hidden
            Assert.IsTrue(Regex.IsMatch(Hide(@"|image_skyline=442px_-_London_Lead_Image.jpg|"), Hidden + @"\|"));
            Assert.IsTrue(Regex.IsMatch(Hide(@"|image_skyline=442px_-_London_Lead_Image.jpg <!--comm-->|"), Hidden + " " + Hidden + @"\|"));
            Assert.IsTrue(Regex.IsMatch(Hide(@"|image_map=London (European Parliament constituency).svg   |"), Hidden + @"   \|"));
            Assert.IsTrue(Regex.IsMatch(Hide(@"|image_map=westminster.tube.station.jubilee.arp.jpg|"), Hidden + @"\|"));
            Assert.IsTrue(Regex.IsMatch(Hide(@"|Cover  = AmorMexicanaThalia.jpg |"), Hidden + @" \|"));
            Assert.IsTrue(Regex.IsMatch(Hide(@"|image = AmorMexicanaThalia.jpg |"), Hidden + @" \|"));
            Assert.IsTrue(Regex.IsMatch(Hide(@"|
image = AmorMexicanaThalia.jpg |"), Hidden + @" \|"));
            Assert.IsTrue(Regex.IsMatch(Hide(@"|Img = BBC_logo1.jpg
|"), Hidden + @"
\|"));
            Assert.IsTrue(Regex.IsMatch(Hide(@"| image name = Fred Astaire.jpg |"), Hidden + @" \|"));
            Assert.IsTrue(Regex.IsMatch(Hide(@"|image2 = AmorMexicanaThalia.jpg |"), Hidden + @" \|"));
            Assert.IsTrue(Regex.IsMatch(Hide(@"|map = AmorMexicanaThalia.jpg |"), Hidden + @" \|"));
        }
        
        [Test]
        public void HideImagesPartial()
        {
            // in tests below no text is hidden
            Assert.AreEqual(Caption1, Hide(Caption1));
            Assert.AreEqual(Caption2, Hide(Caption2));
            Assert.AreEqual(Caption3, Hide(Caption3));
            Assert.AreEqual(Caption4, Hide(Caption4));
            Assert.AreEqual(Caption5, Hide(Caption5));

            // in tests below part of string is hidden
            Assert.IsTrue(Hide(@"[[Image:some image name.JPG|thumb|words with typos]]").EndsWith(@"thumb|words with typos]]"));
            Assert.IsTrue(Hide(@"[[Image:some image name.JPEG|words with typos]]").EndsWith(@"words with typos]]"));
            Assert.IsTrue(Hide(@"[[Image:some image name.jpg|thumb|words with typos ]]").EndsWith(@"thumb|words with typos ]]"));
            Assert.IsTrue(Hide(@"[[Image:some image name.png|20px|words with typos and [[links]] here]]").EndsWith(@"20px|words with typos and [[links]] here]]"));
            Assert.IsTrue(Hide(@"[[Image:some image name.svg|thumb|words with typos ]]").EndsWith(@"thumb|words with typos ]]"));
            Assert.IsTrue(Hide(@"[[Image:some_image_name.further words.tiff|thumb|words with typos<ref name=a/>]]").EndsWith(@"thumb|words with typos<ref name=a/>]]"));
            Assert.IsTrue(Hide(@"[[Image:some_image_name.for a word.png|thumb|words with typo]]").EndsWith(@"thumb|words with typo]]"));
            Assert.IsTrue(Hide(@"[[Image:some_image_name.png|thumb|words with typo]] Normal words in text").EndsWith(@"thumb|words with typo]] Normal words in text"));
            Assert.IsTrue(Hide(@"[[Image:some_image_name.png]] Normal words in text").EndsWith(@" Normal words in text"));
            Assert.IsTrue(Hide(Caption4 + Field1).EndsWith(Field1));
        }
        
        [Test]
        public void HideImagesMisc()
        {
            Assert.IsFalse(HideMore(@"{{Drugbox|
   |IUPAC_name = 6-chloro-1,1-dioxo-2''H''-1,2,4-benzothiadiazine-7-sulfonamide
   | image=Chlorothiazide.svg
   | image2=Chlorothiazide-from-xtal-3D-balls.png
   | CAS_number=58-94-6").Contains("=Chlorothiazide"));
            
            AssertAllHidden(@"{{ gallery
|title=Lamu images
|width=150
|lines=2
|Image:LamuFort.jpg|Lamu Fort
|Image:LAMU Riyadha Mosque.jpg|Riyadha Mosque
|Image:04 Donkey Hospital (June 30 2001).jpg|Donkey Sanctuary
}}");
        }

        [Test]
        public void HideImagesMore()
        {
            AssertAllHiddenMore(@"[[File:foo.jpg]]");
            AssertAllHiddenMore(@"[[File:foo with space and 0004.jpg]]");
            AssertAllHiddenMore(@"[[File:foo.jpeg]]");
            AssertAllHiddenMore(@"[[File:foo.JPEG]]");
            AssertAllHiddenMore(@"[[Image:foo with space and 0004.jpeg]]");
            AssertAllHiddenMore(@"[[Image:foo.jpeg]]");
            AssertAllHiddenMore(@"[[Image:foo with space and 0004.jpg]]");
            AssertAllHiddenMore(@"[[File:foo.jpg|");
            AssertAllHiddenMore(@"[[File:foo with space and 0004.jpg|");
            AssertAllHiddenMore(@"[[File:foo.jpeg|");
            AssertAllHiddenMore(@"[[Image:foo with space and 0004.jpeg|");
            AssertAllHiddenMore(@"[[Image:foo.jpeg|");
            AssertAllHiddenMore(@"[[Image:foo with SPACE() and 0004.jpg|");
            AssertAllHiddenMore(@"[[File:foo.gif|");
            AssertAllHiddenMore(@"[[Image:foo with space and 0004.gif|");
            AssertAllHiddenMore(@"[[Image:foo.gif|");
            AssertAllHiddenMore(@"[[Image:foo with SPACE() and 0004.gif|");
            AssertAllHiddenMore(@"[[File:foo.png|");
            AssertAllHiddenMore(@"[[Image:foo with space and 0004.png|");
            AssertAllHiddenMore(@"[[Image:foo_here.png|");
            AssertAllHiddenMore(@"[[Image:foo with SPACE() and 0004.png|");
            AssertAllHiddenMore(@"[[Image:westminster.tube.station.jubilee.arp.jpg|");

            // in these ones all but the last | is hidden
            Assert.IsTrue(Regex.IsMatch(HideMore(@"|image_skyline=442px_-_London_Lead_Image.jpg|"), Hidden + @"\|"));
            Assert.IsTrue(Regex.IsMatch(HideMore(@"|image_map=London (European Parliament constituency).svg   |"), Hidden + @"   \|"));
            Assert.IsTrue(Regex.IsMatch(HideMore(@"|image_map=westminster.tube.station.jubilee.arp.jpg|"), Hidden + @"\|"));
            Assert.IsTrue(Regex.IsMatch(HideMore(@"|Cover  = AmorMexicanaThalia.jpg |"), Hidden + @" \|"));
            Assert.IsTrue(Regex.IsMatch(HideMore(@"|image = AmorMexicanaThalia.jpg |"), Hidden + @" \|"));
            Assert.IsTrue(Regex.IsMatch(HideMore(@"|
image = AmorMexicanaThalia.jpg |"), Hidden + @" \|"));
            Assert.IsTrue(Regex.IsMatch(HideMore(@"|Img = BBC_logo1.jpg
|"), Hidden + @"
\|"));
            Assert.IsTrue(Regex.IsMatch(HideMore(@"| image name = Fred Astaire.jpg |"), Hidden + @" \|"));
            Assert.IsTrue(Regex.IsMatch(HideMore(@"|image2 = AmorMexicanaThalia.jpg |"), Hidden + @" \|"));

            // in tests below no text is hidden

            Assert.AreEqual(Caption1, HideMore(Caption1));
            Assert.AreEqual(Caption2, HideMore(Caption2));
            Assert.AreEqual(Caption3, HideMore(Caption3));
            Assert.AreEqual(Caption4, HideMore(Caption4));
            Assert.AreEqual(Caption5, HideMore(Caption5));

            // in tests below part of string is hidden
            Assert.IsTrue(HideMore(@"[[Image:some_image_name.png]] Normal words in text").EndsWith(@" Normal words in text"));
            Assert.IsTrue(HideMore(Caption4 + Field1).EndsWith(Field1));

            // in these ones all but the last | or }} is hidden
            Assert.IsTrue(Regex.IsMatch(HideMore(@"| Photo =Arlberg passstrasse.jpg |"), Hidden + @" \|"));
            Assert.IsTrue(Regex.IsMatch(HideMore(@"| Photo =Arlberg passstrasse.jpg}}"), Hidden + @"}}"));
            Assert.IsTrue(Regex.IsMatch(HideMore(@"|photo=Arlberg passstrasse.jpg|"), Hidden + @"\|"));
            Assert.IsTrue(Regex.IsMatch(HideMore(@"| Photo =Arlberg passstrasse.jpg
|"), Hidden + @"
\|"));
            Assert.IsTrue(Regex.IsMatch(HideMore(@"| Image =Arlberg passstrasse.jpg |"), Hidden + @" \|"));
            Assert.IsTrue(Regex.IsMatch(HideMore(@"| image =Arlberg passstrasse.jpg |"), Hidden + @" \|"));
            Assert.IsTrue(Regex.IsMatch(HideMore(@"| Img =Arlberg passstrasse.jpg |"), Hidden + @" \|"));
            Assert.IsTrue(Regex.IsMatch(HideMore(@"| img =Arlberg passstrasse.jpg }}"), Hidden + @" }}"));

            Assert.IsFalse(HideMore(@"{{Drugbox|
   |IUPAC_name = 6-chloro-1,1-dioxo-2''H''-1,2,4-benzothiadiazine-7-sulfonamide
   | image=Chlorothiazide.svg
   | image2=Chlorothiazide-from-xtal-3D-balls.png
   | CAS_number=58-94-6").Contains("=Chlorothiazide"));
            
            AssertAllHiddenMore(@"<imagemap>
File:Blogs001.jpeg|Description
File:Blogs002.jpeg|Description
</imagemap>");
        }

        [Test]
        public void HideGalleries()
        {
            AssertAllHiddenMore(@"<gallery>
Image:foo|a [[bar]]
Image:quux[http://example.com]
</gallery>");
            AssertAllHiddenMore(@"<gallery name=""test"">
Image:foo|a [[bar]]
Image:quux[http://example.com]
</gallery>");
        }

        [Test]
        public void HideExternalLinks()
        {
            AssertAllHiddenMore("[http://foo]", true);
            AssertAllHiddenMore("[http://foo bar]", true);
            AssertAllHiddenMore("[http://foo [bar]", true);
            
            Assert.IsTrue(Hide(@"date=April 2010|url=http://w/010111a.html}}", true, true, true).Contains(@"date=April 2010|url="));
            Assert.IsTrue(Hide(@"date=April 2010|url=http://w/010111a.html}}", true, true, true).Contains(@"}}"));
        }
        
        [Test]
        public void HidePlainExternalLinks()
        {
            AssertAllHiddenMore("http://foo.com/asdfasdf/asdf.htm", true);
            AssertAllHiddenMore("https://www.foo.com/asdfasdf/asdf.htm", true);
            Assert.IsFalse(HideMore(@"http://foo.com/asdfasdf/asdf.htm", true, false, false).Contains("asdf"));
            Assert.IsTrue(HideMore(@"http://foo.com/asdfasdf/asdf.htm", false, false, false).Contains("asdf"));
        }

        [Test]
        public void HideWikiLinksOnlyPlusWord()
        {
            AssertAllHiddenMore("[[Foo]]bar");
            AssertAllHiddenMore("[[Foo|test]]bar");
        }

        [Test]
        public void SimpleHide()
        {
            AssertHidden("<source>foo</source>");
            AssertHidden(@"<source  lang=""foo_bar"">\nfoo\n</source>");
            AssertAllHidden("<source>\r\nfoo\r\n</source><source foo>bar</source>");

            AssertHidden("<pre>foo\r\nbar</pre>");
            AssertHidden("<pre style=quux>foo\r\nbar</pre>");
            AssertHidden("<math>foo\r\nbar</math>");
            AssertHidden("<timeline>foo\r\nbar</timeline>");
            AssertHidden("<timeline foo=bar>foo\r\nbar</timeline>");
            AssertHidden("<nowiki>foo\r\nbar</nowiki>");

            AssertHidden("<!--foo-->");
            AssertHidden("<!--foo\r\nbar-->");

            // hideExternalLinks
            AssertHidden("[http://foo]");
            AssertHidden("[http://foo bar]");
            AssertHidden("https://bar");
            Assert.AreEqual("http://foo", Hide("http://foo", false, false, true));

            // leaveMetaHeadings
            AssertHidden("<!--Categories -->");
            AssertHidden("<!--foo-->", true, true, true);
            Assert.AreEqual("<!-- categories-->", Hide("<!-- categories-->", true, true, true));

            // hideImages
            AssertHidden("[[Image:foo.JPG]]");
            Assert.AreEqual("[[Image:foo.jpg]]", Hide("[[Image:foo.jpg]]", true, false, false));
        }
    }

    [TestFixture]
    public class ArticleTests : RequiresInitialization
    {
        [Test]
        public void NamespacelessName()
        {
            Assert.AreEqual("Foo", new Article("Foo").NamespacelessName);
            Assert.AreEqual("Foo", new Article("Category:Foo").NamespacelessName);
            Assert.AreEqual("Category:Foo", new Article("Category:Category:Foo").NamespacelessName);

            // TODO: uncomment when Namespace.Determine() will support non-normalised names
            //Assert.AreEqual("Foo", new Article("Category : Foo").NamespacelessName);

            // this behaviour has changed in recent MW versions
            //Assert.AreEqual("", new Article("Category:").NamespacelessName);
            //Assert.AreEqual("", new Article("Category: ").NamespacelessName);
        }
    }

    [TestFixture]
    public class NamespaceTests : RequiresInitialization
    {
        [Test]
        public void Determine()
        {
            Assert.AreEqual(0, Namespace.Determine("test"));
            Assert.AreEqual(0, Namespace.Determine(":test"));
            Assert.AreEqual(0, Namespace.Determine("test:test"));
            Assert.AreEqual(0, Namespace.Determine("My Project:Foo"));

            Assert.AreEqual(Namespace.User, Namespace.Determine("User:"));

            Assert.AreEqual(Namespace.Talk, Namespace.Determine("Talk:foo"));
            Assert.AreEqual(Namespace.UserTalk, Namespace.Determine("User talk:bar"));

            Assert.AreEqual(Namespace.File, Namespace.Determine("File:foo"));
            Assert.AreEqual(Namespace.File, Namespace.Determine("Image:foo"));

            Assert.AreEqual(Namespace.Project, Namespace.Determine("Wikipedia:Foo"));
            Assert.AreEqual(Namespace.Project, Namespace.Determine("Project:Foo"));
        }

        [Test]
        public void DetermineDeviations()
        {
            Assert.AreEqual(Namespace.File, Namespace.Determine("File : foo"));
            Assert.AreEqual(Namespace.User, Namespace.Determine("user:foo"));
            Assert.AreEqual(Namespace.UserTalk, Namespace.Determine("user_talk:foo"));
            Assert.AreEqual(Namespace.UserTalk, Namespace.Determine("user%20talk:foo"));
        }

        [Test]
        public void IsTalk()
        {
            Assert.IsTrue(Namespace.IsTalk(Namespace.Talk));
            Assert.IsTrue(Namespace.IsTalk(Namespace.UserTalk));

            Assert.IsFalse(Namespace.IsTalk(Namespace.User));
            Assert.IsFalse(Namespace.IsTalk(Namespace.Project));

            Assert.IsFalse(Namespace.IsTalk(Namespace.Special));
            Assert.IsFalse(Namespace.IsTalk(Namespace.Media));

            Assert.IsTrue(Namespace.IsTalk("Talk:Test"));
            Assert.IsTrue(Namespace.IsTalk("User talk:Test"));

            Assert.IsFalse(Namespace.IsTalk("Test"));
            Assert.IsFalse(Namespace.IsTalk("User:Test"));
        }

        [Test]
        public void IsMainSpace()
        {
            Assert.IsTrue(Namespace.IsMainSpace(Namespace.Article));
            Assert.IsFalse(Namespace.IsMainSpace(Namespace.Talk));

            Assert.IsTrue(Namespace.IsMainSpace("Test"));

            Assert.IsFalse(Namespace.IsMainSpace("User:"));
            Assert.IsFalse(Namespace.IsMainSpace("Talk:Test"));
            Assert.IsFalse(Namespace.IsMainSpace("User:Test"));
            Assert.IsFalse(Namespace.IsMainSpace("User talk:Test"));

            Assert.IsFalse(Namespace.IsMainSpace("File:Test"));
            Assert.IsFalse(Namespace.IsMainSpace("Image:Test"));
        }

        [Test]
        public void IsImportant()
        {
            Assert.IsTrue(Namespace.IsImportant("Test"));
            Assert.IsTrue(Namespace.IsImportant("File:Test.jpg"));
            Assert.IsTrue(Namespace.IsImportant("Template:Test"));
            Assert.IsTrue(Namespace.IsImportant("Category:Test"));

            Assert.IsFalse(Namespace.IsImportant("Talk:Test"));
            Assert.IsFalse(Namespace.IsImportant("File talk:Test"));
            Assert.IsFalse(Namespace.IsImportant("Template talk:Test"));
            Assert.IsFalse(Namespace.IsImportant("Category talk:Test"));
            Assert.IsFalse(Namespace.IsImportant("User:Test"));
        }

        [Test]
        public void IsUserSpace()
        {
            Assert.IsTrue(Namespace.IsUserSpace("User:Test"));
            Assert.IsTrue(Namespace.IsUserSpace("User talk:Test"));

            Assert.IsFalse(Namespace.IsUserSpace("User"));
            Assert.IsFalse(Namespace.IsUserSpace("Test"));
            Assert.IsFalse(Namespace.IsUserSpace("Project:User"));
        }

        [Test]
        public void IsUserTalk()
        {
            Assert.IsTrue(Namespace.IsUserTalk("User talk:Test"));

            Assert.IsFalse(Namespace.IsUserTalk("User:Test"));
            Assert.IsFalse(Namespace.IsUserTalk("Test"));
            Assert.IsFalse(Namespace.IsUserTalk("Project:User"));
        }

        [Test]
        public void IsUserPage()
        {
            Assert.IsTrue(Namespace.IsUserPage("User:Test"));

            Assert.IsFalse(Namespace.IsUserPage("User talk:Test"));
            Assert.IsFalse(Namespace.IsUserPage("Test"));
            Assert.IsFalse(Namespace.IsUserPage("Project:User"));
        }

        [Test]
        public void IsSpecial()
        {
            Assert.IsTrue(Namespace.IsSpecial(Namespace.Media));
            Assert.IsTrue(Namespace.IsSpecial(Namespace.Special));

            Assert.IsFalse(Namespace.IsSpecial(Namespace.Article));
            Assert.IsFalse(Namespace.IsSpecial(Namespace.TemplateTalk));
        }

        [Test]
        public void NormalizeNamespace()
        {
            Assert.AreEqual("User:", Namespace.Normalize("User:", 2));
            Assert.AreEqual("User:", Namespace.Normalize("user :", 2));
            Assert.AreEqual("User talk:", Namespace.Normalize("User_talk:", 3));

            Assert.AreEqual("Image:", Namespace.Normalize("image:", 6));
            Assert.AreEqual("File:", Namespace.Normalize("file:", 6));
            Assert.AreEqual("Image talk:", Namespace.Normalize("image talk:", 7));
            
            Assert.AreEqual("user:", Namespace.Normalize("user :", 7), "only changes colon for incorrect namespace number");
            Assert.AreEqual("user:", Namespace.Normalize("user:", 7), "only changes colon for incorrect namespace number");
        }

        [Test]
        public void Verify()
        {
            Assert.IsTrue(Namespace.VerifyNamespaces(Variables.CanonicalNamespaces));

            var ns = new Dictionary<int, string>(Variables.CanonicalNamespaces);
            
            ns[0] = "";
            Assert.IsFalse(Namespace.VerifyNamespaces(ns));

            ns.Remove(0);
            ns[1] = "Talk";
            Assert.IsFalse(Namespace.VerifyNamespaces(ns));
            
            ns.Remove(1);
            Assert.IsFalse(Namespace.VerifyNamespaces(ns));
        }
    }

    [TestFixture]
    public class ConcurrencyTests : RequiresInitialization
    {
        [Test]
        public void Parser()
        {
            Parsers p1 = new Parsers();
            Parsers p2 = new Parsers();

            // http://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Bugs/Archive_10#NullReferenceException_in_HideText.AddBackMore
            string s1 = p1.HideText("<pre>foo bar</pre>");
            string s2 = p2.HideText("<source>quux</source>");
            Assert.AreEqual("<pre>foo bar</pre>", p1.AddBackText(s1));
            Assert.AreEqual("<source>quux</source>", p2.AddBackText(s2));

            // in the future, we may use parser objects for processing several wikis at once
            //Assert.AreNotEqual(p1.StubMaxWordCount, p2.StubMaxWordCount);

        }
    }
    
    [TestFixture]
    public class TalkHeaderTests : RequiresInitialization
    {
        const string articleTextHeader = @"{{talkheader|noarchive=no}}
[[Category:Foo bar]]";
        
        [Test]
        public void MoveTalkHeader()
        {
            string talkheader = @"{{talk header|noarchive=no|search=no|arpol=no|wp=no|disclaimer=no|shortcut1|shortcut2}}", talkrest = @"==hello==
hello talk";
            string articleText = talkrest + "\r\n" + talkheader;
            
            TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
            
            Assert.AreEqual(talkheader.Replace("{{talk", @"{{Talk") + "\r\n" + talkrest + "\r\n", articleText);
            
            // handles {{talk header}} on same line as other template
            string WPBS = @"{{WikiProjectBannerShell|blp=yes|1=
{{OH-Project|class=B|importance=Low|nested=yes}}
{{WPBiography|living=yes|class=B|priority=Low|filmbio-work-group=yes|nested=yes|listas=Parker, Sarah Jessica}}
{{WikiProject Cincinnati|class=B|importance=mid|nested=yes}}
}}", rest = "\r\n" +  @"==Song Jessie by Joshua Kadison==
In the article it says that above mentioned";
            articleText = WPBS + @"{{Talkheader}}" + rest;
            
            TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
            
            Assert.AreEqual(@"{{Talk header}}" + "\r\n" + WPBS + rest, articleText);
            
            // no change if already at top
            articleText = talkheader + "\r\n" + talkrest;
            
            TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
            Assert.AreEqual(talkheader + "\r\n" + talkrest, articleText);
            
            // no change if no talk header
            articleText = talkrest;
            
            TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
            Assert.AreEqual(talkrest, articleText);
        }
        
        [Test]
        public void RenameTalkHeader()
        {
            string talkheader = @"{{talkheader|noarchive=no}}", talkrest = @"==hello==
hello talk";
            string articleText = talkrest + "\r\n" + talkheader;
            
            TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
            
            Assert.AreEqual(@"{{Talk header|noarchive=no}}" + "\r\n" + talkrest+ "\r\n", articleText, "renamed to upper case with space");
        }
        
        [Test]
        public void TalkHeaderDefaultsortChange()
        {
            string start = @"
==Foo==
bar", df = @"{{DEFAULTSORT:Bert}}";
            
            string articleText = start + "\r\n" + df;
            
            TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.MoveToBottom);
            Assert.AreEqual(start + "\r\n"+ "\r\n" + df, articleText);
            
            articleText = start + df;
            
            TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.MoveToTop);
            Assert.AreEqual(df + "\r\n" + start, articleText);
            
            articleText = start + df;
            
            TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
            Assert.AreEqual(start + df, articleText);
            
            string df2 = @"{{DEFAULTSORT:}}";
            
            articleText = start + df2;
            
            TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.MoveToBottom);
            Assert.AreEqual(start, articleText, "defaultsort with no key removed");
            
            articleText = start + df2;
            
            TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.MoveToTop);
            Assert.AreEqual(start, articleText, "defaultsort with no key removed");
        }
        
        [Test]
        public void SkipToTalk()
        {
            string articleText = @"{{Skiptotoc}}", STT = @"{{Skip to talk}}";
            
            TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
            Assert.AreEqual(STT + "\r\n", articleText);
            
            articleText = @"{{skiptotoctalk}}";
            
            TalkPageFixes.ProcessTalkPage(ref articleText, DEFAULTSORT.NoChange);
            Assert.AreEqual(STT + "\r\n", articleText);
        }
        
        [Test]
        public void AddMissingFirstCommentHeader()
        {
            const string comment = @"
Hello world comment.";
            string articleTextIn = articleTextHeader + comment;
            
            // plain comment
            TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);
            
            Assert.AreEqual(articleTextIn, articleTextHeader + "\r\n" + @"
==Untitled==
Hello world comment.");
            
            // idented comment2
            articleTextIn = articleTextHeader + @"
*Hello world comment2.";
            
            TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);
            
            Assert.AreEqual(articleTextIn, articleTextHeader +"\r\n" + @"
==Untitled==
*Hello world comment2.");
            
            // idented comment3
            articleTextIn = articleTextHeader + @"
:Hello world comment3.";
            
            TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);
            
            Assert.AreEqual(articleTextIn, articleTextHeader + "\r\n" + @"
==Untitled==
:Hello world comment3.");
            
            // quoted comment
            articleTextIn = articleTextHeader + @"
""Hello world comment4"".";
            
            TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);
            
            Assert.AreEqual(articleTextIn, articleTextHeader + "\r\n" + @"
==Untitled==
""Hello world comment4"".");
            
            // heading level 3 changed to level 2
            articleTextIn = articleTextHeader + "\r\n" + @"===Foo bar===
*Hello world comment2.";
            
            TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);
            
            Assert.IsTrue(articleTextIn.Contains(@"==Foo bar=="));
            Assert.IsFalse(articleTextIn.Contains(@"==Untitled=="));
        }
        
        [Test]
        public void AddMissingFirstCommentHeaderNoChanges()
        {
            // no change – header already
            string articleTextIn = articleTextHeader + @"
==Question==
:Hello world comment3.";
            
            
            TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);
            
            Assert.AreEqual(articleTextIn, articleTextHeader + @"
==Question==
:Hello world comment3.");
            
            // no change – already header at top
            articleTextIn = @"
{{Some template}}
==Question==
:Hello world comment3.";
            
            
            TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);
            
            Assert.AreEqual(@"
{{Some template}}
==Question==
:Hello world comment3.", articleTextIn);
            
            // no change – already header at top 2
            articleTextIn = @"
==Question==
{{Some template}}
:Hello world comment3.";
            
            TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);
            
            Assert.AreEqual(@"
==Question==
{{Some template}}
:Hello world comment3.", articleTextIn);
            
            // no change – no comments
            articleTextIn = @"
==Question==
{{Some template}}";
            
            TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);
            
            Assert.AreEqual(@"
==Question==
{{Some template}}", articleTextIn);
            
            // no change – only text in template
            articleTextIn = @"
{{foo|
bar|
end}}";
            
            TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);
            
            Assert.AreEqual(@"
{{foo|
bar|
end}}", articleTextIn);
            
            // no change – only comments
            articleTextIn = @"
<!--
foo
-->";
            
            TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);
            
            Assert.AreEqual(@"
<!--
foo
-->", articleTextIn);
            
            // no change – only TOC
            articleTextIn = @"
__TOC__";
            
            TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);
            
            Assert.AreEqual(@"
__TOC__", articleTextIn);
            
            // no change -- only in template
            const string allInTemplate = @"{{archive box|
*[[/Archive: GA review|Good Article review]]}}

== Explanation of Wright's work in ''Certaine Errors'' ==";
            
            articleTextIn = allInTemplate;
            
            TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);
            
            Assert.AreEqual(allInTemplate, articleTextIn);
            
            // no change -- only after template on same line
            // http://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Bugs#Section_header_added_in_wrong_position
            const string allAfterTemplate = @"{{archive box|words}} extra foo

{{another one}}";
            
            articleTextIn = allAfterTemplate;
            
            TalkPageFixes.ProcessTalkPage(ref articleTextIn, DEFAULTSORT.NoChange);
            
            Assert.AreEqual(allAfterTemplate, articleTextIn);
        }
        
        [Test]
        public void WikiProjectBannerShellRedirects()
        {
            string red1 = @"{{WPBS}}", WikiProjectBannerShell = @"{{WikiProjectBannerShell}}";
            
            Assert.AreEqual(WikiProjectBannerShell, TalkPageFixes.WikiProjectBannerShell(red1));
        }
        
        [Test]
        public void WikiProjectBannerShellDupeParameters()
        {
            Assert.AreEqual(@"{{WikiProjectBannerShell|blp=yes}}", TalkPageFixes.WikiProjectBannerShell(@"{{WikiProjectBannerShell|blp=yes|blp=yes}}"));
            Assert.AreEqual(@"{{WikiProjectBannerShell|blpo=yes|blp=yes}}", TalkPageFixes.WikiProjectBannerShell(@"{{WikiProjectBannerShell|blpo=yes|blpo=yes|blp=yes|blp=yes}}"));
            Assert.AreEqual(@"{{WikiProjectBannerShell|collapsed=yes}}", TalkPageFixes.WikiProjectBannerShell(@"{{WikiProjectBannerShell|collapsed=yes|collapsed=yes|collapsed=yes}}"));
        }
        
        [Test]
        public void WikiProjectBannerShellUnneededParams()
        {
            Assert.AreEqual(@"{{WikiProjectBannerShell}}", TalkPageFixes.WikiProjectBannerShell(@"{{WikiProjectBannerShell|blp=no|activepol=no|collapsed=no|blpo=no}}"));
        }
        
        [Test]
        public void WikiProjectBannerShellWPBiography()
        {
            Assert.AreEqual(@"{{WikiProjectBannerShell|1={{WPBiography|foo=bar}}}}", TalkPageFixes.WikiProjectBannerShell(@"{{WikiProjectBannerShell|1={{WPBiography|foo=bar}}}}"));
            
            Assert.AreEqual(@"{{WikiProjectBannerShell|blp=yes|1={{WPBiography|foo=bar|living=yes}}}}", TalkPageFixes.WikiProjectBannerShell(@"{{WikiProjectBannerShell|blp=|1={{WPBiography|foo=bar|living=yes}}}}"),"appends blp=yes to WPBS");
            Assert.AreEqual(@"{{WikiProjectBannerShell|blp=yes|1={{WikiProject Biography|foo=bar|living=yes}}}}", TalkPageFixes.WikiProjectBannerShell(@"{{WikiProjectBannerShell|blp=|1={{WikiProject Biography|foo=bar|living=yes}}}}"),"appends blp=yes to WPBS");
            Assert.AreEqual(@"{{WikiProjectBannerShell|activepol=yes|1={{WPBiography|foo=bar|activepol=yes}}}}", TalkPageFixes.WikiProjectBannerShell(@"{{WikiProjectBannerShell|activepol=abc|1={{WPBiography|foo=bar|activepol=yes}}}}"),"ignores invalid values");
            Assert.AreEqual(@"{{WikiProjectBannerShell|blpo=yes|1={{WPBiography|foo=bar|blpo=yes}}}}", TalkPageFixes.WikiProjectBannerShell(@"{{WikiProjectBannerShell|blpo=|1={{WPBiography|foo=bar|blpo=yes}}}}"),"appends blpo=yes to WPBS");
            Assert.AreEqual(@"{{WikiProjectBannerShell|blpo=|1={{WPBiography|foo=bar|blpo=no}}}}", TalkPageFixes.WikiProjectBannerShell(@"{{WikiProjectBannerShell|blpo=|1={{WPBiography|foo=bar|blpo=no}}}}"));
            
            Assert.AreEqual(@"{{WikiProjectBannerShell|1={{WPBiography|foo=bar|living=no}}}}", TalkPageFixes.WikiProjectBannerShell(@"{{WikiProjectBannerShell|blp=yes|1={{WPBiography|foo=bar|living=no}}}}"));
        }
        
        [Test]
        public void WikiProjectBannerShellAddingWikiProjects()
        {
            Assert.AreEqual(@"{{WikiProjectBannerShell|1={{WPBiography|foo=bar}}
{{WikiProject foo}}}}
", TalkPageFixes.WikiProjectBannerShell(@"{{WikiProjectBannerShell|1={{WPBiography|foo=bar}}}}
{{WikiProject foo}}"), "WikiProjects pulled into WPBS");

            Assert.AreEqual(@"{{WikiProjectBannerShell|1={{WPBiography|foo=bar}}
{{WikiProject foo}}
{{WikiProject bar}}}}

", TalkPageFixes.WikiProjectBannerShell(@"{{WikiProjectBannerShell|1={{WPBiography|foo=bar}}}}
{{WikiProject foo}}
{{WikiProject bar}}"), "WikiProjects pulled into WPBS");

            Assert.AreEqual(@"{{WikiProjectBannerShell|1={{WikiProject foo}}
{{WikiProject bar}}
{{WikiProject Biography|living=yes|foo=bar}} | blp=yes}}", TalkPageFixes.WikiProjectBannerShell(@"{{WikiProject Biography|living=yes|foo=bar}}{{WikiProjectBannerShell|1={{WikiProject foo}}
{{WikiProject bar}}}}"), "WikiProjects pulled into WPBS, WPBIO contains living=yes");

            Assert.AreEqual(@"{{WikiProjectBannerShell|1={{WikiProject foo}}
{{WikiProject bar}}
{{WikiProject Biography|activepol=yes|foo=bar}} | activepol=yes}}", TalkPageFixes.WikiProjectBannerShell(@"{{WikiProject Biography|activepol=yes|foo=bar}}{{WikiProjectBannerShell|1={{WikiProject foo}}
{{WikiProject bar}}}}"), "WikiProjects pulled into WPBS, WPBIO contains activepol=yes");

            Assert.AreEqual(@"{{WikiProjectBannerShell|1={{WikiProject foo}}
{{WikiProject bar}}
{{WikiProject Biography|living=yes|activepol=yes|foo=bar}} | blp=yes | activepol=yes}}", TalkPageFixes.WikiProjectBannerShell(@"{{WikiProject Biography|living=yes|activepol=yes|foo=bar}}{{WikiProjectBannerShell|1={{WikiProject foo}}
{{WikiProject bar}}}}"), "WikiProjects pulled into WPBS, WPBIO contains living, activepol=yes");

        }
        
        [Test]
        public void WikiProjectBannerShellUnnamedParam()
        {
            Assert.AreEqual(@"{{WikiProjectBannerShell|1={{WPBiography|foo=bar}}}}", TalkPageFixes.WikiProjectBannerShell(@"{{WikiProjectBannerShell|{{WPBiography|foo=bar}}}}"), "1= added when missing");
            Assert.AreEqual(@"{{WikiProjectBannerShell|1=
{{WPBiography|foo=bar}}}}", TalkPageFixes.WikiProjectBannerShell(@"{{WikiProjectBannerShell|
{{WPBiography|foo=bar}}}}"));
            
            const string otherUnnamed = @"{{WikiProjectBannerShell|random}}";
            Assert.AreEqual(otherUnnamed, TalkPageFixes.WikiProjectBannerShell(otherUnnamed), "other unknown parameter not named 1=");
        }
        
        [Test]
        public void WikiProjectBannerShellBLP()
        {
            const string a = @"{{WikiProjectBannerShell|blp=yes|1={{WPBiography|foo=bar|living=yes}}}}";
            
            Assert.AreEqual(a, TalkPageFixes.WikiProjectBannerShell(a + "{{Blp}}"),"removes redundant banner");
            Assert.AreEqual(a, TalkPageFixes.WikiProjectBannerShell(a.Replace("blp=yes", "blp=") + "{{Blp}}"),"empty parameter in WPBS");
            Assert.AreEqual("{{Blp}}", TalkPageFixes.WikiProjectBannerShell("{{Blp}}"));
        }
        
        [Test]
        public void WikiProjectBannerShellBLPO()
        {
            const string a = @"{{WikiProjectBannerShell|blpo=yes|1={{WPBiography|foo=bar}}}}";
            
            Assert.AreEqual(a, TalkPageFixes.WikiProjectBannerShell(a + "{{Blpo}}"),"removes redundant banner");
            Assert.AreEqual(a, TalkPageFixes.WikiProjectBannerShell(a + "{{BLPO}}"),"removes redundant banner");
            Assert.AreEqual(a, TalkPageFixes.WikiProjectBannerShell(a + "{{BLP others}}"),"removes redundant banner");
            Assert.AreEqual(a, TalkPageFixes.WikiProjectBannerShell(a.Replace("blpo=yes", "blpo=") + "{{Blpo}}"),"empty parameter in WPBS");
            Assert.AreEqual("{{Blpo}}", TalkPageFixes.WikiProjectBannerShell("{{Blpo}}"));
        }
        
        [Test]
        public void WikiProjectBannerShellActivepol()
        {
            const string a = @"{{WikiProjectBannerShell|activepol=yes|1={{WPBiography|foo=bar|activepol=yes}}}}";
            
            Assert.AreEqual(a, TalkPageFixes.WikiProjectBannerShell(a + "{{Activepol}}"),"removes redundant banner");
            Assert.AreEqual(a, TalkPageFixes.WikiProjectBannerShell(a.Replace("activepol=yes|", "activepol=|") + "{{activepol}}"),"empty parameter in WPBS");
            Assert.AreEqual("{{activepol}}", TalkPageFixes.WikiProjectBannerShell("{{activepol}}"));
        }
        
        [Test]
        public void WikiProjectBannerShellMisc()
        {
            const string a = @"{{wpbs|1=|banner collapsed=no|
{{WPBiography|living=yes|class=Start|priority=|listas=Hill, A}}
{{WikiProject Gender Studies}}
{{WikiProject Oklahoma}}
}}", b = @"{{WikiProjectBannerShell|banner collapsed=no|1=
{{WPBiography|living=yes|class=Start|priority=|listas=Hill, A}}
{{WikiProject Gender Studies}}
{{WikiProject Oklahoma}}
| blp=yes
}}";
            Assert.AreEqual(b, TalkPageFixes.WikiProjectBannerShell(a));
        }
        
        [Test]
        public void AddWikiProjectBannerShell()
        {
            const string a = @"{{WikiProject a|text}}", b = @"{{WikiProject b|text}}", c = @"{{WikiProject c|text}}", d = @"{{WikiProject d|text}}";
            Assert.AreEqual(a, TalkPageFixes.WikiProjectBannerShell(a));
            Assert.AreEqual(a + b, TalkPageFixes.WikiProjectBannerShell(a + b));
            Assert.AreEqual(a + b + c, TalkPageFixes.WikiProjectBannerShell(a + b + c));
            Assert.AreEqual(@"{{WikiProjectBannerShell|1=" + "\r\n" + a + "\r\n" + b + "\r\n" + c + "\r\n" + d + "\r\n" + @"}}", TalkPageFixes.WikiProjectBannerShell(a + b + c + d));
            
            const string e = @"{{talk header}}
{{WikiProject Biography|listas=Bar, F|living=yes|class=Start|priority=mid|sports-work-group=yes}}
{{WikiProject F|class=Start|importance=mid|italy=y}}
{{WikiProject X|class=Start|importance=Mid}}
{{WikiProject D|class=Start|importance=Low}}", f = @"{{WikiProjectBannerShell|1=
{{WikiProject Biography|listas=Bar, F|living=yes|class=Start|priority=mid|sports-work-group=yes}}
{{WikiProject F|class=Start|importance=mid|italy=y}}
{{WikiProject X|class=Start|importance=Mid}}
{{WikiProject D|class=Start|importance=Low}}
| blp=yes
}}{{talk header}}



";
            
            Assert.AreEqual(f, TalkPageFixes.WikiProjectBannerShell(e), "adds WPBS, ignores non-wikiproject templates");
        }
        
        [Test]
        public void RemoveTemplateNamespace()
        {
            string T1 = @"{{Template:Foo}}", T2 = @"{{Template:Foo}}
==Section==
{{Template:Bar}}";
            
            Assert.IsFalse(TalkPageFixes.ProcessTalkPage(ref T1, DEFAULTSORT.NoChange));
            Assert.AreEqual("{{Foo}}", T1, "template namespace removed");
            
            Assert.IsFalse(TalkPageFixes.ProcessTalkPage(ref T2, DEFAULTSORT.NoChange));
            Assert.AreEqual(@"{{Foo}}
==Section==
{{Template:Bar}}", T2, "changes only made in zeroth section");
        }
        
        [Test]
        public void WPBiographyTopBilling()
        {
            string a = @"{{WPBiography|foo=yes|living=yes}}
{{WikiProject London}}
",  b = @"{{WikiProjectBannerShell|banner collapsed=no|1=
{{WPBiography|living=yes|class=Start|priority=|listas=Hill, A}}
{{WikiProject Gender Studies}}
{{WikiProject Oklahoma}}
| blp=yes
}}", c = @"{{WPBiography|foo=yes|living=no}}
{{WikiProject London}}";
            Assert.AreEqual(a, TalkPageFixes.WPBiography(@"{{WikiProject London}}
{{WPBiography|foo=yes|living=yes}}"), "WPBiography moved above WikiProjects");
            Assert.AreEqual(a, TalkPageFixes.WPBiography(a), "no change when WPBiography ahead of WikiProjects");
            Assert.AreEqual(b, TalkPageFixes.WPBiography(b), "no change when WPBS present");
            Assert.AreEqual(c, TalkPageFixes.WPBiography(c), "no change when not living");
            Assert.AreEqual(c + @"{{blp}}", TalkPageFixes.WPBiography(c + @"{{blp}}"), "no change when not living");
            
            Assert.AreEqual(a, TalkPageFixes.WPBiography(a + @"{{blp}}"), "blp template removed when living=y");
        }
        
        [Test]
        public void WPBiography()
        {
            string a = @"{{WPBiography|foo=yes|living=yes|listas=Foé}}";
            
            Assert.AreEqual(a.Replace(@"é","e"), TalkPageFixes.WPBiography(a), "diacritics removed from WPBiography listas");
            Assert.AreEqual(a.Replace(@"é","e"), TalkPageFixes.WPBiography(a.Replace(@"é","e")), "no change when no diacritics in WPBiography listas");
        }
        
        [Test]
        public void WPBiographyBLPActivepol()
        {
            string a = @"{{WPBiography}}";
            
            Assert.AreEqual(a.Replace(@"}}"," | living=yes}}"), TalkPageFixes.WPBiography(a + @"{{blp}}"), "Add blp to WPBiography");
            Assert.AreEqual(a.Replace(@"}}"," |living=yes}}"), TalkPageFixes.WPBiography(a.Replace(@"}}"," |living=}}") + @"{{blp}}"), "Add value to empty parameter");
            Assert.AreEqual(a.Replace(@"}}","|living=no}}" + @"{{blp}}"), TalkPageFixes.WPBiography(a.Replace(@"}}","|living=no}}") + @"{{blp}}"), "No change if blp=yes and living=no");
            Assert.AreEqual(a.Replace(@"}}"," | living=yes | activepol=yes | politician-work-group=yes}}"), TalkPageFixes.WPBiography(a + @"{{activepol}}"), "Add activepol to WPBiography");
            Assert.AreEqual(a.Replace(@"}}"," | living=yes | activepol=yes | politician-work-group=yes}}"), TalkPageFixes.WPBiography(a + @"{{active politician}}"), "Add activepol via redirect to WPBiography");
            Assert.AreEqual(a.Replace(@"}}"," |activepol= yes| living=yes | politician-work-group=yes}}"), TalkPageFixes.WPBiography(a.Replace(@"}}"," |activepol=}}") + @"{{activepol}}"), "Add value to empty parameter");
            Assert.AreEqual(a.Replace(@"}}"," |activepol=yes | living=yes | politician-work-group=yes}}"), TalkPageFixes.WPBiography(a.Replace(@"}}"," |activepol=no}}") + @"{{activepol}}"), "Change value to activepol no nonsense parameter");
            Assert.AreEqual(a.Replace(@"}}"," | living=yes | activepol=yes | politician-work-group=yes}}"), TalkPageFixes.WPBiography(a + @"{{blp}}{{activepol}}"), "Add activepol and blp to WPBiography");
        }

        [Test]
        public void WPSongs()
        {
            string a = @"{{WPSongs}}";

            Assert.AreEqual(a.Replace(@"}}"," | needs-infobox=yes}}"), TalkPageFixes.WPSongs(a + @"{{sir}}"), "Add sir to WPSongs");
            Assert.AreEqual(a.Replace(@"}}"," | needs-infobox=yes}}"), TalkPageFixes.WPSongs(a.Replace(@"}}"," | needs-infobox=}}" + @"{{sir}}")), "Add sir to WPSongs with empty parameter");
            Assert.AreEqual(a.Replace(@"}}"," | needs-infobox=yes}}"), TalkPageFixes.WPSongs(a.Replace(@"}}"," | needs-infobox=}}" + @"{{Single infobox request}}")), "Add Single infobox request to WPSongs with empty parameter");
            Assert.AreEqual(a.Replace(@"}}"," | needs-infobox=yes}}"), TalkPageFixes.WPSongs(a.Replace(@"}}"," | needs-infobox=yes}}") + @"{{sir}}"), "Remove sir when WPSongs with needs-infobox=yes exists");
            Assert.AreEqual(a, TalkPageFixes.WPSongs(a), "Do nothing");
            Assert.AreEqual(a, TalkPageFixes.WPSongs(a.Replace(@"}}","|importance=yes}}")), "Remove importance");
            Assert.AreEqual(a, TalkPageFixes.WPSongs(a.Replace(@"}}","|needs-infobox=no}}")), "Remove needs-infobox=no");
            Assert.AreEqual(a, TalkPageFixes.WPSongs(a.Replace(@"}}","|importance=yes|needs-infobox=no}}")), "Remove importance and needs-infobox=no");

            string b = @"{{WikiProject Songs}}";

            Assert.AreEqual(b.Replace(@"}}"," | needs-infobox=yes}}"), TalkPageFixes.WPSongs(b + @"{{sir}}"), "Add sir to WPSongs");
            Assert.AreEqual(b.Replace(@"}}"," | needs-infobox=yes}}"), TalkPageFixes.WPSongs(b.Replace(@"}}"," | needs-infobox=}}" + @"{{sir}}")), "Add sir to WPSongs with empty parameter");
            Assert.AreEqual(b.Replace(@"}}"," | needs-infobox=yes}}"), TalkPageFixes.WPSongs(b.Replace(@"}}"," | needs-infobox=}}" + @"{{Single infobox request}}")), "Add Single infobox request to WPSongs with empty parameter");
            Assert.AreEqual(b.Replace(@"}}"," | needs-infobox=yes}}"), TalkPageFixes.WPSongs(b.Replace(@"}}"," | needs-infobox=yes}}") + @"{{sir}}"), "Remove sir when WPSongs with needs-infobox=yes exists");
            Assert.AreEqual(b, TalkPageFixes.WPSongs(b), "Do nothing");
        }

    }
    
    [TestFixture]
    public class InTemplateRuleTests
    {
        [Test]
        public void TemplateUsedInText()
        {
            // casing
            Assert.IsTrue(InTemplateRule.TemplateUsedInText("Bert", @"Bert}} was great"));
            Assert.IsTrue(InTemplateRule.TemplateUsedInText("Bert", @"bert}} was great"));
            Assert.IsTrue(InTemplateRule.TemplateUsedInText("bert", @"Bert}} was great"));
            Assert.IsTrue(InTemplateRule.TemplateUsedInText("bert", @"bert}} was great"));
            
            //spacing
            Assert.IsTrue(InTemplateRule.TemplateUsedInText("Bert", @"Bert }} was great"));
            Assert.IsTrue(InTemplateRule.TemplateUsedInText("Bert", @" Bert}} was great"));
            Assert.IsTrue(InTemplateRule.TemplateUsedInText("Bert", @"Bert|fo=yes}} was great"));
            Assert.IsTrue(InTemplateRule.TemplateUsedInText("Bert", @"Bert
|fo=yes}} was great"));
            Assert.IsTrue(InTemplateRule.TemplateUsedInText("Bert", @"Bert |fo=yes}} was great"));
            Assert.IsTrue(InTemplateRule.TemplateUsedInText("Bert", @" Bert|fo=yes}} was great"));
            Assert.IsTrue(InTemplateRule.TemplateUsedInText("Bert", @"
    Bert|fo=yes}} was great"));
            
            //comments
            Assert.IsTrue(InTemplateRule.TemplateUsedInText("Bert", @"Bert|fo=yes}} was <!--great-->"));
            Assert.IsTrue(InTemplateRule.TemplateUsedInText("Bert", @"<!--thing--> Bert }} was great"));
            Assert.IsTrue(InTemplateRule.TemplateUsedInText("Bert", @"Bert<!--thing-->}} was great"));
            Assert.IsTrue(InTemplateRule.TemplateUsedInText("Bert", @"Bert<!--thing-->|foo=bar}} was great"));
            
            Assert.IsTrue(InTemplateRule.TemplateUsedInText("", @"Bert}} was great"));
            
            // underscores
            Assert.IsTrue(InTemplateRule.TemplateUsedInText("Bert Li", @"Bert Li}} was great"));
            Assert.IsTrue(InTemplateRule.TemplateUsedInText("Bert Li", @"Bert   Li}} was great"));
            Assert.IsTrue(InTemplateRule.TemplateUsedInText("Bert Li", @"Bert_Li}} was great"));
        }
        
        [Test]
        public void TemplateUsedInTextNoMatches()
        {
            Assert.IsFalse(InTemplateRule.TemplateUsedInText("Bert", @"{{Bert}} was great"));
            Assert.IsFalse(InTemplateRule.TemplateUsedInText("Bert", @"Bert was great"));
            Assert.IsFalse(InTemplateRule.TemplateUsedInText("Bert", @"[[Bert]] was great"));
            Assert.IsFalse(InTemplateRule.TemplateUsedInText("Bert", @"Tim}} was great"));
            Assert.IsFalse(InTemplateRule.TemplateUsedInText("Bert", @"BERT}} was great"));
            Assert.IsFalse(InTemplateRule.TemplateUsedInText("Bert Li", @"BertLi}} was great"));
            
            Assert.IsFalse(InTemplateRule.TemplateUsedInText("Bert", @"<!--Bert}}--> was great"));
        }
    }
    
    [TestFixture]
    public class ListMakerTests : RequiresInitialization
    {
        private readonly ListMaker LMaker = new ListMaker();
        
        [Test]
        public void NormalizeTitle()
        {
            Assert.AreEqual("Foo", LMaker.NormalizeTitle(@"http://en.wikipedia.org/w/index.php?title=Foo&diff=3&oldid=4"));
            Assert.AreEqual("Health effects of chocolate", LMaker.NormalizeTitle(@"http://en.wikipedia.org/w/index.php?title=Health_effects_of_chocolate&diff=4018&oldid=40182"));
            Assert.AreEqual("Foo", LMaker.NormalizeTitle(@"http://en.wikipedia.org/w/index.php?title=Foo&action=history"));
            Assert.AreEqual("Foo", LMaker.NormalizeTitle(@"http://en.wikipedia.org/w/index.php?title=Foo&oldid=5"));
            Assert.AreEqual("Science (journal)", LMaker.NormalizeTitle(@"http://en.wikipedia.org/w/index.php?title=Science%20%28journal%29&action=history"));
            
            Assert.AreEqual("Foo", LMaker.NormalizeTitle(@"   Foo"), "cleans spacing from Firefox Category paste");
        }
        
        [Test]
        public void NormalizeTitleSecure()
        {
            Assert.AreEqual("Foo", LMaker.NormalizeTitle(@"https://secure.wikimedia.org/wikipedia/en/wiki/Foo"));
            Assert.AreEqual("Foo", LMaker.NormalizeTitle(@"https://secure.wikimedia.org/wikipedia/en/w/index.php?title=Foo&oldid=41"));
        }
    }
}
