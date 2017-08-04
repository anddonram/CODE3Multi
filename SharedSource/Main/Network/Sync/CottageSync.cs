using System;
using System.Collections.Generic;
using System.Text;
using WaveEngine.Framework;
using WaveEngine.Networking;
using WaveEngine.Networking.Messages;

namespace Codigo.Components
{
    public class CottageSync : NetworkSyncComponent
    {
        [RequiredComponent]
        private Cottage cot;

        private int lastPeople;

        public override bool NeedSendSyncData()
        {
            return cot.GetOccupation() != lastPeople;
        }

        public override void ReadSyncData(IncomingMessage binaryReader)
        {
            int units = binaryReader.ReadInt32();
            List<WorldObject> wos = new List<WorldObject>();
            for (int i = 0; i < units; i++)
            {
             Entity ent=   EntityManager.Find(binaryReader.ReadString());
                if (!ent.IsDisposed)
                {
                   WorldObject person= ent.FindComponent<WorldObject>();
                    if (person != null)
                    {
                        wos.Add(person);
                    }
                }
            }
            cot.SetPeople(wos);
        }

        public override void WriteSyncData(OutgoingMessage writer)
        {
            lastPeople = cot.GetOccupation();

            writer.Write(lastPeople);
            foreach (WorldObject wo in cot.GetPeople())
            {
                writer.Write(wo.Owner.Name);
            }
        }
    }
}
