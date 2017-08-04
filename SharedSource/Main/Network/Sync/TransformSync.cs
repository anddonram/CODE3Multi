using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using WaveEngine.Common.Input;
using WaveEngine.Common.Math;
using WaveEngine.Framework;
using WaveEngine.Framework.Graphics;
using WaveEngine.Framework.Managers;
using WaveEngine.Framework.Services;
using WaveEngine.Networking;
using WaveEngine.Networking.Messages;

namespace Codigo
{   /**
     * <summary>
     * This class sync the position by the transform
     * Message intensive
     * </summary>
     */
    [DataContract]
    public class TransformSync : NetworkSyncComponent
    {
        [DataMember]
        private Vector2 lastPosition;
  
        [RequiredComponent]
        protected Transform2D transform;

        [RequiredComponent]
        protected WorldObject wo;

        public override bool NeedSendSyncData()
        {
            return this.lastPosition != this.transform.Position;// && wo.GetAction() != ActionEnum.Move;//this is only if movementsync does not handle enter and exit cottage, which does now
        }

        public override void WriteSyncData(OutgoingMessage writer)
        {
            this.lastPosition = this.transform.Position;
            writer.Write(this.lastPosition.X);
            writer.Write(this.lastPosition.Y);
        }

        public override void ReadSyncData(IncomingMessage binaryReader)
        {
            this.transform.Position = new Vector2(binaryReader.ReadSingle(), binaryReader.ReadSingle()); 
        }
    }
}