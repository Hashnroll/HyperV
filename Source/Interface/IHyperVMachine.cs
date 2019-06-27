using System.Management;

namespace HyperVRemote.Source.Interface
{
	/// <summary>
	/// Интерфейс виртуальной машины Hyper-V
	/// </summary>
	public interface IHyperVMachine
	{
		/// <summary>
		/// ВМ как ManagementObject
		/// </summary>
		ManagementObject AsRawMachine { get; }

		/// <summary>
		/// Имя машины
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Статус
		/// </summary>
		HyperVStatus Status { get; }

		/// <summary>
		/// Запустить ВМ
		/// </summary>
		void Start();

		/// <summary>
		/// Остановить
		/// </summary>
		void Stop();

		/// <summary>
		/// Сбросить
		/// </summary>
		void Reset();

        /// <summary>
		/// Сделать снепшот
		/// </summary>
        void CreateSnapshot();

        void RemoveSnapshot(ManagementObject snapshot);

        void ApplySnapshot(ManagementObject snapshot);

        void ListRecoverySnapshots();

        /// <summary>
        /// Восстановить из последнего снапшота
        /// </summary>
        void RestoreLastSnapShot();
	}
}