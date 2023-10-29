using OpenMud.Mudpiler.Compiler.DmlPreprocessor.Util;

namespace OpenMud.Mudpiler.Tests.PreprocessorTest
{
    public class TextProcessingTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestBasicMasking()
        {
            var testString = "//Hello\n'World'\nthis is a /*test*/ string\nThis is a \"string!\" hmm";
            var testMasking = new DocumentCharacterMasking(testString);

            Assert.IsTrue(testMasking.CommentsAndStrings.Count() == testMasking.Resources.Count() && testMasking.CommentsAndStrings.Count() == testString.Length);
            var commentText = new string(testString.Zip(testMasking.CommentsAndStrings).Select(a => a.Second ? a.First : '.').ToArray());

            Assert.IsTrue(commentText == "//Hello\n................../*test*/..................\"string!\"....");

            return;
        }

        [Test]
        public void TestMaskInsertion()
        {
            var testString = "//Hello\n'World'\nthis is a /*test*/ string\nThis is a \"string!\" hmm";
            var testMasking = new DocumentCharacterMasking(testString);

            var insertString = "INSERTED VALUE";
            testString = testString.Insert(9, insertString);

            testMasking.Insert(9, insertString.Length, isResource: true);

            var commentText = new string(testString.Zip(testMasking.CommentsAndStrings).Select(a => a.Second ? a.First : '.').ToArray());
            var rsrcText = new string(testString.Zip(testMasking.Resources).Select(a => a.Second ? a.First : '.').ToArray());

            Assert.IsTrue(commentText == "//Hello\n................................/*test*/..................\"string!\"....");
            Assert.IsTrue(rsrcText == "........'INSERTED VALUEWorld'..................................................");

            return;
        }

        [Test]
        public void TestAllow()
        {
            var testString = "//Hello\n'World'\nthis is a /*test*/ string\nThis is a \"string!\" hmm";
            var testMasking = new DocumentCharacterMasking(testString);

            Assert.IsFalse(testMasking.Accept(0, 1));
            Assert.IsFalse(testMasking.Accept(7, 1));
            Assert.IsTrue(testMasking.Accept(8, 1, true));
            Assert.IsFalse(testMasking.Accept(8, 1, false));
            Assert.IsTrue(testMasking.Accept(14, 1, true));
            Assert.IsFalse(testMasking.Accept(14, 1, false));


            Assert.IsFalse(testMasking.Accept(26, 1, true));
            Assert.IsFalse(testMasking.Accept(33, 1, true));

            Assert.IsTrue(testMasking.Accept(34, 1, true));
            return;
        }

        [Test]
        public void TestDocumentAllow()
        {
            var testDocumentSrc =
                @"this is not a comment
//this is a comment
and /*this*/ is a comment
but 'this is a resource' and
";
            var document = SourceFileDocument.Create("testfile.dml", 0, testDocumentSrc);

            Assert.IsTrue(document.AllowReplace(0, 10, false));
            Assert.IsTrue(document.AllowReplace(14, 9, false));
            Assert.IsFalse(document.AllowReplace(14, 10, false));


            Assert.IsFalse(document.AllowReplace(23, 1, false));
            Assert.IsFalse(document.AllowReplace(23, 19, false));

            Assert.IsFalse(document.AllowReplace(43, 1, false));
            Assert.IsTrue(document.AllowReplace(44, 1, false));

            Assert.IsTrue(document.AllowReplace(47, 1, false));
            Assert.IsFalse(document.AllowReplace(48, 1, false));
            Assert.IsFalse(document.AllowReplace(48, 8, false));
            Assert.IsFalse(document.AllowReplace(48, 10, false));
            Assert.IsTrue(document.AllowReplace(56, 1, false));


            Assert.IsTrue(document.AllowReplace(75, 5, true));
            Assert.IsFalse(document.AllowReplace(75, 5, false));

        }

        private void TestDocuemnt(SourceFileDocument doc)
        {
            var raw = doc.AsPlainText(false);

            for (var i = 0; i < raw.Length; i++)
            {
                var c = raw[i];
                if (c == 'a')
                    Assert.IsTrue(doc.AllowReplace(i, 1, false));
                else if (c == 'r')
                {
                    Assert.IsFalse(doc.AllowReplace(i, 1, false));
                    Assert.IsTrue(doc.AllowReplace(i, 1, true));
                } else if (c == 'x')
                {
                    Assert.IsFalse(doc.AllowReplace(i, 1, false));
                }
            }
        }

        [Test]
        public void TestDocumentInsertSingleOrigin()
        {
            char allow = 'a';
            char allowResource = 'r';
            char disallow = 'x';

            var testDocumentSrc =
                @"aaaaaaaaaaaaaaa
//xxxxxxxxxxxx
aaa /*xxxxx*/ aa a aaaaaaa
aaa 'rrrrrrrrrrrrrr' aaaa
";
            var doc = SourceFileDocument.Create("test.dml", 0, testDocumentSrc);
            var docRaw = doc.AsPlainText(false);

            var testInsert = "aaaaaaaa";
            docRaw = docRaw.Insert(0, testInsert);
            doc.Rewrite(0, 0, testInsert);

            Assert.IsTrue(doc.AsPlainText(false) == docRaw);

            TestDocuemnt(doc);
        }

        [Test]
        public void TestDocumentInsertMultiLineOriginAndPrepend()
        {
            char allow = 'a';
            char allowResource = 'r';
            char disallow = 'x';

            var testDocumentSrc =
                @"aaaaaaaaaaaaaaa
//xxxxxxxxxxxx
aaa /*xxxxx*/ aa a aaaaaaa
aaa 'rrrrrrrrrrrrrr' aaaa
";
            var doc = SourceFileDocument.Create("test.dml", 0, testDocumentSrc);
            var docRaw = doc.AsPlainText(false);

            var testInsert = "aaaaaaaa\r\naaaaaa\r\naaaaaaa";
            docRaw = docRaw.Insert(0, testInsert);
            doc.Rewrite(0, 0, testInsert);

            Assert.IsTrue(doc.AsPlainText(false) == docRaw);

            TestDocuemnt(doc);
        }

        [Test]
        public void TestDocumentInsertSingleLineOrigin()
        {
            char allow = 'a';
            char allowResource = 'r';
            char disallow = 'x';

            var testDocumentSrc =
                @"aaaaaaaaaaaaaaa
//xxxxxxxxxxxx
aaa /*xxxxx*/ aa a aaaaaaa
aaa 'rrrrrrrrrrrrrr' aaaa
";
            var doc = SourceFileDocument.Create("test.dml", 0, testDocumentSrc);
            var docRaw = doc.AsPlainText(false);

            var testInsert = "aaaaaaaaaaaaaa\r\n";
            docRaw = docRaw.Insert(0, testInsert);
            doc.Rewrite(0, 0, testInsert);

            Assert.IsTrue(doc.AsPlainText(false) == docRaw);

            TestDocuemnt(doc);
        }
    }
}