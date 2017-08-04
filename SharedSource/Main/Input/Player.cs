using Codigo.Algorithms;
using Codigo.Behaviors;
using Codigo.Components;
using Codigo.GUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using WaveEngine.Common.Graphics;
using WaveEngine.Common.Input;
using WaveEngine.Common.Math;
using WaveEngine.Components.UI;
using WaveEngine.Framework;
using WaveEngine.Framework.Graphics;
using WaveEngine.Framework.Managers;
using WaveEngine.Framework.Services;
using WaveEngine.Framework.UI;
using WaveEngine.Networking;
using WaveEngine.TiledMap;

namespace Codigo
{
    public class Player : Behavior
    {
        /**
         * <summary>
         * This is true if the entity name and the clientId of this instance are the same.
         * Each client has as many player entities as players exist, but only one of them is localplayer in each client
         * </summary>
         */
        public bool isLocalPlayer = false;
        private Map map;
        private List<WorldObject> people;
        /**
         * <summary>
         * if this is active, this is the player the new objects will have as player
         * </summary>
         */
        public bool active = false;

        public string playerName;
        public Color playerColor;
      
        /**
         * <summary>
         * The team the player belongs to, -1 if it belongs to no team
         * </summary>
         */
        public int playerTeam = -1;


        public WorldObject selectedWO = null;
        /**
        * <summary>
        * This is true if we are selecting a path for a mobile to move
        * </summary>
        */
        private Boolean readingTiles = false;
        /**
        * <summary>
        * The path for the mobile we want to move
        * </summary>
        */
        private List<LayerTile> path = new List<LayerTile>();
        public Castle castle;

        /**
        * <summary>
        * The amount of wood the player has for buildings
        * </summary>
        */
        public float woodAmount { get; private set; }

        /**
         * <summary>
         * This is the tile the mouse is pointing at(not the selected tile)
         * </summary>
         */
        public LayerTile currentTile { get; private set; }
        /**
         * <summary>
         * The mobile in the current tile the mouse is pointing at
         * </summary>
         */
        private WorldObject currentMobile;
        /**
         * <summary>
         * The static wo in the current tile the mouse is pointing at
         * </summary>
         */
        private WorldObject currentWO;

        /**
         * <summary>
         * The last tile that was pressed on
         * </summary>
         */
        public LayerTile lastActionTile { get; private set; }
        protected override void Initialize()
        {
            base.Initialize();
            //health = maxHealth;

            people = new List<WorldObject>();
        }
        public List<WorldObject> GetPeople()
        {
            return people;
        }
        public bool HasEnoughWood(float amount)
        {
            return amount <= woodAmount;
        }
        public void AddWood(float amount)
        {
            woodAmount += amount;
            if (woodAmount < 0)
                woodAmount = 0;
        }
        public Boolean RemoveWood(float amount)
        {
            Boolean res = false;
            if (HasEnoughWood(amount))
            {
                woodAmount -= amount;
                res = true;
            }
            return res;
        }
        private NetworkService networkService;
        private KeysBehavior keysBehavior;
        private FogOfWar fog;

        private UIBehavior ui;
        public void SetResources()

        {
            woodAmount = 5000;


            if (isLocalPlayer)
            {
                networkService = (Owner.Scene as NetworkedScene).networkService;
                keysBehavior = RenderManager.ActiveCamera2D.Owner.FindComponent<KeysBehavior>();
                ui = UIBehavior.ui;
                map = Map.map;
                fog = FogOfWar.fog;
                PathDrawable draw = new PathDrawable();
                draw.SetPlayer(this, path);
                Owner.AddComponent(draw);
                
            }

        }

        protected override void Update(TimeSpan gameTime)
        {

            if (!active || !isLocalPlayer)

            {
                return;
            }
            
            currentTile = Map.map.GetTileByWorldPosition(GetMousePosition());


            //If the wo is inside other (p.e: a cottage) we deselect it
            if (selectedWO != null && (selectedWO.GetAction() == ActionEnum.Inside||selectedWO.IsDestroyed()))
            {
                selectedWO = null;
                SendSelect(selectedWO);

                ui.ClearActionButtons();
                lastActionTile = null;

            }


            if (ui.MouseOverGUI())
            {
                return;
            }


            //If we are selecting something we own
            if (selectedWO != null && !selectedWO.IsDestroyed() && selectedWO.player == this)
            {
                if (currentTile != null)
                {
                    currentMobile = Map.map.GetMobile(currentTile.X, currentTile.Y);
                    currentWO = Map.map.GetWorldObject(currentTile.X, currentTile.Y);

                    //If we are not creating a path
                    if (!readingTiles && selectedWO.IsMobile())
                    {
                        //But we want to start a new path
                        if (keysBehavior.IsCommandExecuted(CommandEnum.StartPath))
                        {
                            if (!selectedWO.IsActionBlocking() && map.Adjacent(currentTile, map.GetTileByWorldPosition(selectedWO.GetCenteredPosition())))
                            {
                                //Start path
                                readingTiles = true;
                                path.Add(Map.map.GetTileByWorldPosition(selectedWO.GetCenteredPosition()));
                            }
                        }
                    }

                    if (readingTiles && keysBehavior.IsCommandExecuted(CommandEnum.AddTile))
                    {
                        AddTile();
                    }
                    if (readingTiles && keysBehavior.IsCommandExecuted(CommandEnum.FinishPath))
                    {
                        //Right button no longer pressed, set path
                        MovementBehavior per = selectedWO.Owner.FindComponent<MovementBehavior>();
                        if (path.Count > 2 && per != null)
                        {
                            SendPath(path);
                        }
                        readingTiles = false;
                        path.Clear();
                    }
                    if (keysBehavior.IsCommandExecuted(CommandEnum.ActInTile))
                    {
                        readingTiles = false;
                        path.Clear();
                        if (ui.optionsAlreadyShown && currentTile == lastActionTile && selectedWO.IsMobile() && !selectedWO.IsActionBlocking())
                        {
                            for (int i = 0; i < ui.actions.Count; i++)
                                if (ui.actions[i].text == "Move")
                                {
                                    ui.actions[i].buttonAction(null, null);
                                    break;
                                }
                            lastActionTile = null;
                        }
                        else
                        {
                            HandleAction();
                        }
                    }

                    //Shortcuts for vicious ones

                    if (keysBehavior.IsCommandExecuted(CommandEnum.Chop))

                        Chop();
                    if (keysBehavior.IsCommandExecuted(CommandEnum.Fight))
                        Fight();
                    if (keysBehavior.IsCommandExecuted(CommandEnum.Build))
                        Build();
                    if (keysBehavior.IsCommandExecuted(CommandEnum.Heal))
                        Heal();
                    if (keysBehavior.IsCommandExecuted(CommandEnum.Train))
                        Train();

                    //Cottage shortcuts
                    Cottage cottage = selectedWO.Owner.FindComponent<Cottage>();
                    if (cottage != null)
                    {

                        if (keysBehavior.IsCommandExecuted(CommandEnum.CottageOne))
                        {
                            SendExit(1, currentTile);
                        }
                        if (keysBehavior.IsCommandExecuted(CommandEnum.CottageTwo))
                        {
                            SendExit(2, currentTile);
                        }
                        if (keysBehavior.IsCommandExecuted(CommandEnum.CottageThree))
                        {
                            SendExit(3, currentTile);
                        }
                        if (keysBehavior.IsCommandExecuted(CommandEnum.CottageFour))
                        {
                            SendExit(4, currentTile);
                        }
                        if (keysBehavior.IsCommandExecuted(CommandEnum.CottageFive))
                        {
                            SendExit(5, currentTile);
                        }
                        if (keysBehavior.IsCommandExecuted(CommandEnum.CottageSix))
                        {
                            SendExit(6, currentTile);
                        }

                    }
                    //Update cottage info any
                    ui.UpdateCottageUI(cottage);

                }
                if (keysBehavior.IsCommandExecuted(CommandEnum.Stop))
                    SendStop(selectedWO);
                if (keysBehavior.IsCommandExecuted(CommandEnum.Destroy))
                    Destroy();
            }

            //Select
            if (keysBehavior.IsCommandExecuted(CommandEnum.Select))
            {
                lastActionTile = null;
                Select();
            }
        }
        /**
         * <summary>
         * Assumes selectedWO is not null. Checks all posible actions between selectedWO and any of the mouse tile WOs
         * </summary>
         */
        private void HandleAction()
        {
            //We can't do anything if we are moving (you'd better not modify that)
            if (!selectedWO.IsActionBlocking())
            {
                ui.ClearActionButtons();
                //As always, when handling actions we must save the instances at the moment
                //If you remove this lines, when a person tries to exit a cottage, for example,
                //the tile for exit will be the currentTile below the button at the moment it is pressed (which will probably be null), and not the tile that was selected
                WorldObject currentWO = null;
                WorldObject currentMobile = null;
                LayerTile currentTile = this.currentTile;
                if (fog == null || fog.IsVisible(currentTile.X, currentTile.Y))
                {
                    currentWO = this.currentWO;
                    currentMobile = this.currentMobile;
                }
                else if (fog.IsPartiallyVisible(currentTile.X, currentTile.Y))
                {
                    currentWO = fog.GetRevealedWO(currentTile.X, currentTile.Y);
                }
                foreach (ActionBehavior act in selectedWO.allActions)
                {
                    if (act.CanShowButton(currentMobile))
                    {       
                        ui.AddAction(act.GetCommandName(currentMobile),()=>HandleMovementAction(currentMobile, currentTile, act));

                    }
                    if (act.CanShowButton(currentWO))
                    {      
                        ui.AddAction(act.GetCommandName(currentWO), () => HandleMovementAction(currentWO, currentTile,  act));
                    }
                }

                if (selectedWO.IsMobile())
                {
                    //A button to move if "we see" the tile is free
                    if ((currentWO == null || currentWO.IsTraversable(selectedWO)) && (currentMobile == null || currentMobile.IsTraversable(selectedWO)))
                    {     
                        ui.AddAction("Move", () => CalculatePathDStar(selectedWO, currentTile));
                    }

                }
                Cottage cottage = selectedWO.Owner.FindComponent<Cottage>();
                if (cottage != null)
                {
                    for (int i = 0; i < cottage.GetPeople().Count; i++)
                    {
                        //Careful with event handlers, actions in loops, because the i variable wont work
                        int aux = i;
                        WorldObject peopleInside = cottage.GetPeople()[aux];
                        if ((currentMobile == null || currentMobile.IsTraversable(peopleInside)) &&
                            (currentWO == null || currentWO.IsTraversable(peopleInside)))
                            
                            ui.AddAction(string.Format("Exit cottage {0}: {1}", aux + 1, cottage.GetPeople()[aux].GetWoName()), () => SendExit(aux + 1, currentTile));
                    }
                }

                if (ui.actions.Count == 1)
                {
                    //if there is only an action, we directly execute it
                    ui.ExecuteAction(0);

                }
                else if (ui.actions.Count > 1)
                {

                    for (int i = 0; i < ui.actions.Count; i++)
                    {
                        //Careful with event handlers, actions in loops, because the i variable wont work
                        int aux = i;
                        if (i >= ui.buttons.Count)
                        {
                            //We have less buttons than actions, create a new one
                            ui.CreateActionButton();
                        }
                        ui.UpdateActionButton(aux);

                    }
                    ui.optionsAlreadyShown = true;
                    lastActionTile = currentTile;
                    WaveServices.Layout.PerformLayout(Owner.Scene);
                }
            }
        }
        /**
         * <summary>
         * Method for executing an action if requires adjacency, 
         * or computes a path and stores the action for when it finishes moving closer
         * </summary>
         */
        private void HandleMovementAction(WorldObject wo, LayerTile currentTile, ActionBehavior act)
        {
            if (selectedWO != null && !selectedWO.IsDestroyed() && !selectedWO.IsActionBlocking())
                if (wo.IsAdjacent(selectedWO))
                    SendCommand(act,wo,false);
                else if (selectedWO.IsMobile())
                {
                    List<LayerTile> dPath = new List<LayerTile>();

                    DStarLite dstar = selectedWO.Owner.FindComponent<DStarLite>();

                    LayerTile start = map.GetTileByWorldPosition(selectedWO.GetCenteredPosition());
                    if (currentTile != start)
                    {
                        dPath = dstar.DStar(start, currentTile);
                    }
                    else
                    {
                        //we are in the same tile, we move to an adjacent an enqueue action
                        dPath.Add(start);
                        List<LayerTile> neighbors = IsAdjacentTileFree(start, dstar);
                        if (neighbors.Count > 0)
                        {
                            dPath.Add(neighbors[0]);
                        }

                    }
                    if (dPath.Count > 1)
                    {
                        SendPath(dPath);
                        SendCommand(act, wo, true);
                    }
                }
        }

        private void SendCommand(ActionBehavior act, WorldObject wo, bool store)
        {
            if (selectedWO!=null&&!selectedWO.IsDestroyed() && wo != null && !wo.IsDestroyed())
            {
                var message = networkService.CreateClientMessage();
                message.Write(NetworkedScene.WO_ACTION);
                message.Write(selectedWO.Owner.Name);
                message.Write(wo.Owner.Name);
                message.Write((int)act.GetCommand());
                message.Write(store);

                networkService.SendToServer(message, WaveEngine.Networking.Messages.DeliveryMethod.ReliableOrdered);
            }
        }

        private void CalculatePathDStar(WorldObject sel,LayerTile end)
        {

            if (sel != null && !sel.IsDestroyed() && !sel.IsActionBlocking())
            {
                LayerTile start = map.GetTileByWorldPosition(sel.GetCenteredPosition());
                DStarLite dstar = sel.Owner.FindComponent<DStarLite>();
                if (dstar != null)
                {
                    List<LayerTile> dPath = dstar.DStar(start, end);
                    Trace.WriteLine("path: " + dPath.Count.ToString());
                    SendPath(dPath);
                    
                }

            }
        }
        
        private void SendExit(int number,LayerTile currentTile)
        {
            if (currentTile != null && !selectedWO.IsDestroyed())
            {
                var message = networkService.CreateClientMessage();
                message.Write(NetworkedScene.EXIT_COTTAGE);

                message.Write(selectedWO.Owner.Name);
                message.Write(number);
                message.Write(currentTile.X);
                message.Write(currentTile.Y);
                networkService.SendToServer(message, WaveEngine.Networking.Messages.DeliveryMethod.ReliableOrdered);
            }
        }


        public void SendStop(WorldObject wo)
        {
            if (!wo.IsDestroyed())
            {
                readingTiles = false;
                path.Clear();

                var message = networkService.CreateClientMessage();
                message.Write(NetworkedScene.STOP);
                message.Write(wo.Owner.Name);
                networkService.SendToServer(message, WaveEngine.Networking.Messages.DeliveryMethod.ReliableOrdered);
            }
        }
        public void SendAttacking(WorldObject wo)
        {
            if (!wo.IsDestroyed())
            {
                readingTiles = false;
                path.Clear();

                var message = networkService.CreateClientMessage();
                message.Write(NetworkedScene.ATTACK);
                message.Write(wo.Owner.Name);
                networkService.SendToServer(message, WaveEngine.Networking.Messages.DeliveryMethod.ReliableOrdered);
            }
        }

        public void SendPath(List<LayerTile> path)
        {
            //Right button no longer pressed, set path
            readingTiles = false;

            var message = networkService.CreateClientMessage();
            message.Write(NetworkedScene.MOVE);
            message.Write(selectedWO.Owner.Name);
            message.Write(false);
            message.Write(path.Count);
            foreach (LayerTile tile in path)
            {
                message.Write(tile.X);
                message.Write(tile.Y);
                Trace.WriteLine("X: " + tile.X + ", Y:" + tile.Y);
            }
            networkService.SendToServer(message, WaveEngine.Networking.Messages.DeliveryMethod.ReliableOrdered);

            path.Clear();
        }
       
        [Obsolete]
        private void CalculatePath(MovementBehavior per)
        {
            Trace.WriteLine("calculating path");
            LayerTile start = map.GetTileByWorldPosition(selectedWO.GetCenteredPosition());
            WaveServices.TaskScheduler.CreateTask(async () =>
            {
                List<LayerTile> dPath = AStar.Astar(currentTile, start);
                Trace.WriteLine("path: " + dPath.Count.ToString());
                await WaveServices.Dispatcher.RunOnWaveThread(
                    () =>
                    {
                        if (!selectedWO.IsDestroyed() && !selectedWO.IsActionBlocking())
                        {
                            SendPath(dPath);
                        }
                    }
                    );
            });

        }

        private void Select()
        {
            WorldObject previousWO = selectedWO;
            if (currentTile != null)
            {
                //check visibility
                if (fog == null || fog.IsVisible(currentTile.X, currentTile.Y))
                {
                    WorldObject wo = map.GetMobile(currentTile.X, currentTile.Y);
                    WorldObject wo2 = map.GetWorldObject(currentTile.X, currentTile.Y);

                    if (wo2 != null && !wo2.IsSelectable(this))
                    {
                        return;
                    }

                    //check visibility
                    if (wo != null && wo.IsSelectable(this))
                    {
                        if (wo == selectedWO && wo2 != null && wo2.IsSelectable(this))
                        {
                            selectedWO = wo2;
                        }
                        else
                        {
                            selectedWO = wo;
                        }

                    }
                    else if (wo2 != null && wo2.IsSelectable(this))
                    {
                        selectedWO = wo2;
                    }
                    else
                    {
                        selectedWO = null;
                    }
                }
                else
                {
                    selectedWO = null;
                }
                ui.ClearActionButtons();
            }
            if (selectedWO != previousWO)
            {
                SendSelect(selectedWO);
                ui.UpdateSpeedSlider();
            }
            path.Clear();
        }
        private void SendSelect(WorldObject wo)
        {
            var message = networkService.CreateClientMessage();
            message.Write(NetworkedScene.SELECT);
            string woName = (wo == null||wo.IsDestroyed()) ? string.Empty : wo.Owner.Name;
            message.Write(Owner.Name);
            message.Write(woName);
            networkService.SendToServer(message, WaveEngine.Networking.Messages.DeliveryMethod.ReliableOrdered);
        }
        private void Fight()
        {
            FightBehavior fight = selectedWO.Owner.FindComponent<FightBehavior>();
            if (fight != null)
            {
                WorldObject wo = currentMobile;
                if (wo == null)
                    wo = currentWO;

                SendCommand(fight, wo, false);
            }    
        }
      
        private void Build()
        {
            BuildBehavior build = selectedWO.Owner.FindComponent<BuildBehavior>();
            if (build != null)
            {
                WorldObject wo = currentMobile;
                if (wo == null)
                    wo = currentWO;
                SendCommand(build, wo, false);

            }
        }

        private void Heal()
        {
            HealBehavior heal = selectedWO.Owner.FindComponent<HealBehavior>();
            if (heal != null)
            {
                WorldObject wo = currentMobile;
                if (wo == null)
                    wo = currentWO;
               
                SendCommand(heal, wo, false);
            }
        }

        private void Train()
        {
            TrainBehavior train = selectedWO.Owner.FindComponent<TrainBehavior>();
            if (train != null)
            {
                WorldObject wo = currentMobile;
                if (wo == null)
                    wo = currentWO;
                SendCommand(train, wo, false);
                
            }
        }
      
        private void Chop()
        {
            ChopBehavior chop = selectedWO.Owner.FindComponent<ChopBehavior>();
            if (chop != null)
            {
                WorldObject wo = currentWO;
                SendCommand(chop, wo, false);
            }
        }

        /**
        * <summary>
        * destroy a wo selected wo if it's yours
        * first the mobile ones, then the static ones
        * </summary>
        */

        private void Destroy()
        {
            //if it belongs to us
            if (selectedWO != null && selectedWO.player == this && !selectedWO.IsDestroyed())
            {
                selectedWO.Destroy();
            }
        }

        /**
         * <summary>
          * Adds the tile that the mouse is pointing at to path list.
          * </summary>
          */
        private void AddTile()
        {
            LayerTile lastElement = null;
            if (path.Count != 0)
                lastElement = path.ElementAt(path.Count - 1);
            if (currentTile != null && !path.Contains(currentTile) && map.Adjacent(currentTile, lastElement)) //Cuando haya restricciones para pasar por una casilla se deben añadir también aquí
            {
                path.Add(currentTile);
            }
        }

   
        private Vector2 GetMousePosition()
        {       
           return keysBehavior.currentMousePosition;
        }
        /**
         * <summary>
         * Finds adjacent and possibly free tiles to the one given
         * </summary>
         */
        private List<LayerTile> IsAdjacentTileFree(LayerTile tile, DStarLite dstar)
        {
            List<LayerTile> res = new List<LayerTile>();
            if (tile != null)
            {
                foreach (Point neighborPoint in fog.Adjacents(tile, dstar.adjPoints))
                {
                    LayerTile u = map.GetTileByMapCoordinates(neighborPoint.X, neighborPoint.Y);
                    if (dstar.CalculateDistance(u)!=float.PositiveInfinity)
                    {
                        res.Add(u);
                    }
                }
                }
            return res;
        }
    }
}
