using Codigo.Behaviors;
using Codigo.Components;
using Codigo.GUI;
using Codigo.Sound;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using WaveEngine.Common.Graphics;
using WaveEngine.Common.Math;
using WaveEngine.Components.UI;
using WaveEngine.Framework;
using WaveEngine.Framework.Graphics;
using WaveEngine.Framework.Services;
using WaveEngine.Framework.UI;
using WaveEngine.Networking;
using WaveEngine.Networking.Messages;
using WaveEngine.TiledMap;
using static Codigo.NetworkedScene;
namespace Codigo
{
    public class ClientDispatcher
    {
        
        private NetworkedScene networkedScene;
        private NetworkService networkService;
        private int playerReady = 0;

        public static readonly Color[] colors = { Color.Red, Color.Blue, Color.Green, Color.Yellow };
        public static readonly string[] colorNames = { "Red", "Blue", "Green", "Yellow" };
        
        private Player[] players;

        /**
         * <summary>
         * Headers for the GUI player list
         * </summary>
         */
        private TextBlock pName, team, color;

        /**
         * <summary>
         * Each player info handles a row of data
         * </summary>
         */
        private List<PlayerInfo> playerInfo;

        public ClientDispatcher(NetworkedScene n)
        {
            this.networkedScene = n;
            this.networkService = networkedScene.networkService;
            
            playerInfo = new List<PlayerInfo>();
            
        }
        public void ClientMessageDispatcher(object sender, IncomingMessage receivedMessage)
        {
            if (networkedScene.IsDisposed)
                return;
            int messageType = receivedMessage.ReadInt32();
            switch (messageType)
            {
                case KICK:
                    KickClient(sender, receivedMessage);
                    break;
                case ADD_PLAYER:
                    ClientNewPlayer(sender, receivedMessage);
                    break;
                case NEW_PLAYER:
                    NewPlayerInfo(sender, receivedMessage);
                    break;
                case REMOVE_PLAYER:
                    RemovePlayerInfo(sender, receivedMessage);
                    break;
                case UI:
                    SetAllElementsAndStart(receivedMessage.ReadBoolean());
                    break;
                case ADD_WO:
                    if (!networkedScene.isHost)
                        ReceiveWOMessage(sender, receivedMessage);
                    break;
                case ADD_CASTLE:
                    if (!networkedScene.isHost)
                        ReceiveCastleMessage(sender, receivedMessage);
                    break;
                case DESTROY_WO:
                    if (!networkedScene.isHost)
                        DestroyWO(sender, receivedMessage);
                    break;
                case SYNC:
                    if (!networkedScene.isHost)
                        Synchro(sender, receivedMessage);
                    break;
                case MOVE:
                    if (!networkedScene.isHost)
                        SynchroPath(sender,receivedMessage);
                    break;
                case FINISH:
                    UIBehavior.ui.Owner.FindComponent<VictoryBehavior>().Finish(receivedMessage.ReadString(),receivedMessage.ReadString());
                    break;
                default:
                    Trace.WriteLine("Mensaje erróneo recibido desde el servidor");
                    break;
            }
        }

        private void RemovePlayerInfo(object sender, IncomingMessage receivedMessage)
        {
            var playerIdentifier = receivedMessage.ReadString();
            var pos = -1;
            for (int i = 0; i < playerInfo.Count; i++)
            {
                if (playerInfo[i].playerIdentifier == playerIdentifier)
                {
                    if (networkedScene.isHost)
                    {
                        networkedScene.serverDispatcher.RemoveColor(playerInfo[i].playerColor);
                    }
                    playerInfo[i].Remove(networkedScene.EntityManager);
                    playerInfo.RemoveAt(i);
                    pos = i;
                    break;
                }
            }
            if (pos >= 0)
            {
                for (int i = pos; i < playerInfo.Count; i++)
                {
                    playerInfo[i].MoveUp();
                }
                WaveServices.Layout.PerformLayout();
            }
        }

        private void NewPlayerInfo(object sender, IncomingMessage receivedMessage)
        {
            if (playerInfo.Count == 0)
            {
                AddHeader();
            }
            string playerIdentifier = receivedMessage.ReadString();
            string playerName = receivedMessage.ReadString();
            bool playerAlreadyExists = false;
            for (int i = 0; i < playerInfo.Count; i++)
            {
                if (playerInfo[i].playerIdentifier == playerIdentifier)
                {
                    playerAlreadyExists = true;
                    //break;
                }else if (networkedScene.isHost)
                {
                    //We must resend the all the info as the new player does not have any of that
                    PlayerInfoSync infoSync = playerInfo[i].Owner.FindComponent<PlayerInfoSync>();
                    OutgoingMessage mes = networkedScene.networkService.CreateServerMessage();
                    mes.Write(NetworkedScene.SYNC);
                    mes.Write(infoSync.Owner.Name);
                    mes.Write(infoSync.GetType().ToString());
                    infoSync.WriteSyncData(mes);
                    networkedScene.networkService.SendToClients(mes, DeliveryMethod.ReliableOrdered);
                }
            }
            if (!playerAlreadyExists)
            {

                Entity p = new Entity("playerInfo" + playerIdentifier);
                PlayerInfo info = new PlayerInfo();
                info.Set(playerIdentifier, playerName,
                    networkedScene.networkService.ClientIdentifier==playerIdentifier, 
                    playerInfo.Count,
                    networkedScene);

                p.AddComponent(info)
                    .AddComponent(new PlayerInfoSync())
                    .AddComponent(new SyncBehavior());
                playerInfo.Add(info);
                networkedScene.EntityManager.Add(p);
            }
        }

        private void AddHeader()
        {
            pName = new TextBlock { Text = "Name", Margin = new Thickness(0, 30, 10, 10) };
            networkedScene.EntityManager.Add(pName);

            team = new TextBlock { Text="Team", Margin = new Thickness(150, 30, 10, 10) };
            networkedScene.EntityManager.Add(team);

            color = new TextBlock { Text="Color", Margin = new Thickness(300, 30, 10, 10) };
            networkedScene.EntityManager.Add(color);
        }
 
        /// <summary>
        /// Kicks the player as the room is full
        /// </summary>
        private void KickClient(object sender, IncomingMessage receivedMessage)
        {

            var playerIdentifier = receivedMessage.ReadString();

            if (networkService.ClientIdentifier == playerIdentifier)
            {
                //negative response, exit
                //remove all info from existence
                for (int i = 0; i < playerInfo.Count; i++)
                {
                    playerInfo[i].Remove(networkedScene.EntityManager);
                    playerInfo.RemoveAt(i);
                }
                networkedScene.  state.Text = "exiting...";
                WaveServices.TimerFactory.CreateTimer(TimeSpan.FromSeconds(1), () =>
                {
                    networkedScene.Cancel();

                }, false, networkedScene);
            }

        }

        private void ClientNewPlayer(object sender, IncomingMessage receivedMessage)
        {
            int maxPlayers = receivedMessage.ReadInt32();
            if (players == null)
            {
                players = new Player[maxPlayers];
            }
            var playerIdentifier = receivedMessage.ReadString();
            if (networkedScene.EntityManager.Find("player" + playerIdentifier) == null)
            {
                Entity p = new Entity("player" + playerIdentifier);
                p.Tag = "player";
                Player player1 = new Player();

                p.AddComponent(player1)
                                    .AddComponent(new PlayerSync())
                                    .AddComponent(new PlayerDataSync())
                                    .AddComponent(new SyncBehavior());
                PlayerInfo info = null;
                foreach (PlayerInfo infos in playerInfo)
                {
                    if (playerIdentifier == infos.playerIdentifier)
                    {
                        info = infos;
                        break;
                    }
                }
                if (networkedScene.isHost)
                {
                    if (info != null)
                    {
                        player1.playerColor = colors[info.playerColor];
                        player1.playerName = info.playerName;
                        player1.playerTeam = info.playerTeam;
                    }
                }
                info.Remove(networkedScene.EntityManager);
                playerInfo.Remove(info);
                networkedScene.EntityManager.Remove(info.Owner);
                
                players[playerReady++] = player1;

                player1.isLocalPlayer = networkService.ClientIdentifier == playerIdentifier;

                networkedScene.EntityManager.Add(p);

                if (maxPlayers == playerReady)
                {
                    RemoveHeader();
                    networkedScene.state.Text = "we are ready: waiting for server...";
                    CallServerUI();
                }
            }
        }
        private void RemoveHeader()
        {
            networkedScene.EntityManager.Remove(pName);
            networkedScene.EntityManager.Remove(team);
            networkedScene.EntityManager.Remove(color);
        }
        private void SetAllElementsAndStart(bool bigField)
        {
            networkedScene.state.Text = "setting up GUI";
            if (bigField)
            {
                Map.map.SetBounds(20, 20);
            }else
            {
                Map.map.SetBounds(10, 12);
            }
            Map.map.SetMap();
            FogOfWar.fog.InitializeFog( true);

            UIBehavior ui = UIBehavior.ui;
            ui.SetPlayers(players);
            ui.SetUI();
            ui.Owner.FindComponent<Camera2DBehavior>().SetDefaultValues();
            for (int i = 0; i < players.Length; i++)
                if (players[i].isLocalPlayer)
                {
                    ui.playerButtons[i].IsChecked = true;
                    new PopulateNetworkedGame().SetCameraPosition(i, players.Length, networkedScene);
                    break;
                }



            if (networkedScene.isHost && POPULATE)
            {
                PopulateNetworkedGame pop = new PopulateNetworkedGame();
                pop.SetRiver(networkedScene.serverDispatcher);
                pop.SetWoods(networkedScene.serverDispatcher);
                for (int i = 0; i < players.Length; i++)
                {
                    pop.SetPlayer(networkedScene.serverDispatcher, players[i], i, players.Length);
                    if (players[i].isLocalPlayer)
                    {
                        pop.SetCameraPosition(i, players.Length, networkedScene);
                    }
                }
                UIBehavior.ui.Owner.FindComponent<VictoryBehavior>().Enable();
            }
            
            networkedScene.RemoveLobbyUI();
            Trace.WriteLine("Messages: "+networkedScene.sentMessages);
            WaveServices.Layout.PerformLayout();
            ui.Owner.IsActive = true;
            Map.map.Owner.IsActive = true;
            Map.map.Owner.IsVisible = true;

            //this class shows the costs for computing d* path on a selected WO
            //DStarDrawable dstarBeh = new DStarDrawable();
            //networkedScene.AddSceneBehavior(dstarBeh, SceneBehavior.Order.PostUpdate);
            //dstarBeh.Set();

            //this is for clearing the new changing visibility tiles,  D* purposes
            networkedScene.AddSceneBehavior(new FogClearBehavior(), SceneBehavior.Order.PostUpdate);


        }

        private void ReceiveWOMessage(object sender, IncomingMessage receivedMessage)
        {
            SendData data = new SendData();
            data.clientId = receivedMessage.ReadString();
            data.creating = receivedMessage.ReadString(); ;
            data.position = new Vector2(receivedMessage.ReadSingle(), receivedMessage.ReadSingle());
            data.realName = receivedMessage.ReadString();
            Trace.WriteLine("recibido!!: " + data.creating);

            Entity ent = networkedScene.EntityManager.Find(data.realName);
            if (ent == null)
            {
                ent = UIBehavior.ui.CreateForClient(data);
            }

        }
        private void ReceiveCastleMessage(object sender, IncomingMessage receivedMessage)
        {
            //reads playerid
            string playerID = receivedMessage.ReadString();
            Player p = UIBehavior.ui.FindPlayer(playerID);
            
            Castle castle = new Castle();
            Entity entity = new Entity()
                .AddComponent(new Transform2D())
                .AddComponent(castle);
            //castle name
            entity.Name = receivedMessage.ReadString();
            castle.SetCastleForClient(receivedMessage,p);
            p.castle = castle;
            castle.player = p;

            networkedScene.EntityManager.Add(entity);
            for (int i = 0; i < castle.GetSize(); i++)
            {
                WorldObject part = castle.GetPart(i);
                networkedScene.EntityManager.Add(part.Owner);
            }
                Trace.WriteLine("Castillito");
        }
        private void DestroyWO(object sender, IncomingMessage receivedMessage)
        {
            
                string nameWO = receivedMessage.ReadString();
                Entity ent = networkedScene.EntityManager.Find(nameWO);
                if (ent != null)
                {
                    ent.FindComponent<WorldObject>().CDestroy();
                }
            
        }
        private void Synchro(object sender, IncomingMessage receivedMessage)
        {

            string entName = receivedMessage.ReadString();
            string cName = receivedMessage.ReadString();
            Entity ent = networkedScene.EntityManager.Find(entName);
            if (ent != null&&!ent.IsDisposed)
            {
                NetworkSyncComponent[] comps = ent.FindComponents<NetworkSyncComponent>(false);

                for (int i = 0; i < comps.Length; i++)
                {
                    NetworkSyncComponent comp = comps[i];
                    Trace.WriteLine(comp.GetType().ToString());
                    if (comp.GetType().ToString() == cName)
                    {
                        comp.ReadSyncData(receivedMessage);
                        break;
                    }
                }


            }
        }

        /*
       * send a message to the server so that they now we are in
       */
        private void CallServerReady()
        {
            networkedScene.state.Text = "notifying server...";
            var message = networkService.CreateClientMessage();
            message.Write(START);
            message.Write(networkService.ClientIdentifier);

            networkService.SendToServer(message, DeliveryMethod.ReliableOrdered);
        }


        private void CallServerUI()
        {
            networkedScene.state.Text = "notifying server ui...";
            var message = networkService.CreateClientMessage();
            message.Write(UI);
            message.Write(networkService.ClientIdentifier);

            networkService.SendToServer(message, DeliveryMethod.ReliableOrdered);

        }

        private void SynchroPath(object sender, IncomingMessage receivedMessage)
        {
            Entity owner = networkedScene.EntityManager.Find(receivedMessage.ReadString());
            if (owner != null && !owner.IsDisposed)
            {
                int pathLength = receivedMessage.ReadInt32();
                MovementBehavior movement = owner.FindComponent<MovementBehavior>();

                if (movement != null)
                {
                    List<LayerTile> path = new List<LayerTile>(pathLength);

                    Map map = Map.map;
                    for (int i = 0; i < pathLength; i++)
                    {
                        LayerTile tile = map.GetTileByMapCoordinates(receivedMessage.ReadInt32(), receivedMessage.ReadInt32());
                        if (tile != null)
                        {
                            path.Add(tile);
                        }
                    }
                    movement.SetPathForClient(path);
                }
            }
        }

    }
}
