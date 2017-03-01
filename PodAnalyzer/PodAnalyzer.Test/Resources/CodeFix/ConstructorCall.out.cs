using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PodAnalyzer.Test.Resources
{
    public class MultiProp
    {
        string Foo { get; }
        string Bar { get; }
        string Baz { get; }

        public MultiProp(
            string foo,
            string bar,
            string baz)
        {
            Foo = foo;
            Bar = bar;
            Baz = baz;
        }

        static void Test()
        {
            var mp = new MultiProp(
                foo: "hello",
                bar: "world",
                baz: "!");
        }
    }
}
