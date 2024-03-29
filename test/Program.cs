using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HyperVRemote.Source.Implementation;
using HyperVRemote.HyperV;

namespace test
{
    class Program
    {
        static void Main(string[] args)
        {
            HyperVController controller = new HyperVController("DESKTOP-9H356BC",null,null);
            Console.WriteLine(controller.Status);

            controller.Connect();
            Console.WriteLine(controller.Status);

            foreach (HyperVMachine machine in controller.VirtualMachines)
            {
                Console.WriteLine("Name: {0}\nStatus: {1}\n", machine.Name, machine.Status);

                machine.CreateSnapshot();
            }
            Console.ReadLine();
        }
    }
}
