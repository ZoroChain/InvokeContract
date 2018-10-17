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
        string ChainHash {
            get;
            set;
        }
        string WIF {
            set;
            get;
        }
        string targetWIF {
            set;
            get;
        }
        string ContractPath {
            set;
            get;
        }
        string ContractHash {
            set;
            get;
        }
        Task StartAsync();
    }
}
