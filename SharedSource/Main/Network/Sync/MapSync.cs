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
    class MapSync : NetworkSyncComponent
    {
        public const int OCCUPIED=0, MOBILE=1, WO=2;
        
        private Map map;
        private MapStatusRecover recover;
        private List<MapChange> changes;
        protected override void Initialize()
        {
            base.Initialize();
            changes = new List<MapChange>();
           
        }
        protected override void ResolveDependencies()
        {
            base.ResolveDependencies();
            map = Owner.FindComponent<Map>();
            recover = Owner.FindComponent<MapStatusRecover>();
        }
        public override bool NeedSendSyncData()
        {
            return changes.Count > 0;
        }

        public override void ReadSyncData(IncomingMessage binaryReader)
        {
            int nChanges = binaryReader.ReadInt32();
            for(int i = 0; i < nChanges; i++)
            {
                int changeType = binaryReader.ReadInt32();
                int x = binaryReader.ReadInt32();
                int y = binaryReader.ReadInt32();
                if (changeType == OCCUPIED)
                {
                    map.SetTileOccupied(x, y, binaryReader.ReadBoolean());
                }
                else
                {
                    string woName = binaryReader.ReadString();
                    if (string.IsNullOrEmpty(woName)) {
                        if (changeType == MOBILE)
                        {
                            map.SetMobile(x, y, null);
                        }
                        else if (changeType == WO)
                        {
                            map.SetWorldObject(x, y, null);
                        }
                    }

                    else
                    {
                        Entity ent = EntityManager.Find(woName);
                        if (ent != null)
                        {
                            if (changeType == MOBILE)
                            {
                                map.SetMobile(x, y, ent.FindComponent<WorldObject>());
                            }
                            else if (changeType == WO)
                            {
                                map.SetWorldObject(x, y, ent.FindComponent<WorldObject>());
                            }
                        }
                        else
                        {
                            recover.AddChange(x, y, woName, changeType);
                        }
                    }
                }
            }
        }

        public override void WriteSyncData(OutgoingMessage writer)
        {
            writer.Write(changes.Count);
            for(int i = 0; i < changes.Count; i++)
            {
                MapChange change = changes[i];
                writer.Write(change.type);
                writer.Write(change.x);
                writer.Write(change.y);
                if (change.type == OCCUPIED)
                {
                    writer.Write(change.occupied);
                   
                }
                else  
                {
                    writer.Write(change.woName);

                }
            }
            changes.Clear();
        }

        public void AddChange(int x, int y, WorldObject wo, bool mobile)
        {
            MapChange change = new MapChange();
            change.x = x;
            change.y = y;
            change.type = mobile ? MOBILE : WO;
            if (wo != null)
            {  
                change.woName = wo.Owner.Name;
            }else
            {
                change.woName = string.Empty;
            }
            changes.Add(change);
        }

        public void AddChange(int x, int y, bool occupy)
        {
            MapChange change = new MapChange();
                change.x = x;
                change.y = y;
                change.type = OCCUPIED;
                change.occupied = occupy;
                changes.Add(change);  
        }

  
    }
}
