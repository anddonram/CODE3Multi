using System;
using System.Collections.Generic;
using System.Text;
using WaveEngine.Framework;
using static Codigo.Components.MapSync;
namespace Codigo.Behaviors
{      
    /**
          * <summary>
          * Explanation:
          * When a WO in the server is created, the tiles on the map are occupied and set. 
          * This makes two extra messages be sent, apart from the wo creation: one for settileoccupied and other for setmobile/setworldobject
          * It is necessary to save this data so that it can be used when the wo create message arrives
          * Create from data received to allow sync.
          * Compare with Create(LayerTile) to ensure no behaviors are being left behind
          * </summary>
          * 
          */
    class MapStatusRecover : Behavior
    {
        /**
         * <summary>
         * the changes are stored here
         * </summary>
         */
        private List<MapChange> changes=new List<MapChange>();
        /**
         * <summary>
        * the changes to be deleted are added here and cleared at the end of the frame
        * </summary>
        */
        private List<MapChange> toRemove = new List<MapChange>();
        protected override void Update(TimeSpan gameTime)
        {
           foreach(MapChange change in changes)
            {
                Entity ent = EntityManager.Find(change.woName);
                if (ent != null)
                {
                    if (change.type == MOBILE)
                    {
                        Map.map.SetMobile(change.x, change.y, ent.FindComponent<WorldObject>());
                    }
                    else if (change.type == WO)
                    {
                        Map.map.SetWorldObject(change.x, change.y, ent.FindComponent<WorldObject>());
                    }
                    toRemove.Add(change);
                }
            }
            if (toRemove.Count > 0)
            {
                foreach (MapChange change in toRemove)
                    changes.Remove(change);
                toRemove.Clear();
            }
        }
        /**
         * <summary>
         * Stores a change for later use
         * </summary>
         */
        public void AddChange(int x, int y, string woName,int type)
        {
            MapChange change = new MapChange();
            change.x = x;
            change.y = y;
            change.type = type;
            change.woName = woName;
            changes.Add(change);
            
        }
    }
}
