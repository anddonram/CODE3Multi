using System;
using System.Runtime.Serialization;
using WaveEngine.Common.Graphics;
using WaveEngine.Common.Math;
using WaveEngine.Framework;
using WaveEngine.Networking;
using WaveEngine.Networking.Messages;

namespace Codigo
{
    [DataContract]
    public class PlayerSync : NetworkSyncComponent
    {
        [RequiredComponent]
        private Player player;
   
        [DataMember]
        private string lastWO = string.Empty;
        [DataMember]
        private float lastWood=0;
        //TODO: selectedWO is only synced in the host client, as this is run in the server.
        //It should be synced for clients selecting WOs as well
        public override bool NeedSendSyncData()
        {
            return player.woodAmount!=lastWood || (player.selectedWO == null && lastWO != string.Empty) ||
                (player.selectedWO != null && player.selectedWO.GetWoName() != lastWO);
        }

        public override void ReadSyncData(IncomingMessage binaryReader)
        {
            //Sync wood
            player.RemoveWood(player.woodAmount);
            player.AddWood(binaryReader.ReadSingle());
            
            // obtener el networkId del objeto para pasarlo
            if (!player.isLocalPlayer)
            {
                string woName = binaryReader.ReadString();
                if (string.IsNullOrEmpty(woName)) {
                    player.selectedWO = null;
                }
                else
                {
                    Entity ent = EntityManager.Find(woName);
                    if (ent!=null&&!ent.IsDisposed)
                    player.selectedWO =ent.FindComponent<WorldObject>();
                }
            }
        }

        public override void WriteSyncData(OutgoingMessage writer)
        {
            lastWood = player.woodAmount;
            lastWO = player.selectedWO != null ? player.selectedWO.GetWoName() : string.Empty;

            writer.Write(lastWood);
            writer.Write(lastWO);
        }
    }
}