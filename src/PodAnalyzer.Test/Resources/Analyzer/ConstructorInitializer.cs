using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PodAnalyzer.Test.Resources.Analyzer
{
    class A
    {
        int I { get; }
        A(int i) { I = i; }
    }

    class B : A
    {
        B(int i) : base(i) { }
    }
}
