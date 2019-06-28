using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
namespace HyperVRemote.Source.Interface
{
    public interface IHyperVSnapshot
    {
        string Name { get; }

        DateTime Time { get; }

        ManagementObject RawShapshot { get; }

    }
}
