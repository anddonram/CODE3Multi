using Codigo.Behaviors;
using Codigo.Components;
using Codigo.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using WaveEngine.Common.Math;
using WaveEngine.Components.UI;
using WaveEngine.Framework;
using WaveEngine.Framework.Graphics;
using WaveEngine.Framework.UI;
using WaveEngine.Networking;
using WaveEngine.Networking.Messages;
using WaveEngine.TiledMap;
using static Codigo.NetworkedScene;
namespace Codigo
{
    public class ServerDispatcher
    {
        /**
         * <summary>
         * The maximum number of players for a match, between 2 and 4
         * </summary>
         */
        private int maxPlayers;
        private NetworkedScene networkedScene;
        private NetworkService networkService;

        /**<summary>
         * The slider for controlling the number of max players
         * </summary>
         */
        private Slider maxPlayersSelector;
        /**<summary>
         * A label for the max players slider
         * </summary>
         */
        private TextBlock maxPlayersLabel;

        /**<summary>
         * A label for the max field selector
         * </summary>
         */
        private TextBlock fieldLabel;
        /**<summary>
         * A switcher that controls whether we want a big or small field
         * </summary>
         */
        private ToggleSwitch2 fieldSelector;
        /**
         * <summary>
         * The number of players who are ready to update its UI and start the game
         * </summary>
         */
        private int playerUI = 0;
        /**
         * <summary>
         * The ids and names of the players for the match.
         * </summary>
         */
        private List<string> ids,names;
        /**
         * <summary>
         * The color indexes that are being used by a player.
         * </summary>
         */
        private HashSet<int> chosenColors;

        public ServerDispatcher(NetworkedScene n)
        {
            this.networkedScene = n;
            this.networkService = networkedScene.networkService;
            ids = new List<string>();
            names = new List<string>();
            chosenColors = new HashSet<int>();
        }
        public void HostMessageDispatcher(object sender, IncomingMessage receivedMessage)
        {

            if (networkedScene.IsDisposed)
                return;
            int messageType = receivedMessage.ReadInt32();
            switch (messageType)
            {
                case START:
                    HostNewPlayer(sender, receivedMessage);
                    break;
                case ADD_WO:
                    RerouteWOData(sender, receivedMessage);
                    break;
                case REMOVE_PLAYER:
                    SendRemove(sender, receivedMessage);
                    break;
                case CHANGE_PLAYER:
                    ChangePlayerData(sender,receivedMessage);
                    break;
                case UI:
                    ForceUI(sender, receivedMessage);
                    break;
                case DESTROY_WO:
                    RerouteDestroy(sender, receivedMessage);
                    break;
                case MOVE:
                    Move(sender, receivedMessage);
                    break;
                case STOP:
                    Stop(sender, receivedMessage);
                    break;
                case EXIT_COTTAGE:
                    ExitCottage(sender, receivedMessage);
                    break;
                case SELECT:
                    Select(sender, receivedMessage);
                    break;
                case CHANGE_SPEED:
                    ChangeSpeed(sender,receivedMessage);
                    break;
				case WO_ACTION:
                    ExecuteAction(sender, receivedMessage);
                    break;

				case ATTACK:
                    ChangeAttacking(sender, receivedMessage);
                    break;                default:
                    Trace.WriteLine("Mensaje erróneo recibido desde el cliente");
                    break;
            }
        }
        /**
         * <summary>
         * Changes the speed of a WO
         * </summary>
         */
        private void ChangeSpeed(object sender, IncomingMessage receivedMessage)
        {
            Entity owner = networkedScene.EntityManager.Find(receivedMessage.ReadString());

            if (owner != null && !owner.IsDisposed)
            {
                owner.FindComponent<WorldObject>().genericSpeed = receivedMessage.ReadSingle();
            }
        }
        /**
        * <summary>
        * Modifies the color or team of a player
        * </summary>
        */
        private void ChangePlayerData(object sender, IncomingMessage receivedMessage)
        {
           string playerIdentifier = receivedMessage.ReadString();
           int dataType= receivedMessage.ReadInt32();
           Entity ent= networkedScene.EntityManager.Find("playerInfo"+playerIdentifier);
           if (ent != null)
            {
                PlayerInfo info = ent.FindComponent<PlayerInfo>();
                bool next = receivedMessage.ReadBoolean();
                switch (dataType)
                {
                    case PlayerInfo.READY:
                        info.ready = !info.ready;
                        info.ChangeReady();
                        
                        break;
                    case PlayerInfo.COLOR:
                        if (!info.ready)
                        {
                            info.playerColor = FindNextColor(info.playerColor);
                            info.nextColor.Text = ClientDispatcher.colorNames[info.playerColor];
                            info.nextColor.Foreground = ClientDispatcher.colors[info.playerColor];
                            info.nextColor.BorderColor = ClientDispatcher.colors[info.playerColor];
                        }
                        break;
                    case PlayerInfo.TEAM:
                        if (!info.ready)
                        {
                            if (next)
                            {
                                info.playerTeam = info.playerTeam + 1;
                                if (info.playerTeam >= ids.Count)
                                {
                                    info.playerTeam = -1;
                                }
                            }
                            else
                            {
                                if (info.playerTeam == -1)
                                {
                                    info.playerTeam = info.playerTeam + ids.Count;
                                }
                                else
                                {
                                    info.playerTeam = info.playerTeam - 1;
                                }
                            }
                            info.textTeam.Text = info.playerTeam == -1 ? "No team" : "Team " + info.playerTeam;
                        }
                        break;
                    default:
                        break;
                }
            }
        }
        /**
         * <summary>
         * Removes a color from the chosen list, so other players can use it
         * </summary>
         */
        public void RemoveColor(int playerColor)
        {
            chosenColors.Remove(playerColor);
        }
        /**
         * <summary>
         * Finds a new available color for the player
         * </summary>
         */
        public int FindNextColor(int playerColor)
        {
            int res = playerColor;
            int length = ClientDispatcher.colors.Length;

            if (playerColor < 0) {
                //First color for new player
                for(int i = 0; i < length; i++)
                {
                    if (!chosenColors.Contains(i))
                    {
                        res = i;
                        break;
                    }
                }
            }
            else
            {
                //New color for already existent player
                for (int i = playerColor; i < playerColor + length; i++)
                {
                    int mod = i % length;
                    if (!chosenColors.Contains(mod))
                    {
                        res = mod;
                        break;
                    }
                }
                chosenColors.Remove(playerColor);
            }
            chosenColors.Add(res);
            return res;

        }

        /**
         * <summary>
         * We get the identifier in the server.
         * Create the player
         * </summary>
         */
        private void HostNewPlayer(object sender, IncomingMessage receivedMessage)
        {
            if (ids.Count == 0)
            {
                maxPlayersLabel = new TextBlock("maxPlayersLabel") {HorizontalAlignment=HorizontalAlignment.Right, Text="Max Players", Margin = new Thickness(0, 70, 200, 0) };
                maxPlayersSelector = new Slider("maxPlayers") { Value = 2, Minimum = 2, Maximum = 4, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 70, 80, 0) };
                maxPlayersSelector.ValueChanged += ChangeMaxPlayers;
                maxPlayers = 2;
                networkedScene.EntityManager.Add(maxPlayersLabel);
                networkedScene.EntityManager.Add(maxPlayersSelector);

                fieldLabel = new TextBlock("fieldLabel") { Text = "Map size", HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 200, 0) };
                fieldSelector = new ToggleSwitch2("fieldSelector") { IsOn = false, OnText = "Big", OffText = "Small", HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 100, 0) };
                networkedScene.EntityManager.Add(fieldLabel);
                networkedScene.EntityManager.Add(fieldSelector);
            }
            string playerIdentifier = receivedMessage.ReadString();
            string playerName= receivedMessage.ReadString();
            if (ids.Count < maxPlayers)
            {
                networkedScene.state.Text = "player identified";

                ids.Add(playerIdentifier);
                names.Add(playerName);
                for (int i = 0; i < ids.Count; i++)
                    NotifyNewPlayerToClients(ids[i], names[i]);
            }
            else
            {
                //reached the limit, kick
                Kick(playerIdentifier);
            }
        }

        private void ChangeMaxPlayers(object sender, ChangedEventArgs e)
        {
            if (ids.Count <= maxPlayersSelector.Value)
            {
                //if we can reduce the number of players, we will
                maxPlayers = maxPlayersSelector.Value;
            }
            else
            {
                //otherwise, we cannot touch that value
                maxPlayersSelector.Value = maxPlayers;
            }
        }

        private void SendRemove(object sender, IncomingMessage receivedMessage)
        {
            var playerIdentifier = receivedMessage.ReadString();
            if (ids.Contains(playerIdentifier))
            {
                names.RemoveAt(ids.IndexOf(playerIdentifier));
                ids.Remove(playerIdentifier);
                SendRemove(playerIdentifier);
            }
        }
        private void NotifyNewPlayerToClients(string playerIdentifier,string playerName)
        {
            var responseMessage = networkService.CreateServerMessage();
            responseMessage.Write(NEW_PLAYER);
            responseMessage.Write(playerIdentifier);
            responseMessage.Write(playerName);
            networkService.SendToClients(responseMessage, DeliveryMethod.ReliableOrdered);
        }
        /**
         * <summary>
         * Starts the match if the player cap has been reached and returns true if successful
         * </summary>
         * 
         */
        public bool StartMatch()
        {
            bool res = EnoughPlayers() && DifferentTeams()&&AllReady();
            if (res)
            {
                //Clear the player selector so it cannot be changed
                networkedScene.EntityManager.Remove(maxPlayersSelector);
                networkedScene.EntityManager.Remove(maxPlayersLabel);

                for (int i = 0; i < maxPlayers; i++)
                {
                    
                    //Ask clients to create their players
                    var responseMessage = networkService.CreateServerMessage();
                    responseMessage.Write(ADD_PLAYER);
                    responseMessage.Write(maxPlayers);
                    responseMessage.Write(ids[i]);
                    networkService.SendToClients(responseMessage, DeliveryMethod.ReliableOrdered);
                }
            }
            return res;
        }
        /**
        * <summary>
        * Returns true there is de number of players required
        * </summary>
        */
        private bool EnoughPlayers()
        {
            bool res= ids.Count == maxPlayers;
            if (!res)
            {
                networkedScene.state.Text=string.Format("Not enough players! Must be {0}",maxPlayers);
            }
            return res;
        }

        /**
        * <summary>
        * Returns true if all players are ready
        * </summary>
        */
        private bool AllReady()
        {
            bool res = true;
            foreach (string id in ids)
            {
                Entity ent = networkedScene.EntityManager.Find("playerInfo" + id);
                PlayerInfo info = ent.FindComponent<PlayerInfo>();
                if (!info.ready)
                {
                    res = false;
                    networkedScene.state.Text = "Not all players ready!";
                    break;
                }
            }
            return res;
        }

        /**
* <summary>
* This returns true if there is at least two teams or one player without team.
* Also, if they are all ready, even if we have not reached the player limit
* </summary>
*/
        private bool DifferentTeams()
        {
            
            HashSet<int> teams = new HashSet<int>();

            foreach (string id in ids)
            {
                Entity ent = networkedScene.EntityManager.Find("playerInfo" + id);
                PlayerInfo info = ent.FindComponent<PlayerInfo>();
                teams.Add(info.playerTeam);
                if (info.playerTeam == -1 || teams.Count > 1)
                {
                    return true;
                }

            }
           networkedScene.state.Text= "They're all of the same team!";
            return false;
        }

        public void SendRemove(string playerIdentifier)
        {
            var responseMessage = networkService.CreateServerMessage();
            responseMessage.Write(REMOVE_PLAYER);
            responseMessage.Write(playerIdentifier);
            networkService.SendToClients(responseMessage, DeliveryMethod.ReliableOrdered);
        }
        public void Kick(string playerIdentifier)
        {
            var responseMessage = networkService.CreateServerMessage();
            responseMessage.Write(KICK);
            responseMessage.Write(playerIdentifier);
            networkService.SendToClients(responseMessage, DeliveryMethod.ReliableOrdered);
        }

        private void RerouteWOData(object sender, IncomingMessage receivedMessage)
        {
            SendData data = new SendData();
            data.clientId = receivedMessage.ReadString();
            data.creating = receivedMessage.ReadString(); ;
            data.position = new Vector2(receivedMessage.ReadSingle(), receivedMessage.ReadSingle());
            CreateAndSync(data);
        }

        public void CreateAndSync(SendData data)
        {
            networkedScene.sentMessages++;
            if (data.creating != "Castle")
            {
                Entity ent = UIBehavior.ui.Create(data);
                if (ent != null && ent.Scene != null)
                {
                    OutgoingMessage mes = networkService.CreateServerMessage();
                    mes.Write(ADD_WO);
                    mes.Write(data.clientId);
                    mes.Write(data.creating);

                    mes.Write(data.position.X);
                    mes.Write(data.position.Y);
                    mes.Write(ent.Name);
                    networkService.SendToClients(mes, WaveEngine.Networking.Messages.DeliveryMethod.ReliableOrdered);
                }
            }
            else
            {
                LayerTile tile = Map.map.GetTileByWorldPosition(data.position);
                if (tile != null)
                {
                    Castle castle = new Castle();
                    Entity entity = new Entity()
                        .AddComponent(new Transform2D())
                        .AddComponent(castle);
                    Player p = UIBehavior.ui.FindPlayer(data.clientId);
                    bool createdCastle = castle.SetCastle(tile.X, tile.Y, 
                        PopulateNetworkedGame.castleWidth, PopulateNetworkedGame.castleHeight, 
                        p, data.clientId);
                    if (createdCastle)
                    {
                        entity.Name = ("WO-Castle" + data.clientId + "-" + new Random().NextDouble() + "-" + data.creating);
                        networkedScene.EntityManager.Add(entity);
                        p.castle = castle;
                        castle.player = p;

                        OutgoingMessage mes = networkService.CreateServerMessage();
                        mes.Write(ADD_CASTLE);
                        mes.Write(data.clientId);
                        mes.Write(entity.Name);

                        mes.Write(castle.GetSize());
                        for (int i = 0; i < castle.GetSize(); i++)
                        {
                            WorldObject part = castle.GetPart(i);
                            networkedScene.EntityManager.Add(part.Owner);
                            mes.Write(part.Owner.Name);
                            mes.Write(part.GetX());
                            mes.Write(part.GetY());
                        }
                        networkService.SendToClients(mes, WaveEngine.Networking.Messages.DeliveryMethod.ReliableOrdered);
                    
                    }
                }

            }
        }

        /**
        * <summary>Force all players to update the UI when all ready</summary>
        */
        private void ForceUI(object sender, IncomingMessage receivedMessage)
        {
            if (playerUI < maxPlayers)
            {
                playerUI++;
            }
            if (playerUI == maxPlayers)
            {
                var responseMessage = networkService.CreateServerMessage();
                responseMessage.Write(UI);
                responseMessage.Write(fieldSelector.IsOn);
                networkedScene.EntityManager.Remove(fieldLabel);
                networkedScene.EntityManager.Remove(fieldSelector);
                networkService.SendToClients(responseMessage, DeliveryMethod.ReliableOrdered);
            }
        }

        private void RerouteDestroy(object sender, IncomingMessage receivedMessage)
        {
            string nameWO = receivedMessage.ReadString();
            Entity ent = networkedScene.EntityManager.Find(nameWO);
            if (ent != null)
            {
                ent.FindComponent<WorldObject>().SDestroy();
                ent.FindComponent<WorldObject>().CDestroy();

            }
        }
        private void Move(object sender, IncomingMessage receivedMessage)
        {
            Entity owner = networkedScene.EntityManager.Find(receivedMessage.ReadString());
            if (owner != null && !owner.IsDisposed)
            {
                bool recalculate = receivedMessage.ReadBoolean();
                int pathLength = receivedMessage.ReadInt32();
                Trace.WriteLine("moving tiles:" + pathLength);
                MovementBehavior movement = owner.FindComponent<MovementBehavior>();

                if (movement != null && pathLength > 1)
                {
                    List<LayerTile> path = new List<LayerTile>(pathLength);
                    bool validPath = true;
                    Map map = Map.map;
                    //First tile
                    path.Add(map.GetTileByMapCoordinates(receivedMessage.ReadInt32(), receivedMessage.ReadInt32()));
                    for (int i = 1; i < pathLength; i++)
                    {
                        LayerTile tile = map.GetTileByMapCoordinates(receivedMessage.ReadInt32(), receivedMessage.ReadInt32());

                        if (tile != null && !path.Contains(tile) && map.Adjacent(tile, path[i - 1])) //Cuando haya restricciones para pasar por una casilla se deben añadir también aquí
                        {
                            path.Add(tile);
                        }
                        else
                        {
                            validPath = false;
                            break;
                        }
                    }

                    if (validPath)
                    {
                        NewPath(owner, movement, path,recalculate);

                    }
                }
            }
        }

        public void NewPath(Entity owner, MovementBehavior movement, List<LayerTile> path,bool recalculate)
        {
            if (!recalculate)
            {
                movement.SetPath(path);

                //We synchro the path for other to visualize it
                var message = networkService.CreateServerMessage();

                message.Write(MOVE);

                message.Write(owner.Name);
                //if the path was accepted, we will send it
                path = movement.path;
                message.Write(path.Count);

                foreach (LayerTile tile in path)
                {
                    message.Write(tile.X);
                    message.Write(tile.Y);
                    Trace.WriteLine("X: " + tile.X + ", Y:" + tile.Y);
                }
                networkService.SendToClients(message, DeliveryMethod.ReliableOrdered);
            }
            else
            {
                movement.ForcePath(path);
            }
        }

        private void Stop(object sender, IncomingMessage receivedMessage)
        {
            Entity owner = networkedScene.EntityManager.Find(receivedMessage.ReadString());
            if (owner != null && !owner.IsDisposed)
            {
                
                owner.FindComponent<WorldObject>().Stop();
            }

        }
private void ChangeAttacking(object sender, IncomingMessage receivedMessage)
        {
            Entity owner = networkedScene.EntityManager.Find(receivedMessage.ReadString());
            if (owner != null && !owner.IsDisposed)
            {

                owner.FindComponent<WorldObject>().AttackToggle();
            }
        }        private void ExitCottage(object sender, IncomingMessage receivedMessage)
        {
            Entity cottage = networkedScene.EntityManager.Find(receivedMessage.ReadString());
            if (cottage != null && !cottage.IsDisposed)
            {
                Cottage cot = cottage.FindComponent<Cottage>();
                int number = receivedMessage.ReadInt32();
                if (cot != null && !cot.UnderConstruction())
                {
                    LayerTile aux = Map.map.GetTileByMapCoordinates(receivedMessage.ReadInt32(),receivedMessage.ReadInt32());
                    cot.ExitPerson(number, aux);
                }

            }
        }
        private void Select(object sender, IncomingMessage receivedMessage)
        {
            Player player = UIBehavior.ui.FindPlayerByName(receivedMessage.ReadString());
            // obtener el networkId del objeto para pasarlo
            if (player != null && !player.isLocalPlayer)
            {
                string woName = receivedMessage.ReadString();
                if (string.IsNullOrEmpty(woName))
                {
                    player.selectedWO = null;
                }
                else
                {
                    Entity ent = networkedScene.EntityManager.Find(woName);
                    if (ent != null && !ent.IsDisposed)
                    {
                        player.selectedWO = ent.FindComponent<WorldObject>();
                    }
                }
            }

        }
        private void ExecuteAction(object sender, IncomingMessage receivedMessage)
        {
            Entity owner = networkedScene.EntityManager.Find(receivedMessage.ReadString());
            Entity other = networkedScene.EntityManager.Find(receivedMessage.ReadString());
            if (owner != null && !owner.IsDisposed && other != null && !other.IsDisposed)
            {
                WorldObject wo=owner.FindComponent<WorldObject>();
                CommandEnum cmd = (CommandEnum)receivedMessage.ReadInt32();
                foreach(ActionBehavior act in wo.allActions)
                {
                    if (act.GetCommand() == cmd)
                    {
                        bool store = receivedMessage.ReadBoolean();
                        if (store)
                        {
                            wo.EnqueueAction(() => act.Act(other.FindComponent<WorldObject>()));
                        }else
                        {
                            act.Act(other.FindComponent<WorldObject>());
                        }
                        break;
                    }
                }
               

            }
        }
    }
}
