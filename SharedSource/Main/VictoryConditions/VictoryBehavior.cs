using Codigo.VictoryConditions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using WaveEngine.Common.Graphics;
using WaveEngine.Components.UI;
using WaveEngine.Framework;
using WaveEngine.Framework.Services;
using WaveEngine.Framework.UI;
using WaveEngine.Networking.Messages;

namespace Codigo.Behaviors
{
    public class VictoryBehavior : Behavior
    {
        private VictoryCondition[] conditions;
        private UIBehavior uibehavior;
        private bool finished;

        private NetworkedScene ns;
        protected override void Initialize()
        {
            base.Initialize();
            //health = maxHealth;

            conditions = Owner.FindComponents<VictoryCondition>(isExactType:false);
            finished = false;
            ns = Owner.Scene as NetworkedScene;
        }
       
        protected override void ResolveDependencies()
        {
            base.ResolveDependencies();
            uibehavior = Owner.FindComponent<UIBehavior>();
        }
        public void Enable()
        {          
            foreach (VictoryCondition v in conditions)
            {
                v.active = true;
            }
        }
        protected override void Update(TimeSpan gameTime)
        {
            if (!ns.isHost)
                return;
            if (!finished)
                foreach (VictoryCondition v in conditions)
                {
                    if (v.active)
                    {
                        Player p = v.GetWinner();
                        if (p != null)
                        {
                            Trace.WriteLine("The winner is " + p.playerName);
                            Trace.WriteLine( v.GetName());
                            finished = true;
                            SendFinish(p.playerName, v.GetName());
                            break;
                        }
                    }
                }
        }

        public void Finish(string playerName, string v)
        {

            TextBlock t = new TextBlock { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Height = 100, Foreground = Color.Black, Margin = new Thickness(0, 0, 0, 0), Width = 400, Text = playerName + " wins", TextAlignment = TextAlignment.Center };
            Owner.Scene.EntityManager.Add(t);

            TextBlock t2 = new TextBlock { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Height = 100, Foreground = Color.Black, Margin = new Thickness(0, 0, 0, 50), Width = 400, Text = v, TextAlignment = TextAlignment.Center };
            Owner.Scene.EntityManager.Add(t2);
            WaveServices.Layout.PerformLayout();
            Owner.Scene.Pause();
            WaveServices.TimerFactory.CreateTimer(TimeSpan.FromSeconds(3), () =>
            {
                ns.Cancel();

            },false);

        }
        private void SendFinish(string playerName, string v)
        {
            OutgoingMessage mes = ns.networkService.CreateServerMessage();
            mes.Write(NetworkedScene.FINISH);
            mes.Write(playerName);
            mes.Write(v);
            ns.networkService.SendToClients(mes, DeliveryMethod.ReliableOrdered);
        }
    }
}
