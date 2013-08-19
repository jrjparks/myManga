using System;
using Core.IO;

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
                TestData binTest2 = "Test.bin".LoadObject<TestData>();
                Console.WriteLine(String.Format("BINARY: {0}->{1}", binTest2.TestString, binTest2.TestUInt32));
                TestData binTest3 = "Test.bin.zip".LoadFromArchive<TestData>("Test.bin");
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
                TestData xmlTest2 = "Test.xml".LoadObject<TestData>(SaveType.XML);
                Console.WriteLine(String.Format("xmlTest: {0}->{1}", xmlTest2.TestString, xmlTest2.TestUInt32));
                TestData xmlTest3 = "Test.xml.zip".LoadFromArchive<TestData>("Test.xml", SaveType.XML);
                Console.WriteLine(String.Format("xmlTest: {0}->{1}", xmlTest3.TestString, xmlTest3.TestUInt32));
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
