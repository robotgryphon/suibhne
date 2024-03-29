﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ostenvighx.Suibhne.Services {
    public abstract class ServiceConnector {

        /// <summary>
        /// The identifier used to locate and manage this service connection.
        /// </summary>
        public Guid Identifier;

        /// <summary>
        /// Occurs when a connection is complete and data is ready to be served.
        /// </summary>
        public event Events.ServiceEvent OnConnectionComplete;

        /// <summary>
        /// Occurs when a connection is completely terminated.
        /// </summary>
        public event Events.ServiceEvent OnDisconnectComplete;

        /// <summary>
        /// A value that indicates what status the connection is in.
        /// See the Base.Reference.ConnectionStatus enum.
        /// </summary>
        /// <seealso cref="Ostenvighx.Suibhne.Services.Chat.Reference.ConnectionStatus"/>
        public Reference.ConnectionStatus Status;

        public event Events.CustomEventDelegate OnCustomEventFired;

        #region Connection Handling
        public abstract void Start();
        public abstract void Stop(String reason = "");

        protected virtual void ConnectionComplete() {
            if (this.OnConnectionComplete != null)
                OnConnectionComplete(this);
        }

        protected virtual void DisconnectionComplete() {
            if (this.OnDisconnectComplete != null)
                OnDisconnectComplete(this);
        }
        #endregion

        protected virtual void FireEvent(String json) {
            if (this.OnCustomEventFired != null) {
                OnCustomEventFired(Identifier, json);
            }
        }

        public abstract String[] GetSupportedEvents();

        public abstract void Setup(String configBase);
    }
}
