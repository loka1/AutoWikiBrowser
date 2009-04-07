using WikiFunctions.Parse;
using NUnit.Framework;

namespace UnitTests
{
    public class RequiresParser2 : RequiresInitialization
    {
        protected Parsers parser2;

        public RequiresParser2()
        {
            parser2 = new Parsers();
        }
    }

    [TestFixture]
    public class SorterTests : RequiresParser2
    {
        //public SorterTests()
        //{
        //Variables.SetToEnglish();
        //}

        [Test]
        public void RemoveStubs()
        {
            // shouldn't break anything
            string s = "==foo==\r\nbar<ref name=\"123\"/>{{boz}}";
            Assert.AreEqual("", MetaDataSorter.removeStubs(ref s));
            Assert.AreEqual("==foo==\r\nbar<ref name=\"123\"/>{{boz}}", s);

            // should remove stubs, but not section stubs
            s = "{{foo}}{{stub}}{{foo-stub}}bar{{sect-stub}}{{not-a-stub|123}}{{not a|stub}}";
            Assert.AreEqual("{{stub}}\r\n{{foo-stub}}\r\n", MetaDataSorter.removeStubs(ref s));
            Assert.AreEqual("{{foo}}bar{{sect-stub}}{{not-a-stub|123}}{{not a|stub}}", s);

            //shouldn't fail
            s = "";
            Assert.AreEqual("", MetaDataSorter.removeStubs(ref s));
            Assert.AreEqual("", s);
            s = "{{stub}}";
            Assert.AreEqual("{{stub}}\r\n", MetaDataSorter.removeStubs(ref s));
            Assert.AreEqual("", s);
        }

        [Test]
        public void MoveDablinksTests()
        {
            const string d = @"Fred is a doctor.
Fred has a dog.
[[Category:Dog owners]]
{{some template}}
";

            string e = @"{{otherpeople1|Fred the dancer|Fred Smith (dancer)}}";
            Assert.AreEqual(e + "\r\n" + d, MetaDataSorter.moveDablinks(d + e));

            e = @"{{For|Fred the dancer|Fred Smith (dancer)}}";
            Assert.AreEqual(e + "\r\n" + d, MetaDataSorter.moveDablinks(d + e));

            e = @"{{redirect2|Fred the dancer|Fred Smith (dancer)}}";
            Assert.AreEqual(e + "\r\n" + d, MetaDataSorter.moveDablinks(d + e));

            e = @"{{redirect2|Fred the {{dancer}}|Fred Smith (dancer)}}";
            Assert.AreEqual(e + "\r\n" + d, MetaDataSorter.moveDablinks(d + e));

            // check no change when already in correct position
            Assert.AreEqual(e + "\r\n" + d, MetaDataSorter.moveDablinks(e + "\r\n" + d));

            // don't move dablinks in a section
            string f = @"Article words
== heading ==
{{redirect2|Fred the dancer|Fred Smith (dancer)}}
words";
            Assert.AreEqual(f, MetaDataSorter.moveDablinks(f));

        }

        // {{Lifetime}} template lives after categories on en-wiki
        [Test]
        public void LifetimeTests()
        {
            string a = @"Fred is a doctor. Fred has a dog.
{{Lifetime|1922|1987|Smith, Fred}}
[[Category:Dog owners]]";
            const string b = @"[[Category:Dog owners]]
{{Lifetime|1922|1987|Smith, Fred}}
";

            Assert.AreEqual(b, parser2.Sorter.removeCats(ref a, "test"));

            string c = @"Fred is a doctor. Fred has a dog.
{{lifetime|1922|1987|Smith, Fred}}
[[Category:Dog owners]]
[[Category:Foo]]
[[Category:Bar]]";
            const string d = @"[[Category:Dog owners]]
[[Category:Foo]]
[[Category:Bar]]
{{lifetime|1922|1987|Smith, Fred}}
";

            Assert.AreEqual(d, parser2.Sorter.removeCats(ref c, "test"));

            string e = @"Fred is a doctor. Fred has a dog.
{{BIRTH-DEATH-SORT|1922|1987|Smith, Fred}}
[[Category:Dog owners]]
[[Category:Foo]]
[[Category:Bar]]";
            const string f = @"[[Category:Dog owners]]
[[Category:Foo]]
[[Category:Bar]]
{{BIRTH-DEATH-SORT|1922|1987|Smith, Fred}}
";

            Assert.AreEqual(f, parser2.Sorter.removeCats(ref e, "test"));

            // normal spacing rules apply for {{lifetime}} 1 for interwikis, two for stubs
            string g = @"{{Maroon 5}}

{{Lifetime|1979||Carmichael, Jesse}}
[[Category:American keyboardists]]
[[Category:Maroon 5]]

";

            string h = @"[[Category:American keyboardists]]
[[Category:Maroon 5]]
{{Lifetime|1979||Carmichael, Jesse}}
";

            Assert.AreEqual(h, parser2.Sorter.removeCats(ref g, "test"));
        }
    }
}
