using System.Collections.Generic;
using HyperVRemote.Source.Interface;

namespace HyperVRemote.HyperV
{
	/// <summary>
	/// Позволяет управлять с Hyper-V сервером
	/// </summary>
	public interface IHyperVController
	{
		/// <summary>
		/// Имя сервера с которым работает контроллер
		/// </summary>
		string ServerName { get; }

		/// <summary>
		/// Список виртуальных машин сервера
		/// </summary>
		IEnumerable<IHyperVMachine> VirtualMachines { get; }

		/// <summary>
		/// Список виртуальных машин для проверки контента
		/// </summary>
		IEnumerable<IHyperVMachine> ContentVirtualMachines { get; }
	}
}