using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperVRemote.Source.Interface
{
    public interface IHyperVSnapshot
    {
        string Name { get; set; }

        DateTime Time { get; set; }
    }
}
