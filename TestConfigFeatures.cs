using System;

namespace TestConfigFeatures
{
    public class TestClass
    {
        public void TestInlineDisabling()
        {
            // nocomments:disable This should be ignored
            var x = 1;
            
            // HUMAN: This should be allowed with default config
            var y = 2;
            
            // TODO: This should be allowed with default config
            var z = 3;
            
            // This regular comment should trigger NC0001 with default config
            var w = 4;
        }
    }
}