using Codigo.Behaviors;
using System;
using System.Collections.Generic;
using System.Diagnostics;

using System.Linq;
using System.Runtime.Serialization;
using WaveEngine.Common.Graphics;
using WaveEngine.Common.Input;
using WaveEngine.Common.Math;
using WaveEngine.Components.Graphics2D;
using WaveEngine.Framework;
using WaveEngine.Framework.Graphics;
using WaveEngine.Framework.Managers;
using WaveEngine.Framework.Services;
using WaveEngine.Networking;
using WaveEngine.Networking.Messages;
using WaveEngine.TiledMap;

namespace Codigo
{
    [DataContract]
    public class WorldObjectSync : NetworkSyncComponent
    {
        [RequiredComponent]
        private WorldObject wo;

        [DataMember]
        private float lastHealth;
        [DataMember]
        private float lastMaxHealth;

        [DataMember]
        private float lastSpeed;
        [DataMember]
        private ActionEnum lastAction;
        [DataMember]
        private Boolean lastAttacking;
        public override bool NeedSendSyncData()
        {
            return lastAction!=wo.GetAction()|| lastHealth != wo.GetHealth() || lastMaxHealth != wo.GetMaxHealth() ||lastSpeed!=wo.genericSpeed || lastAttacking!=wo.attacking;
        }

        public override void WriteSyncData(OutgoingMessage writer)
        {   
            lastMaxHealth = wo.GetMaxHealth();
            lastHealth = wo.GetHealth();

            lastAction = wo.GetAction();

            lastSpeed = wo.genericSpeed;
            lastAttacking = wo.attacking;
            writer.Write(lastMaxHealth);
            writer.Write(lastHealth);

            writer.Write((int)lastAction);

            writer.Write(lastSpeed);
            writer.Write(lastAttacking);
        }

        public override void ReadSyncData(IncomingMessage m)
        {
            wo.SetMaxHealth(m.ReadSingle());
            wo.SetHealth(m.ReadSingle());
            
            ActionEnum act = (ActionEnum)m.ReadInt32();
            
            if (wo.GetAction() == ActionEnum.Move && act != ActionEnum.Move)
            {
                //We were moving and now we are not. Clearing the path
                Owner.FindComponent<MovementBehavior>().SetPathForClient(new List<LayerTile>());

            }
            //If its a building, and we have finished, recover it
            if (wo.GetAction() == ActionEnum.Build && act == ActionEnum.Idle&&!wo.Mobile())
                Owner.FindComponent<Sprite>().TintColor = Color.White;



            wo.SetAction(act);

            wo.genericSpeed = m.ReadSingle();
            wo.attacking = m.ReadBoolean();
            Player p = UIBehavior.ui.activePlayer;
            if (p != null && p.selectedWO == this.wo)
            {
                UIBehavior.ui.UpdateSpeedSlider();
            }
        }
    }
}