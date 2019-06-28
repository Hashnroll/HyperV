using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using HyperVRemote.Source.Interface;
using Microsoft.Extensions.Options;

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
            
            var connectionOptions = new ConnectionOptions
            {
                Locale = @"en-US"
            };

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

            ConnectionOptions = connectionOptions;

            _scope = new ManagementScope(new ManagementPath
            {
                Server = _options.HyperVServerName,
                NamespacePath = _options.HyperVNameSpace
            }, ConnectionOptions);

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

            List<HyperVMachine> machines = en.Select(machine => new HyperVMachine(() => GetRawMachine(machine["ElementName"] as string))).ToList();

            return machines;
        }

        public IHyperVMachine GetMachineByName(string name)
        {
            var rawMachine = GetRawMachine(name);

            if (rawMachine == null)
                return null;

            return new HyperVMachine(() => GetRawMachine(name));
        }

        private ManagementObject GetRawMachine(string name)
        {
            var en = new ManagementClass(_scope, new ManagementPath("Msvm_ComputerSystem"), null)
                             .GetInstances()
                             .OfType<ManagementObject>().Where(x => (string)x["Caption"] == "Виртуальная машина" || "Virtual Machine" == (string)x["Caption"]);

            return en.FirstOrDefault(x => x["ElementName"] as string == name);
        }
    }
}
