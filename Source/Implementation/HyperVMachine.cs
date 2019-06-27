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
		#region Fields

		private readonly Func<ManagementObject> _refreshMachineFunc;

		#endregion

		#region Ctor

		public HyperVMachine( Func<ManagementObject> refreshFunc )
		{
			_refreshMachineFunc = refreshFunc;
		}

		#endregion

		#region Props

		public ManagementObject AsRawMachine => GetMachine();


		public string Name => AsRawMachine["ElementName"] as string;

		public HyperVStatus Status => (HyperVStatus)AsRawMachine["EnabledState"];

		#endregion

		#region Api

		public void Reset()
		{
			ChangeState( HyperVStatus.Reset );
		}

		public void Start()
		{
			ChangeState( HyperVStatus.Running );
		}

		public void Stop()
		{
			ChangeState( HyperVStatus.Off );
		}

        public void ListRecoverySnapshots()
        {
            ManagementScope scope = AsRawMachine.Scope;

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
                            Console.WriteLine("Name: {0}\nRecovery snapshot creation time: {1}\n", (string)setting["ElementName"], time);
                        }
                    }
                }
            }
        }

        public void CreateSnapshot()
        {
            ManagementScope scope = AsRawMachine.Scope;
            ManagementObject virtualSystemService = Utility.GetServiceObject(scope, "Msvm_VirtualSystemSnapshotService");

            ManagementBaseObject inParams = virtualSystemService.GetMethodParameters("CreateSnapshot");

            // Set the AffectedSystem property.
            ManagementObject vm = Utility.GetTargetComputer(Name, scope);
            if (null == vm)
            {
                throw new ArgumentException(string.Format("The virtual machine \"{0}\" could not be found.", Name));
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
        
        public void RemoveSnapshot(ManagementObject snapshot)
        {
            ManagementScope scope = AsRawMachine.Scope;
            ManagementObject virtualSystemService = Utility.GetServiceObject(scope, "Msvm_VirtualSystemSnapshotService");

            ManagementBaseObject inParams = virtualSystemService.GetMethodParameters("DestroySnapshot");

            ManagementObject vm = Utility.GetTargetComputer(Name, scope);
            if (null == vm)
            {
                throw new ArgumentException(string.Format("The virtual machine \"{0}\" could not be found.", Name));
            }

            inParams["AffectedSnapshot"] = snapshot;

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

        public void ApplySnapshot(ManagementObject snapshot)
        {
            ManagementScope scope = AsRawMachine.Scope;
            ManagementObject virtualSystemService = Utility.GetServiceObject(scope, "Msvm_VirtualSystemSnapshotService");

            ManagementBaseObject inParams = virtualSystemService.GetMethodParameters("ApplySnapshot");

            ManagementObject vm = Utility.GetTargetComputer(Name, scope);
            if (null == vm)
            {
                throw new ArgumentException(string.Format("The virtual machine \"{0}\" could not be found.", Name));
            }

            inParams["Snapshot"] = snapshot;

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

        public ManagementObject getLastSnapShot()
        {
            var raw = AsRawMachine;
            var scope = AsRawMachine.Scope;

            var lastSnapshot = raw.GetRelated(
                "Msvm_VirtualSystemSettingData",
                "Msvm_MostCurrentSnapshotInBranch",
                null,
                null,
                "Dependent",
                "Antecedent",
                false,
                null).OfType<ManagementObject>().FirstOrDefault();

            if (lastSnapshot == null)
                throw new HyperVException("No Snapshot found");

            return lastSnapshot;
        }

        public void RestoreLastSnapShot()
		{
			var raw = AsRawMachine;
			var scope = AsRawMachine.Scope;

			var lastSnapshot = raw.GetRelated(
				"Msvm_VirtualSystemSettingData",
				"Msvm_MostCurrentSnapshotInBranch",
				null,
				null,
				"Dependent",
				"Antecedent",
				false,
				null ).OfType<ManagementObject>().FirstOrDefault();

			if ( lastSnapshot == null )
				throw new HyperVException( "No Snapshot found" );

			var managementService = new ManagementClass( scope, new ManagementPath( "Msvm_VirtualSystemSnapshotService" ), null ).GetInstances().OfType<ManagementObject>().FirstOrDefault();

			var inParameters = managementService.GetMethodParameters( "ApplySnapshot" );
			inParameters["Snapshot"] = lastSnapshot.Path.Path;

			var outParameters = managementService.InvokeMethod( "ApplySnapshot", inParameters, null );
		}
        /*
        static ManagementObject GetLastVirtualSystemSnapshot(ManagementObject vm)
        {
            ManagementObjectCollection settings = vm.GetRelated(
                "Msvm_VirtualSystemsettingData",
                "Msvm_PreviousSettingData",
                null,
                null,
                "SettingData",
                "ManagedElement",
                false,
                null);

            ManagementObject virtualSystemsetting = null;
            foreach (ManagementObject setting in settings)
            {
                Console.WriteLine(setting.Path.Path);
                Console.WriteLine(setting["ElementName"]);
                virtualSystemsetting = setting;
            }

            return virtualSystemsetting;
        }

        static void RemoveVirtualSystemSnapshot(string vmName)
        {
            ManagementScope scope = new ManagementScope(@"root\virtualization\v2", null);
            ManagementObject virtualSystemService = Utility.GetServiceObject(scope, "Msvm_VirtualSystemManagementService");

            ManagementBaseObject inParams = virtualSystemService.GetMethodParameters("RemoveVirtualSystemSnapshot");

            ManagementObject vm = Utility.GetTargetComputer(vmName, scope);

            ManagementObject vmSnapshot = GetLastVirtualSystemSnapshot(vm);

            inParams["SnapshotSettingData"] = vmSnapshot.Path.Path;

            ManagementBaseObject outParams = virtualSystemService.InvokeMethod("RemoveVirtualSystemSnapshot", inParams, null);

            if ((UInt32)outParams["ReturnValue"] == ReturnCode.Started)
            {
                if (Utility.JobCompleted(outParams, scope))
                {
                    Console.WriteLine("Snapshot was removed successfully.");

                }
                else
                {
                    Console.WriteLine("Failed to remove snapshot VM");
                }
            }
            else if ((UInt32)outParams["ReturnValue"] == ReturnCode.Completed)
            {
                Console.WriteLine(outParams["SnapshotsettingData"].ToString());
                Console.WriteLine("Snapshot was removed successfully.");
            }
            else
            {
                Console.WriteLine("Remove virtual system snapshot failed with error {0}", outParams["ReturnValue"]);
            }

            inParams.Dispose();
            outParams.Dispose();
            vmSnapshot.Dispose();
            vm.Dispose();
            virtualSystemService.Dispose();
        }*/
        #endregion

        #region Private methods

        private uint ChangeState( HyperVStatus state )
		{
			var raw = AsRawMachine;
			var scope = AsRawMachine.Scope;

			var managementService = new ManagementClass( scope, new ManagementPath( "Msvm_VirtualSystemManagementService" ), null )
																	.GetInstances()
																	.OfType<ManagementObject>().FirstOrDefault();

			if ( managementService != null )
			{
				var inParameters = managementService.GetMethodParameters( "RequestStateChange" );

				inParameters["RequestedState"] = (object)state;

				var outParameters = raw.InvokeMethod( "RequestStateChange", inParameters, null );
				if ( outParameters != null )
					return (uint)outParameters["ReturnValue"];
			}
			else
				throw new HyperVException( "Could not find machine management service for rstate change" );

			return 0;
		}

		private ManagementObject GetMachine()
		{
			return _refreshMachineFunc?.Invoke();
		}

		#endregion
	}
}