using Codigo.Behaviors;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using WaveEngine.Framework;
using WaveEngine.Networking;
using WaveEngine.Networking.Messages;

namespace Codigo.Components
{
    [DataContract]
    public class PersonSync : NetworkSyncComponent
    {

        [RequiredComponent]
        private Person person;

        private float lastTrees,lastBuildings, lastHeals, lastFights;

        public override bool NeedSendSyncData()
        {
            return person.buildings != lastBuildings || person.trees != lastTrees || person.heals != lastHeals || person.fights != lastFights;
        }

        public override void ReadSyncData(IncomingMessage binaryReader)
        {
            person.SetSyncValues(binaryReader);
        }

        public override void WriteSyncData(OutgoingMessage writer)
        {
            lastBuildings = person.buildings;
            lastTrees = person.trees;
            lastHeals=person.heals;
            lastFights=person.fights;
            writer.Write(lastBuildings);
            writer.Write(lastTrees);
            writer.Write(lastHeals);
            writer.Write(lastFights);
        }
    }
}
