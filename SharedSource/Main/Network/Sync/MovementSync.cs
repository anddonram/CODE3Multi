using Codigo.Behaviors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using WaveEngine.Common.Input;
using WaveEngine.Common.Math;
using WaveEngine.Components.GameActions;
using WaveEngine.Framework;
using WaveEngine.Framework.Graphics;
using WaveEngine.Framework.Managers;
using WaveEngine.Framework.Services;
using WaveEngine.Networking;
using WaveEngine.Networking.Messages;
using WaveEngine.TiledMap;

namespace Codigo
{
    /**
     * <summary>
     * This class tries to sync the position more efficiently, by passing only the changing tiles
     * Handles movement as well as entering and exiting from cottages and switching places with others
     * </summary>
     */
    [DataContract]
    public class MovementSync : NetworkSyncComponent
    {

        [RequiredComponent]
        protected MovementBehavior move;

        [RequiredComponent]
        protected EnterExitBehavior ee;

        [RequiredComponent]
        protected SwitchBehavior sb;

        [RequiredComponent]
        protected WorldObject wo;

        private LayerTile lastNextTile;

        private MoveTo2DGameAction lastAnimation;

        public override bool NeedSendSyncData()
        {
            return (wo.animation!=null && lastAnimation!=wo.animation);
        }

        public override void WriteSyncData(OutgoingMessage writer)
        {
            if (wo.GetAction() == ActionEnum.Move)
            {
                this.lastNextTile = move.nextTile;
            }
            else if (wo.GetAction() == ActionEnum.Switch)
            {
                this.lastNextTile = sb.otherTile;
            }
            else
            {
                this.lastNextTile = ee.nextTile;
            }
            //send action state as it is not synchro yet in the client(for speed purposes)
            //we should be passing speed instead but messages cannot send double numeric variables.
            writer.Write((int)wo.GetAction());

            writer.Write(lastNextTile.LocalPosition.X);
            writer.Write(lastNextTile.LocalPosition.Y);
            lastAnimation = wo.animation;
        }

        public override void ReadSyncData(IncomingMessage binaryReader)
        {
            //find the action so we can synchro the speed accordingly
            ActionEnum act=(ActionEnum)binaryReader.ReadInt32();

            double speed = wo.genericSpeed;
            

            if (wo.animation == null || wo.animation.State == TaskState.Finished)
            {
                wo.animation = new MoveTo2DGameAction(Owner, new Vector2(binaryReader.ReadSingle(), binaryReader.ReadSingle()), TimeSpan.FromSeconds(speed));
                wo.animation.Run();
            }
            else
            {
                //32
                wo.animation.Cancel();
                Vector2 nextTile=new Vector2(binaryReader.ReadSingle(), binaryReader.ReadSingle());
                float distancePerson = Vector2.Distance(nextTile, wo.transform.Position);
                Trace.WriteLine(speed * distancePerson / WorldObjectData.tileSize.X);
                wo.animation= new MoveTo2DGameAction(Owner, nextTile, TimeSpan.FromSeconds(speed*distancePerson/WorldObjectData.tileSize.X));
                wo.animation.Run();
            }

        }
    }
}