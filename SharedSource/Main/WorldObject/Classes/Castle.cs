using Codigo.Behaviors;
using Codigo.Components;
using System;
using WaveEngine.Components.Graphics2D;
using WaveEngine.Framework;
using WaveEngine.Framework.Graphics;
using WaveEngine.Networking.Messages;

namespace Codigo
{
    public class Castle:Component
    {
        private WorldObject[] castleWO;
        private int size = 0;
        private WaveEngine.Framework.Services.Random rnd = new WaveEngine.Framework.Services.Random();
        public Player player;

        public int GetSize()
        {
            return size;
        }
        public WorldObject GetPart(int index)
        {
            return index < size ? castleWO[index] : null;
        }
        public bool SetCastle(int xStart, int yStart, int width, int height,Player active,string playerID)
        {
            
            Map map = Map.map;
            if (map.InBounds(xStart, yStart) && map.InBounds(xStart+width-1,yStart+height-1)) {
                WorldObjectData data = WorldObjectData.GetData("Castle");
                size = width * height;
                WorldObject[] parts = new WorldObject[size];
                int index = 0;
                for (int i = xStart; i < xStart + width; i++)
                {
                    for(int j = yStart; j < yStart + height; j++)
                    {
                        if (map.GetWorldObject(i,j)!=null)
                        {
                            size--;
                        }
                        else
                        {
                            parts[index] = new WorldObject();

                            Entity ent = new Entity()
                                .AddComponent(parts[index]);
                            parts[index].SetWoName(data.woName);
                            parts[index].SetTraversable(data.traversable);
                            parts[index].SetMobile(data.mobile);
                            parts[index].SetHealth(data.health);
                            parts[index].SetMaxHealth(data.maxHealth);

                            parts[index].player=active;

                            ent
                                .AddComponent(new SpriteRenderer())
                                .AddComponent(new Transform2D
                                {
                                    Position = map.GetTilePosition(i, j), DrawOrder = 1f
                                })
                                .AddComponent(new Sprite(data.sprite))
                                .AddComponent(new WorldObjectTraits())
                                .AddComponent(new FogRevealBehavior())
                                .AddComponent(new TransformSync())
                                .AddComponent(new WorldObjectSync())
                                .AddComponent(new SyncBehavior())
                                .AddComponent(new FogAdjacents());
                           
                            CastlePart part = new CastlePart();
                            part.SetCastle(this);
                            ent.AddComponent(part);

                            ent.Name = ("WO-" + playerID + "-" + rnd.NextDouble() + "-" + data.woName);
                            map.SetWorldObject(i, j, parts[index]);

                            // for now, as this is traversable, we don't occupy the tile
                            // map.SetTileOccupied(i, j, true);

                            index++;
                        }
                    }
                }
                castleWO = new WorldObject[size];
                Array.Copy(parts, castleWO, size);
                
            }
            return size > 0;
        }
        public void DestroyPart(WorldObject castlePart)
        {
            for(int i = 0; i < castleWO.Length; i++)
            {
                if (castlePart == castleWO[i]) { castleWO[i] = null; }
            }
            if (IsDestroyed()) { Destroy(); }
        }
        private bool IsDestroyed()
        {
            bool destroyed = true;
            foreach (WorldObject wo in castleWO) {
                if (wo != null&&!wo.IsDestroyed())
                {
                    destroyed = false;
                    break;
                }
            }
            return destroyed;
        }
        private void Destroy()
        {
            if (player != null)
                player.castle = null;
            if (!Owner.IsDisposed)
                EntityManager.Remove(Owner);
        }
   

        public void SetCastleForClient(IncomingMessage receivedMessage,Player p)
        {
            WorldObjectData data = WorldObjectData.GetData("Castle");
            //castle size
            size = receivedMessage.ReadInt32();
            castleWO = new WorldObject[size];
            for (int index = 0; index < size; index++)
            {
                castleWO[index] = new WorldObject();
                Entity ent = new Entity();
                ent.Name = receivedMessage.ReadString();
                ent
                    .AddComponent(castleWO[index]);
                castleWO[index].SetWoName(data.woName);
                castleWO[index].SetTraversable(data.traversable);
                castleWO[index].SetMobile(data.mobile);
                castleWO[index].SetHealth(data.health);
                castleWO[index].SetMaxHealth(data.maxHealth);
                castleWO[index].player = p;
   
                //castle part name
                ent
                    .AddComponent(new SpriteRenderer())
                    //castle part x
                    //castle part y
                    .AddComponent(new Transform2D
                    {
                        Position = Map.map.GetTilePosition(receivedMessage.ReadInt32(), receivedMessage.ReadInt32()),
                        DrawOrder = 1f
                    })
                    .AddComponent(new Sprite(data.sprite))
                    .AddComponent(new WorldObjectTraits())
                    .AddComponent(new FogRevealBehavior())
                    .AddComponent(new TransformSync())
                    .AddComponent(new WorldObjectSync())
                    .AddComponent(new SyncBehavior())
                    .AddComponent(new FogAdjacents());

            }
        }
    }
}
