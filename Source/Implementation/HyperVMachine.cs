using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Management.Instrumentation;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using HyperVRemote.Source.Interface;
using System.Globalization;
using System.Threading;
using System.IO;
using System.Xml;
using System.Collections.Generic;

namespace HyperVRemote.Source.Implementation
{
    public class HyperVMachine : IHyperVMachine
    {
        private readonly ManagementObject _rawMachine;

        public HyperVMachine(ManagementObject rawMachine)
        {
            _rawMachine = rawMachine;
        }

        public ManagementObject RawMachine
        {
            get
            {
                return _rawMachine;
            }
        }

        public string Name
        {
            get
            {
                return _rawMachine["ElementName"] as string;
            }
        }

        public HyperVStatus Status
        {
            get
            {
                return (HyperVStatus)_rawMachine["EnabledState"];
            }
        }

        public ManagementScope Scope
        {
            get
            {
               return _rawMachine.Scope;
            }
        }

        public IEnumerable<IHyperVSnapshot> Snapshots
        {
            get
            {
                return GetSnapshots();
            }
        }

        private IEnumerable<IHyperVSnapshot> GetSnapshots()
        {
            ManagementScope scope = _rawMachine.Scope;

            List<HyperVSnapshot> snapshots = new List<HyperVSnapshot>();

            // Get the VM object and snapshot settings. 
            using (ManagementObject vm = Utility.GetTargetComputer(Name, scope))
            using (ManagementObjectCollection settingsCollection =
                vm.GetRelated("Msvm_VirtualSystemSettingData", "Msvm_SnapshotOfVirtualSystem",
                null, null, null, null, false, null))
            {
                foreach (ManagementObject setting in settingsCollection)
                {
                    using (setting)
                    {
                        string systemType = (string)setting["VirtualSystemType"];

                        if (string.Compare(systemType, VirtualSystemTypeNames.RealizedSnapshot,
                            StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            // It is a recovery snapshot.
                            DateTime time = ManagementDateTimeConverter.ToDateTime(setting["CreationTime"].ToString());
                            snapshots.Add(new HyperVSnapshot((string)setting["ElementName"], time , setting));
                        }
                    }
                }
            }

            return snapshots;
        }
	}
}