using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace InvokeContractTest
{
    public interface IExample
    {
        string Name {
            get;
        }
        string ID {
            get;
        }        
        Task StartAsync();
    }
}
