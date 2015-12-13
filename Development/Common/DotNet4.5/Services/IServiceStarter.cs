using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ostenvighx.Suibhne.Services {
    public interface IServiceStarter {

       ServiceConnector CreateServiceInstance(Guid id);
    }
}
