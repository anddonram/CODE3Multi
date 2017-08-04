using Codigo.Behaviors;
using Codigo.Components;
using System;
using System.Collections.Generic;
using System.Text;
using WaveEngine.Common.Graphics;
using WaveEngine.Common.Math;
using WaveEngine.Components.Graphics2D;
using WaveEngine.Framework;
using WaveEngine.Framework.Graphics;

namespace Codigo.Data
{
    public class CottageData : WorldObjectData
    {
        public CottageData()
        {
            woName = "Cottage";
            traversable = false;
            mobile = false;
            maxHealth = 100;
            health = 1;
            sprite = WaveContent.Assets.cottage_png;
            woodRequired = 250;
            isBuilding = true;
        }

        public override Entity AddComponents(Entity ent)
        {

            WorldObject wo = ent.FindComponent<WorldObject>();
            Entity child = new Entity()
                                .AddComponent(new Transform2D
                                {
                                    Position = new Vector2(0, 0),
                                    LocalDrawOrder = -0.2f,     //the shirt is rendered in front of the person
                                })
                                .AddComponent(new SpriteRenderer())
                                        .AddComponent(new Sprite(WaveContent.Assets.flag_png) { TintColor = wo.player == null ? Color.White : wo.player.playerColor })
                              ;
            ent.AddChild(child);
            return ent.AddComponent(new Cottage())
                                 .AddComponent(new CottageFightTraits())
                                 .AddComponent(new FightBehavior())
                                 .AddComponent(new CottageFogAdjacents())
                                  .AddComponent(new CottageSync());
        }
    }
}
