using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.Management.Instrumentation;
using HyperVRemote.Source.Interface;

namespace HyperVRemote.Source.Implementation
{
    public class HyperVSnapshot : IHyperVSnapshot
    {
        private readonly ManagementObject _rawShapshot;

        public ManagementObject RawShapshot
        {
            get
            {
                return _rawShapshot;
            }
        }


        private string name;
        private DateTime time;

        public string Name { get => name; }
        public DateTime Time { get => time; }

        public HyperVSnapshot(string name, DateTime time, ManagementObject rawShapshot)
        {
            this.name = name;
            this.time = time;
            _rawShapshot = rawShapshot;
        }

    }
}
