using System;
using System.Collections.Generic;
using System.Text;
using WaveEngine.Networking;
using WaveEngine.Networking.Messages;

namespace Codigo.Components
{
    public class SpeedSync : NetworkSyncComponent
    { 
        private float lastSceneSpeed=1;

        public override bool NeedSendSyncData()
        {
            return Owner.Scene.Speed != lastSceneSpeed;
        }

        public override void ReadSyncData(IncomingMessage binaryReader)
        {
            Owner.Scene.Speed = binaryReader.ReadSingle();
        }

        public override void WriteSyncData(OutgoingMessage writer)
        {
            lastSceneSpeed = Owner.Scene.Speed;
            writer.Write(lastSceneSpeed);
        }
    }
}
