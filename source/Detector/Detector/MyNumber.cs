//Omid55
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Detector
{
    internal class MyNumber
    {
        public StylusPointCollection Points { get; set; }
        public int Target { get; set; }


        public MyNumber()
        {
        }

        public MyNumber(StylusPointCollection Points, int Target)
        {
            this.Points = Points;
            this.Target = Target;
        }
    }
}