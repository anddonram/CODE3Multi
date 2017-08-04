using System;
using System.Collections.Generic;
using System.Text;
using WaveEngine.Common.Graphics;
using WaveEngine.Components.UI;
using WaveEngine.Framework;
using WaveEngine.Framework.Managers;
using WaveEngine.Framework.Services;
using WaveEngine.Framework.UI;
using WaveEngine.Networking;
using WaveEngine.Networking.Messages;

namespace Codigo.GUI
{
    public class PlayerInfo:Component
    {
        public string playerIdentifier { get; private set; } = "";
        public string playerName { get; private set; } = "player";
        public int playerColor = 0;
        public int playerTeam  = -1;

        public bool ready = false;

        private TextBlock pName;
        public TextBlock textTeam;
        public Button previousTeam,nextTeam,nextColor;
        private Button kickButton;
        public Button toggleReady;

        private ServerDispatcher serverDispatcher;
        private NetworkService networkService;

        /**
         * <summary>
         * A distance to separate player GUI info
         * </summary>
         */ 
        private const float distanceBetweenPlayers=80;

        /**
         * <summary>
         * offset for the first info line: color and team
         * </summary>
         */
        private const float firstGUILine = 100;
        /**
         * <summary>
         * offset for the second info line: buttons
         * </summary>
         */
        private const float secondGUILine = 130;
        public const int COLOR = 0, TEAM = 1, READY = 2;
        /**
         * <summary>
         * Adds all the UI regarding this player (including a kick button for the host)
         * </summary>
         */
        public void Set(string playerID, string playerName, bool isLocalPlayer,int number,NetworkedScene networkedScene) {
            serverDispatcher = networkedScene.serverDispatcher;
            playerIdentifier = playerID;
            this.playerName = playerName;
            this.networkService = networkedScene.networkService;
            pName = new TextBlock { Text = playerName,  Margin = new Thickness(0, firstGUILine + (distanceBetweenPlayers * number), 0, 0) };
            networkedScene.EntityManager.Add(pName);


            nextColor = new Button { IsBorder = isLocalPlayer, Text = "Change", Width = 100, Margin = new Thickness(300, firstGUILine + (distanceBetweenPlayers * number), 10, 10) };
            networkedScene.EntityManager.Add(nextColor);

            toggleReady = new CustomButton() { IsBorder = isLocalPlayer, Text = "Not ready", Margin = new Thickness(450, firstGUILine + (distanceBetweenPlayers * number), 10, 10) };
            networkedScene.EntityManager.Add(toggleReady);

            if (isLocalPlayer)
            {
                toggleReady.Click += ChangeReady;
                previousTeam = new Button { Text = "<", Width = 30,Height=30, Margin = new Thickness(110, firstGUILine + (distanceBetweenPlayers * number), 0, 0) };
                nextTeam = new Button { Text = ">", Width = 30,Height=30, Margin = new Thickness(230, firstGUILine + (distanceBetweenPlayers * number), 0, 0) };

                previousTeam.Click += PreviousTeam;
                nextTeam.Click += NextTeam;

                networkedScene.EntityManager.Add(previousTeam);
                networkedScene.EntityManager.Add(nextTeam);

                nextColor.Click += NextColor;
                
            }
            textTeam = new TextBlock { Text = "No team", Margin = new Thickness(150, firstGUILine + (distanceBetweenPlayers * number), 10, 10) };
            networkedScene.EntityManager.Add(textTeam);


            if (networkedScene.isHost)
            {
                playerColor =networkedScene.serverDispatcher.FindNextColor(-1);
                nextColor.Text = ClientDispatcher.colorNames[playerColor];
                nextColor.Foreground = ClientDispatcher.colors[playerColor];
                nextColor.BorderColor= ClientDispatcher.colors[playerColor];

                kickButton = new Button {IsVisible=!isLocalPlayer, Text="Kick",Margin = new Thickness(600, firstGUILine + (distanceBetweenPlayers * number), 10, 10) };
                kickButton.Click +=Kick;
                networkedScene.EntityManager.Add(kickButton);
               
            }
            WaveServices.Layout.PerformLayout();
        }

        private void ChangeReady(object sender, EventArgs e)
        {
            Change(READY, true);
        }

        private void NextColor(object sender, EventArgs e)
        {
            Change(COLOR, true);
        }


        private void NextTeam(object sender, EventArgs e)
        {
            Change(TEAM, true);
        }

        private void PreviousTeam(object sender, EventArgs e)
        {
            Change(TEAM, false);
        }

        private void Change(int type, bool next)
        {
            var message = networkService.CreateClientMessage();
            message.Write(NetworkedScene.CHANGE_PLAYER);
            message.Write(networkService.ClientIdentifier);
            message.Write(type);
            message.Write(next);
            networkService.SendToServer(message, DeliveryMethod.ReliableOrdered);
        } 

        /**
        * <summary>
        * Request the host to remove the UI regarding this player for all clients and kick this player
        * </summary>
        */
        private void Kick(object sender, EventArgs e)
        {
            serverDispatcher.SendRemove(playerIdentifier);
            serverDispatcher.Kick(playerIdentifier);
        }
        /**
         * <summary>
         * Remove the UI from the scene
         * </summary>
         */
        public void Remove(EntityManager manager)
        {
            manager.Remove(pName);
            manager.Remove(textTeam);
            manager.Remove(toggleReady);
           
            if (previousTeam != null)
            {
                manager.Remove(previousTeam);
            }
            if (nextTeam != null)
            {
                manager.Remove(nextTeam);
            }
            if (nextColor != null)
            {
                manager.Remove(nextColor);
            }
            if (kickButton != null)
            {
                manager.Remove(kickButton);
            }

          
        }

        public void ChangeReady()
        {
            toggleReady.Text = ready ? "Ready" : "Not ready";
        }

        /**
         * <summary>
         * We move the UI 1 step up
         * </summary>
         */
        public void MoveUp()
        {
            Thickness margin = pName.Margin;
            margin.Top -= distanceBetweenPlayers;
            pName.Margin = margin;

            margin = textTeam.Margin;
            margin.Top -= distanceBetweenPlayers;
            textTeam.Margin = margin;

            margin = toggleReady.Margin;
            margin.Top -= distanceBetweenPlayers;
            toggleReady.Margin = margin;


            if (previousTeam != null)
            {
                margin = previousTeam.Margin;
                margin.Top -= distanceBetweenPlayers;
                previousTeam.Margin = margin;
            }
            if (nextTeam != null)
            {
                margin = nextTeam.Margin;
                margin.Top -= distanceBetweenPlayers;
                nextTeam.Margin = margin;
            }
            if (nextColor != null)
            {
                margin = nextColor.Margin;
                margin.Top -= distanceBetweenPlayers;
                nextColor.Margin = margin;
            }
            if (kickButton != null)
            {
                margin = kickButton.Margin;
                margin.Top -= distanceBetweenPlayers;
                kickButton.Margin = margin;
            }
            
        }
    }
}
