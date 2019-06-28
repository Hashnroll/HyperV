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
    class HyperVSnapshot : IHyperVSnapshot
    {
        private string name;
        private DateTime time;

        public string Name { get => name; set => name = value; }
        public DateTime Time { get => time; set => time = value; }
    }
}
