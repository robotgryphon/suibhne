using Ostenvighx.Suibhne.Services;
using Ostenvighx.Suibhne.Services.Irc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class ServiceStarter : IServiceStarter {

    public ServiceConnector CreateServiceInstance(Guid id) {
        return new IrcNetwork(id);
    }
}
