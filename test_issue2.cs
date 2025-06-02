using System;

namespace Test
{
    public class TestIssue2
    {
        public void TestMethod()
        {
            // TODO; this should be converted to TODO:
            var x = 42;
            
            // TODO, this should be converted to TODO:
            var y = 43;
            
            // TODO- this should be converted to TODO:
            var z = 44;
            
            // FIXME; this should be converted to FIXME:
            var a = 45;
            
            // HACK, this should be converted to HACK:
            var b = 46;
        }
    }
}