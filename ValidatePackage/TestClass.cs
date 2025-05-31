using System;

namespace ValidatePackage
{
    public class TestClass
    {
        public void TestMethod()
        {
            /* [!] This comment should be allowed */
            Console.WriteLine("Hello World!");
            
            // [!] This comment should also be allowed
            var x = 42;
            Console.WriteLine(x);
        }
    }
}