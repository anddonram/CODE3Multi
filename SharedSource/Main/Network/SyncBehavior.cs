using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using WaveEngine.Framework;
using WaveEngine.Networking;
using WaveEngine.Networking.Messages;

namespace Codigo.Behaviors
{
    /**
     * <summary>
     * This class syncs all the components in the same entity from server to clients
     * </summary>
     */
    class SyncBehavior : Behavior
    {
        private NetworkSyncComponent[] NetworkSyncComponents;
        private double updateInterval=0.04;
        private double counter = 0;
        NetworkedScene scene;
        protected override void Initialize()
        {
            base.Initialize();
            this.scene=(Owner.Scene as NetworkedScene);
        }
        /// <summary>
        /// Resolves the dependencies.
        /// </summary>
        protected override void ResolveDependencies()
        {
            this.NetworkSyncComponents = this.Owner.Components.Where(x => x is NetworkSyncComponent).Cast<NetworkSyncComponent>().ToArray();
        }
        protected override void Update(TimeSpan gameTime)
        {
            if (scene.isHost && !Owner.IsDisposed && Owner.IsInitialized)
            {
                counter += gameTime.TotalSeconds;
                if (counter >= updateInterval)
                {
                    Sync();
                    counter = 0;
                }

            }
        }

        private void Sync()
        {    
                for (int i = 0; i < NetworkSyncComponents.Length; i++)
                {
                    NetworkSyncComponent c = NetworkSyncComponents[i];
                    if (c.NeedSendSyncData())
                    {
                        OutgoingMessage mes = scene.networkService.CreateServerMessage();
                        mes.Write(NetworkedScene.SYNC);
                        mes.Write(c.Owner.Name);
                        mes.Write(c.GetType().ToString());
                        c.WriteSyncData(mes);
                        scene.networkService.SendToClients(mes, DeliveryMethod.ReliableOrdered);
                        Trace.WriteLine("sending to clients: " + c.Owner.Name);
                    }
                }
        }
    }
}
