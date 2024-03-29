﻿using System;
using System.Collections.Generic;
using System.Linq;
using HyperVRemote.Source.Implementation;
using HyperVRemote.Source.Interface;

namespace HyperVRemote.HyperV
{
	/// <summary>
	/// Позволяет управлять с Hyper-V сервером
	/// </summary>
	public class HyperVController : IHyperVController
	{
		private HyperVProvider _provider;

		public HyperVController( string hyperVServerName, string login, string password )
		{
			ServerName = hyperVServerName;
			Login = login ?? string.Empty;
			Password = password ?? string.Empty;

            HyperVRemoteOptions configuration = new HyperVRemoteOptions { HyperVServerName = ServerName, HyperVUserName = Login, HyperVUserPassword = Password };

            _provider = new HyperVProvider(configuration);
        }

		public string ServerName { get; }

		public string Login { get; }

		public string Password { get; }

        public string Status {
            get {
                if (_provider.isConnected())
                    return "connected";
                else
                    return "not connected";
            } }

		public IEnumerable<IHyperVMachine> VirtualMachines => _provider.GetMachines();

		public IEnumerable<IHyperVMachine> ContentVirtualMachines => VirtualMachines.Where( vm => vm.Name.StartsWith( "(Content" ) );


		public void Connect()
		{
			_provider.Connect();
		}
	}
}
