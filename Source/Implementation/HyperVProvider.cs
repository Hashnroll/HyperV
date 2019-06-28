using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using HyperVRemote.Source.Interface;
using Microsoft.Extensions.Options;
using System;

namespace HyperVRemote.Source.Implementation
{

    public class HyperVProvider : IHyperVProvider
    {
        private readonly HyperVRemoteOptions _options;

        public ConnectionOptions ConnectionOptions { get; }

        private ManagementScope _scope;

        public HyperVProvider(HyperVRemoteOptions options)
        {
            _options = options;

            var connectionOptions = new ConnectionOptions();
            connectionOptions.Locale = @"en-US";

            var domain = _options.Domain;
            if (!string.IsNullOrWhiteSpace(domain))
            {
                connectionOptions.Authority = "ntlmdomain:" + domain;
            }

            var userName = options.HyperVUserName;
            if (!string.IsNullOrWhiteSpace(userName))
            {
                connectionOptions.Username = userName;
            }

            var password = options.HyperVUserPassword;
            if (!string.IsNullOrWhiteSpace(password))
            {
                connectionOptions.Password = password;
            }


            connectionOptions.Timeout = options.Timeout;
            connectionOptions.Impersonation = ImpersonationLevel.Impersonate;
            connectionOptions.Authentication = AuthenticationLevel.Default;

            _scope = new ManagementScope(new ManagementPath
            {
                Server = _options.HyperVServerName,
                NamespacePath = _options.HyperVNameSpace
            }, ConnectionOptions);

            ConnectionOptions = connectionOptions;

            _options = options;
        }

        public void Connect()
        {

            _scope.Connect();
        }

        public bool isConnected()
        {
            return _scope.IsConnected;
        }

        public IEnumerable<IHyperVMachine> GetMachines()
        {
            var en = new ManagementClass(_scope, new ManagementPath("Msvm_ComputerSystem"), null)
                    .GetInstances()
                    .OfType<ManagementObject>().Where(x => (string)x["Caption"] == "Виртуальная машина" || (string)x["Caption"] == "Virtual Machine");

            List<HyperVMachine> machines = en.Select(machine => new HyperVMachine(machine)).ToList();

            return machines;
        }

        public IHyperVMachine GetMachineByName(string name)
        {
            var en = new ManagementClass(_scope, new ManagementPath("Msvm_ComputerSystem"), null)
               .GetInstances()
               .OfType<ManagementObject>().Where(x => "Virtual Machine" == (string)x["Caption"]);

            return new HyperVMachine(en.First(x => x["ElementName"] as string == name));
        }

        private ManagementObject GetRawMachine(string name)
        {
            var en = new ManagementClass(_scope, new ManagementPath("Msvm_ComputerSystem"), null)
                             .GetInstances()
                             .OfType<ManagementObject>().Where(x => (string)x["Caption"] == "Виртуальная машина" || "Virtual Machine" == (string)x["Caption"]);

            return en.FirstOrDefault(x => x["ElementName"] as string == name);
        }

        public void Reset(IHyperVMachine machine)
        {
            ChangeState(machine, HyperVStatus.Reset);
        }

        public void Start(IHyperVMachine machine)
        {
            ChangeState(machine, HyperVStatus.Running);
        }

        public void Stop(IHyperVMachine machine)
        {
            ChangeState(machine, HyperVStatus.Off);
        }

        private uint ChangeState(IHyperVMachine machine, HyperVStatus state)
        {
            var raw = machine.RawMachine;
            var scope = raw.Scope;

            var managementService = new ManagementClass(scope, new ManagementPath("Msvm_VirtualSystemManagementService"), null)
                                                                    .GetInstances()
                                                                    .OfType<ManagementObject>().FirstOrDefault();

            if (managementService != null)
            {
                var inParameters = managementService.GetMethodParameters("RequestStateChange");

                inParameters["RequestedState"] = (object)state;

                var outParameters = raw.InvokeMethod("RequestStateChange", inParameters, null);
                if (outParameters != null)
                    return (uint)outParameters["ReturnValue"];
            }
            else
                throw new HyperVException("Could not find machine management service for rstate change");

            return 0;
        }

        public void CreateSnapshot(IHyperVMachine machine)
        {
            string name = machine.Name;
            ManagementScope scope = machine.Scope;
            ManagementObject virtualSystemService = Utility.GetServiceObject(scope, "Msvm_VirtualSystemSnapshotService");

            ManagementBaseObject inParams = virtualSystemService.GetMethodParameters("CreateSnapshot");

            // Set the AffectedSystem property.
            ManagementObject vm = Utility.GetTargetComputer(name, scope);
            if (null == vm)
            {
                throw new ArgumentException(string.Format("The virtual machine \"{0}\" could not be found.", name));
            }

            inParams["AffectedSystem"] = vm.Path.Path;

            inParams["SnapshotSettings"] = "";

            // Set the SnapshotType property.
            inParams["SnapshotType"] = 2; // Full snapshot.

            ManagementBaseObject outParams = virtualSystemService.InvokeMethod("CreateSnapshot", inParams, null);

            if ((UInt32)outParams["ReturnValue"] == ReturnCode.Started)
            {
                if (Utility.JobCompleted(outParams, scope))
                {
                    Console.WriteLine("Snapshot was created successfully.");

                }
                else
                {
                    Console.WriteLine("Failed to create snapshot VM");
                }
            }
            else if ((UInt32)outParams["ReturnValue"] == ReturnCode.Completed)
            {
                Console.WriteLine("Snapshot was created successfully.");
            }
            else
            {
                Console.WriteLine("Create virtual system snapshot failed with error {0}", outParams["ReturnValue"]);
            }

            inParams.Dispose();
            outParams.Dispose();
            vm.Dispose();
            virtualSystemService.Dispose();
        }

        public void RemoveSnapshot(IHyperVMachine machine, IHyperVSnapshot snapshot)
        {
            string name = machine.Name;
            ManagementScope scope = machine.Scope;
            ManagementObject virtualSystemService = Utility.GetServiceObject(scope, "Msvm_VirtualSystemSnapshotService");

            ManagementBaseObject inParams = virtualSystemService.GetMethodParameters("DestroySnapshot");

            ManagementObject vm = Utility.GetTargetComputer(name, scope);
            if (null == vm)
            {
                throw new ArgumentException(string.Format("The virtual machine \"{0}\" could not be found.", name));
            }

            inParams["AffectedSnapshot"] = snapshot.RawShapshot;

            ManagementBaseObject outParams = virtualSystemService.InvokeMethod("DestroySnapshot", inParams, null);

            if ((UInt32)outParams["ReturnValue"] == ReturnCode.Started)
            {
                if (Utility.JobCompleted(outParams, scope))
                {
                    Console.WriteLine("Snapshot was removed successfully.");

                }
                else
                {
                    Console.WriteLine("Failed to remove snapshot");
                }
            }
            else if ((UInt32)outParams["ReturnValue"] == ReturnCode.Completed)
            {
                Console.WriteLine("Snapshot was removed successfully.");
            }
            else
            {
                Console.WriteLine("Remove virtual system snapshot failed with error {0}", outParams["ReturnValue"]);
            }

            inParams.Dispose();
            outParams.Dispose();
            vm.Dispose();
            virtualSystemService.Dispose();
        }

        public void ApplySnapshot(IHyperVMachine machine, IHyperVSnapshot snapshot)
        {
            string name = machine.Name;
            ManagementScope scope = machine.Scope;
            ManagementObject virtualSystemService = Utility.GetServiceObject(scope, "Msvm_VirtualSystemSnapshotService");

            ManagementBaseObject inParams = virtualSystemService.GetMethodParameters("ApplySnapshot");

            ManagementObject vm = Utility.GetTargetComputer(name, scope);
            if (null == vm)
            {
                throw new ArgumentException(string.Format("The virtual machine \"{0}\" could not be found.", name));
            }

            inParams["Snapshot"] = snapshot.RawShapshot;

            ManagementBaseObject outParams = virtualSystemService.InvokeMethod("ApplySnapshot", inParams, null);

            if ((UInt32)outParams["ReturnValue"] == ReturnCode.Started)
            {
                if (Utility.JobCompleted(outParams, scope))
                {
                    Console.WriteLine("Snapshot was applied successfully.");

                }
                else
                {
                    Console.WriteLine("Failed to apply snapshot");
                }
            }
            else if ((UInt32)outParams["ReturnValue"] == ReturnCode.Completed)
            {
                Console.WriteLine("Snapshot was applied successfully.");
            }
            else
            {
                Console.WriteLine("Apply virtual system snapshot failed with error {0}", outParams["ReturnValue"]);
            }

            inParams.Dispose();
            outParams.Dispose();
            vm.Dispose();
            virtualSystemService.Dispose();
        }
    }
}
