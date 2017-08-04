using Codigo.Behaviors;
using Codigo.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using WaveEngine.Common.Math;
using WaveEngine.Components.Graphics2D;
using WaveEngine.Framework;
using WaveEngine.Framework.Graphics;
using WaveEngine.Networking;
using WaveEngine.TiledMap;

namespace Codigo
{
    public class WorldObject : Component
    {
        public NetworkedScene networkedScene { get; private set; }
        /**
         * <summary>
         * can the wo move? can it be traversed?
         * </summary>
         */
        private bool traversable, mobile;
        /**
         * <summary>
         * If <c>true</c>, it will attack any adjacent enemy automatically
         * </summary>
         */
        public bool attacking;

        public WaveEngine.Components.GameActions.MoveTo2DGameAction animation;
        /**
         * <summary>The name of the world object: a person, an obstacle, a tree...</summary>
         */
        protected string woName = "worldObject";

        /**
         * <summary>The owner of the world object</summary>
         */
        public Player player;
        /**
         * <summary>Max health</summary>
         */
        protected float maxHealth = 100;
        /**
         * <summary>Current health</summary>
         */
        protected float health=10;
        /**
        * <summary>
        * The cell in which the unit is placed
        * </summary>
        */

        protected int x, y;
        
        protected ActionEnum action;
        
        /**<summary>
         * If mobile, this is the time it takes to move between two tiles.
         * But can be used by other things!! such as, for the candle, the time it takes to melt 1 unit
         * </summary>
         */
        public float genericSpeed { get; set; } = 3;

        public void SetMaxHealth(float v)
        {
            maxHealth = v;
            
            //Take damage 0 resets health to be lower or equal to maxhealth
            TakeDamage(0);
        }


        [RequiredComponent]
        public Transform2D transform { get; private set; }


        [RequiredComponent(false)]
        private WorldObjectTraits traits;
        
        public void SetHealth(float v)
        {
            health = v;
            //Take damage 0 resets health to be lower or equal to maxhealth
            TakeDamage(0);
        }


        public ActionBehavior[] allActions;

        protected override void Initialize()
        {
            base.Initialize();
            //health = maxHealth;

            action = ActionEnum.Idle;

            networkedScene = Owner.Scene as NetworkedScene;

            attacking = false;

        }

        protected override void ResolveDependencies()
        {
            base.ResolveDependencies();
            allActions = Owner.Components.Where(x=>x is ActionBehavior).Cast<ActionBehavior>().ToArray();
        }

        /**
         * <summary>
         * Substracts damage amount to current health.
         * If damage is negative, it results in the health being restored
         * </summary>
         * <returns>
         * true if this wo has been destroyed (or health 0, because isdestroyed is not true inmediately), false otherwise
         * </returns>
         */
        public Boolean TakeDamage(float damage)
        {
            health -= damage;
            if (health <= 0)
            {
                Destroy();
            }else if (health > maxHealth) {
                health = maxHealth;
            }
            return IsDestroyed()||health<=0;
        }

        public void Heal(float healing)
        {
            if (health + healing <= maxHealth)
            {
                health+= healing;
            }
            else
            {
                health = maxHealth;
            }
        }

        public void Destroy()
        {
            if (!IsDestroyed())
            {
                NetworkService networkService = (Owner.Scene as NetworkedScene).networkService;

                var message = networkService.CreateClientMessage();
                message.Write(NetworkedScene.DESTROY_WO);
                message.Write(Owner.Name);
                networkService.SendToServer(message, WaveEngine.Networking.Messages.DeliveryMethod.ReliableOrdered);
            }

        }
        public void SDestroy()
        {
            NetworkService networkService = (Owner.Scene as NetworkedScene).networkService;
            
            var message = networkService.CreateServerMessage();
            message.Write(NetworkedScene.DESTROY_WO);
            message.Write(Owner.Name);
            networkService.SendToClients(message, WaveEngine.Networking.Messages.DeliveryMethod.ReliableOrdered);
        }
        /**
         * ClientDestroy
         */
        public void CDestroy()
        {
            if ((Owner.Scene as NetworkedScene).isHost) {
                Map map = Map.map;
                if (!IsTraversable(null))
                {
                    map.SetTileOccupied(x, y, false);
                    if (player != null)
                        player.GetPeople().Remove(this);
                }
                if (IsMobile())
                {
                    if (this.GetWoName() == "Person"&&player!=null)
                    {
                        player.GetPeople().Remove(this);
                    }

                    MovementBehavior per = Owner.FindComponent<MovementBehavior>();
                    if (per.nextTile != null)
                        map.SetTileOccupied(per.nextTile.X, per.nextTile.Y, false);
                    map.SetMobile(x, y, null);
                }
                else if (this.GetWoName() == "Bridge")
                {
                    WorldObject person = map.GetMobile(x, y);
                    if (map.GetMobile(x, y) != null)
                    {
                        //We must nullify the coordinates so that when it is destroyed, 
                        //the tile from in the bridge will not be set free
                        map.SetMobile(person.GetX(), person.GetY(), null);
                        person.SetX(-1);
                        person.SetY(-1);
                        
                        person.Destroy();
                    }
                    map.SetTileOccupied(x, y, true);
                    WorldObject water = Owner.FindComponent<BridgeTraits>().water;
                    water.Owner.IsVisible = true;
                    map.SetWorldObject(x, y, water);
                }
                else
                {
                    map.SetWorldObject(x, y, null);
                }


            }
            else
            {
                if (this.GetWoName() == "Bridge")
                {
                    //This makes water visible again in client
                    WorldObject water = Owner.FindComponent<BridgeTraits>().water;
                    water.Owner.IsVisible = true;
                }
            }

            Player active=UIBehavior.ui.activePlayer;
            if (active != null && active.selectedWO == this)
            {
                active.selectedWO = null;
                UIBehavior.ui.ClearActionButtons();
            }


            if (Owner.Parent != null)
            {
                Castle c = Owner.Parent.FindComponent<Castle>();
                if (c != null)
                {
                    c.DestroyPart(this);
                }
                // For some reasons detach+remove do not work and persons keep fighting castles, so we must do this if else 
                // and removechild already disposes the castle part
                if (!Owner.IsDisposed)
                    Owner.Parent.RemoveChild(Owner.Name);
            }
            else
            {
                EntityManager.Remove(Owner);
            }


        }


        public float GetHealth()
        {
            return health;
        }
        public float GetMaxHealth()
        {
            return maxHealth;
        }
        public int GetX()
        {
            return x;
        }
        public int GetY()
        {
            return y;
        }
        /**
         * <summary>
         * Sets the X coordinate of the world object cell.
         * DON'T USE UNLESS WITH SetWorldObject METHOD IN Map or for network syncing
         * </summary>
         */
        public void SetX(int x)
        {
            this.x = x;
        }
        /**
         * <summary>
         * Sets the Y coordinate of the world object cell.
         * DON'T USE UNLESS WITH SetWorldObject METHOD IN Map or for network syncing
         * </summary>
         */
        public void SetY(int y)
        {
            this.y = y;
        }


        public string GetWoName()
        {
            return traits == null ? woName : traits.GetWoName();
        }

        public bool IsTraversable(WorldObject other) { return traits==null?traversable:traits.IsTraversable(other); }
        public bool IsMobile() { return traits == null ? mobile : traits.IsMobile(); }

        public bool IsVisible(Player p) { return traits == null ? true : traits.IsVisible(p); }

        public bool IsSelectable(Player p) { return traits == null ? false : traits.IsSelectable(p); }
        public bool IsAttackable(WorldObject wo) { return traits == null ? true : traits.IsAttackable(wo); }

        public void SetTraversable(bool traversable) { this.traversable = traversable; }
        public void SetMobile(bool mobile) { this.mobile = mobile; }

        public void SetWoName(string woName)
        {
            if (!string.IsNullOrEmpty(woName))
            {
                this.woName = woName;
            }
        }

        public bool IsDestroyed() { return Owner.IsDisposed; }

        public Boolean IsAdjacent(WorldObject other)
        {
            return (other != null) && 
                (((this.GetX() == other.GetX()) && (this.GetY() == (other.GetY() + 1))) 
                || ((this.GetX() == other.GetX()) && (this.GetY() == (other.GetY() - 1))) 
                || ((this.GetY() == other.GetY()) && (this.GetX() == (other.GetX() + 1))) 
                || ((this.GetY() == other.GetY()) && (this.GetX() == (other.GetX() - 1))));
        }

        public ActionEnum GetAction()
        {
            return action;
        }
        public void SetAction(ActionEnum ae)
        {
            this.action = ae;
        }
        public void Stop()
        {
            this.action = ActionEnum.Idle;
        }
        public void AttackToggle()
        {
            Boolean aux = !this.attacking;
            this.attacking = aux;
            Trace.WriteLine(this.attacking);
        }



        /**
         * <summary>
         * WARNING: DO NOT USE THIS, EXCEPT FOR WOTRAITS, instead use IsMobile()
         * </summary>
         * 
         */
        public bool Mobile()
        {
            return mobile;
        }
        /**
        * <summary>
        * WARNING: DO NOT USE THIS, EXCEPT FOR WOTRAITS, instead use IsTraversable()
        * </summary>
        * 
        */
        public bool Traversable()
        {
            return traversable;
        }
        /**
         * <summary>
         * WARNING: DO NOT USE THIS, EXCEPT FOR WOTRAITS, instead use GetWoName()
         * </summary>
         * 
         */
        public string WoName()
        {
            return woName;
        }
		public Vector2 GetCenteredPosition()
        {
            return this.transform.Position + WorldObjectData.center;
        }


        /**
         * <summary>
         * Returns true if both players are equal or both are not null and have the same team number above -1 (which means no team)
         * </summary>
         */
        public bool IsSameTeam(Player p)
        {
            return (player == p || (player != null && p != null && player.playerTeam >= 0 && player.playerTeam == p.playerTeam));
        }

        /**
         * <summary>
         * This is true if the current action involves a movement or renders the WO inaccessible, like inside a cottage.
         * Use in behaviors to check if a new command can be received.
         * </summary>
         */
        public bool IsActionBlocking()
        {
            return action == ActionEnum.Move ||
                action == ActionEnum.Enter ||
                action == ActionEnum.Exit ||
                action == ActionEnum.Switch ||
                action == ActionEnum.Inside;
        }

        /**<summary>
         * The list of actions to be executed in sequence
         * </summary>
         */
        public List<Action> pendingActions = new List<Action>();

        /**<summary>
         * Adds an action to the queue
         * </summary>
         */
        public void EnqueueAction(Action p)
        {
            pendingActions.Add(p);
        }


        public void ExecuteAction()
        {
            if (pendingActions.Count > 0)
            {
                pendingActions[0]();
                pendingActions.Clear();
            }
        }

    }

}
