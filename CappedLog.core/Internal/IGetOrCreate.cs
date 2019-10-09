using System;
using System.Collections.Generic;
using System.Text;

namespace CappedLog
{
    internal interface IGetOrCreate<Tk,Tv>
    {
        Tk Key { get; }

        Tv CreateValue();
    }
}
