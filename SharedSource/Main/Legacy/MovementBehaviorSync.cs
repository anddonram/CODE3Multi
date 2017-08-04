using Codigo.Behaviors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
{
   
    //[DataContract]
    //public class MovementBehaviorSync : NetworkSyncComponent
    //{
    //    [RequiredComponent]
    //    private MovementBehavior movement;
  
    //    [DataMember]
    //    private Boolean lastMoving;

    //    public override bool NeedSendSyncData()
    //    {
    //        return lastMoving != movement.Moving();
    //    }

    //    public override void WriteSyncData(OutgoingMessage writer)
    //    {
    //        lastMoving = movement.Moving();

    //        writer.Write(lastMoving);


    //    }
        
    //    public override void ReadSyncData(IncomingMessage m)
    //    {

    //        movement.SetMoving(m.ReadBoolean());


    //    }
    //}
}