using System.Management;
using System.Collections.Generic;

namespace HyperVRemote.Source.Interface
{
	/// <summary>
	/// Интерфейс виртуальной машины Hyper-V
	/// </summary>
	public interface IHyperVMachine
	{
        ManagementObject RawMachine { get; }

        string Name { get; }

		HyperVStatus Status { get; }

        ManagementScope Scope { get; }

        IEnumerable<IHyperVSnapshot> Snapshots { get;  }
	}
}