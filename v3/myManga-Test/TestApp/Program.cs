using System;
using Core.IO;
using myMangaSiteExtension.Objects;
using myMangaSiteExtension;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            String s = String.Empty;
            for (int i = 0; i <= 1000; ++i)
                s += i.ToString();
            try
            {
                TestData binTest = new TestData();
                binTest.TestString = "Hello World BINARY" + s;
                binTest.TestUInt32 = 123456;
                Console.WriteLine("Testing BINARY...");
                Console.WriteLine("\tSave...");
                binTest.SaveObject("Test.bin");
                binTest.SaveToArchive("Test.bin.zip", "Test.bin");
                Console.WriteLine("\tLoad...");
                TestData binTest2 = new TestData().LoadObject("Test.bin");
                Console.WriteLine(String.Format("BINARY: {0}->{1}", binTest2.TestString, binTest2.TestUInt32));
                TestData binTest3 = new TestData().LoadFromArchive("Test.bin.zip", "Test.bin");
                Console.WriteLine(String.Format("BINARY: {0}->{1}", binTest3.TestString, binTest3.TestUInt32));
            }
            catch (Exception ex)
            {
                Console.WriteLine("BINARY Test failed...");
                Console.WriteLine(ex.ToString());
            }

            try
            {
                TestData xmlTest = new TestData();
                xmlTest.TestString = "Hello World XML" + s;
                xmlTest.TestUInt32 = 123457;
                Console.WriteLine("Testing XML...");
                Console.WriteLine("\tSave...");
                xmlTest.SaveObject("Test.xml", SaveType.XML);
                xmlTest.SaveToArchive("Test.xml.zip", "Test.xml", SaveType.XML);
                Console.WriteLine("\tLoad...");
                TestData xmlTest2 = new TestData().LoadObject("Test.xml", SaveType.XML);
                Console.WriteLine(String.Format("xmlTest: {0}->{1}", xmlTest2.TestString, xmlTest2.TestUInt32));
                TestData xmlTest3 = new TestData().LoadFromArchive("Test.xml.zip", "Test.xml", SaveType.XML);
                Console.WriteLine(String.Format("xmlTest: {0}->{1}", xmlTest3.TestString, xmlTest3.TestUInt32));
            }
            catch (Exception ex)
            {
                Console.WriteLine("XML Test failed...");
                Console.WriteLine(ex.ToString());
            }

            try
            {
                MangaObject mangaObj = new MangaObject();
                mangaObj.Name = "Hello World!";
                mangaObj.AlternateNames.AddRange(new String[] { "Goodbye World!", "Hello World 2!" });
                mangaObj.Authors.Add("Me");
                mangaObj.Artists.Add("Me");
                mangaObj.Locations.Add(new Core.IO.KeyValuePair<String, String>(Key: "The Internet", Value: "https://google.com"));
                mangaObj.Chapters.Add(new ChapterObject(mangaObj) { Name = "Chapter 1" });
                mangaObj.Chapters[0].Locations.Add(new Core.IO.KeyValuePair<String, String>(Key: "The Internet", Value: "https://google.com"));
                mangaObj.Chapters[0].Pages.Add(new PageObject(mangaObj.Chapters[0]) { PageNumber = 0, Name = "Google" });
                mangaObj.Chapters[0].Pages[0].RemoteLocations.Add("https://youtube.com");
                mangaObj.Covers.Add("1.jpg");

                Console.WriteLine("Testing XML...");
                Console.WriteLine("\tSave...");
                mangaObj.SaveObject("mangaObj.xml", SaveType.XML);
                mangaObj.SaveToArchive("mangaObj.xml.zip", "mangaObj.xml", SaveType.XML);

                Console.WriteLine("Testing BINARY...");
                Console.WriteLine("\tSave...");
                mangaObj.SaveObject("mangaObj.bin");
                mangaObj.SaveToArchive("mangaObj.bin.zip", "mangaObj.bin");

                mangaObj = null;
                mangaObj = new MangaObject().LoadObject("mangaObj.bin");
                Console.WriteLine(mangaObj.Name);
            }
            catch (Exception ex)
            {
                Console.WriteLine("XML Test failed...");
                Console.WriteLine(ex.ToString());
            }
            Console.ReadLine();
        }
    }
}
