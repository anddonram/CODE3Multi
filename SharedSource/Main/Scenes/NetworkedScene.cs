using Codigo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using WaveEngine.Common.Graphics;
using WaveEngine.Common.Math;
using WaveEngine.Components.Cameras;
using WaveEngine.Components.Graphics2D;
using WaveEngine.Components.UI;
using WaveEngine.Framework;
using WaveEngine.Framework.Graphics;
using WaveEngine.Framework.Managers;
using WaveEngine.Framework.Services;
using WaveEngine.Framework.UI;
using WaveEngine.Networking;
using WaveEngine.TiledMap;
using WaveEngine.Networking.Messages;
using Codigo.Behaviors;
using Codigo.Components;

using System.Net;
using System.Net.Sockets;
using Codigo.VictoryConditions;
using Codigo.GUI;
using Codigo.Scenes;
using Codigo.Sound;

namespace Codigo
{
   public class NetworkedScene:Scene
    {
        private const string ApplicationId = "MyGame";
        private const int Port = 1492;
        private const string SceneIdentifier = "NetworkedScene";

        public int sentMessages = 0;
        /**
         * <summary>
         * Message types for communication
         * </summary>
         */
        public const int ADD_PLAYER = 0, START = 1, ADD_WO = 2, KICK = 3, UI = 4, DESTROY_WO = 5, SYNC = 6,
            MOVE = 7, STOP = 8
           , ADD_CASTLE = 13,  EXIT_COTTAGE = 15, SELECT = 16
            ,NEW_PLAYER=19,REMOVE_PLAYER=20, CHANGE_PLAYER=21,CHANGE_SPEED=22,FINISH=23,WO_ACTION = 24, ATTACK = 25;
        /**
         * <summary>
         * Whether we want to populate the map with elements
         * </summary>
         */
        public const bool POPULATE = true;


        public NetworkService networkService;

        public TextBlock state { get; private set; }
        private CustomTextBox[] ipGetters;
        private CustomTextBox playerName;
        private Button clientButton;
        private Button hostButton;
        private Button readyButton;
        private Button cancelButton;
        /**
       * <summary>
       * Change the keys button
       * </summary>
       */
        Button keysButton;

        /**
         * <summary>
         * This will only be true if this instance of the game is a server
         * </summary>
         */
        public bool isHost { get; private set; }
        /**
         * <summary>
         * This will be true if this instance of the game is connected to a server
         * </summary>
         */
        public bool isConnected { get; private set; }

        
        public ServerDispatcher serverDispatcher { get; private set; }
        private ClientDispatcher clientDispatcher;

        public NetworkedScene()
        {
            WaveServices.SoundPlayer.StopAllSounds();

            networkService = WaveServices.GetService<NetworkService>();
            if (networkService==null) {
                WaveServices.RegisterService(new NetworkService());
                networkService = WaveServices.GetService<NetworkService>();
            }      
            
            serverDispatcher = new ServerDispatcher(this);
            clientDispatcher = new ClientDispatcher(this);

            networkService.HostMessageReceived += serverDispatcher.HostMessageDispatcher;
            networkService.ClientMessageReceived += clientDispatcher.ClientMessageDispatcher;
            networkService.HostDisconnected += Disconnect;
        }

        
        private void Disconnect(object sender, Host host)
        {
            Disconnect();

        }

        private void Disconnect()
        {
            networkService.Disconnect();
            WaveServices.SoundPlayer.StopAllSounds();
            //We create a new scene of the same class (myscene, playablescene, test, etc.) of the current scene
            WaveServices.UnregisterService<NetworkService>();
            WaveServices.RegisterService(new NetworkService());

            Scene newScene = Activator.CreateInstance(this.GetType()) as Scene;
            ScreenContext screenContext = new ScreenContext(newScene);
            WaveServices.ScreenContextManager.To(screenContext);

        }

        /*
* send a message to the server so that they create the player
*/
        private void CallServer()
        {
            state.Text = "notifying server...";
            var message = this.networkService.CreateClientMessage();
            message.Write(START);
            message.Write(this.networkService.ClientIdentifier);
            message.Write(playerName.Text);
            this.networkService.SendToServer(message, DeliveryMethod.ReliableOrdered);
        }
     
        protected override void CreateScene()
        {
            this.Load(WaveContent.Scenes.MyScene);
            Viewport v = WaveServices.GraphicsDevice.RenderState.Viewport;
            //Pone el formato de la pantalla virtual (no de la ventana, eso va en app.cs)
            this.VirtualScreenManager.Activate(v.Width, v.Height, StretchMode.Uniform);

            //la camara
            UIBehavior.ui = new UIBehavior();
            UIBehavior.ui.networkService = networkService;
            Entity camera = new Entity("camera2D").AddComponent(new Transform2D { Scale = new Vector2(0.28f, 0.28f) })
                .AddComponent(new Camera2D { BackgroundColor=Color.Brown})
                .AddComponent(new Camera2DBehavior())
                .AddComponent(new KeysBehavior())
                .AddComponent(UIBehavior.ui)
                .AddComponent(new VictoryBehavior())
                .AddComponent(new NoPeopleCondition())
                .AddComponent(new CastleCondition())
                .AddComponent(new SoundHandler());
            
            EntityManager.Add(camera);
            RenderManager.SetActiveCamera2D(camera);
          
            // el mapa
            Map.map = new Map();
            FogOfWar.fog = new FogOfWar();
            Entity map = new Entity("map") 
                .AddComponent(new Transform2D())//{ DrawOrder=1})//the map is rendered at the bottom
                .AddComponent(new TiledMap(POPULATE? WaveContent.Assets.Tiles.Map_12_20_tmx : WaveContent.Assets.Tiles.Map_tmx))
                .AddComponent(Map.map)
                .AddComponent(new MapSync())
                .AddComponent(new MapStatusRecover())
                .AddComponent(new SyncBehavior())
                .AddComponent(FogOfWar.fog)
                .AddComponent(new SpeedSync());
            EntityManager.Add(map);
            camera.IsActive = false;
            map.IsActive = false;

            state = new TextBlock("Identifier") { Text = "State", Margin = new Thickness(0, 0, 0, 0) };
            playerName = new CustomTextBox("playerName") {Width=150, Text = "playerX", Margin = new Thickness(0, 100, 0, 0) };
            ipGetters = new CustomTextBox[4];
            for (int i = 0; i < ipGetters.Length; i++)
            {
                ipGetters[i] = new CustomTextBox("ipGetter"+i) {IsNumeric=true,MaxLength=3,Width=50, Text = "255", Margin = new Thickness(70 * i, 200, 0, 0) };
                EntityManager.Add(ipGetters[i]);
            }
            
            clientButton = new Button("connectButton") { Text = "Connect", Margin = new Thickness(0, 300, 0, 0) };
            hostButton = new Button("hostButton") { Text = "Host", Margin = new Thickness(0, 400, 0, 0) };
            readyButton = new Button("readyButton") { Text = "Start", Margin = new Thickness(0, 400, 0, 0) };
            cancelButton = new Button("cancelButton") { Text = "Cancel", Margin = new Thickness(0, 500, 0, 0) };
            keysButton = new Button { VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Right, Foreground = Color.Black, Text = "Keys", Margin = new Thickness(0, 0, 0, 0) };

            SetButtons(clientButton, Connect);
            SetButtons(hostButton, CreateHost);
            SetButtons(readyButton, StartMatch);
            SetButtons(cancelButton, Cancel);
            SetButtons(keysButton, ChangeKeys);

            EntityManager.Add(state);
            
            EntityManager.Add(playerName);

            //hide the readyButton until we have reached the capacity
            readyButton.IsVisible = false;
            cancelButton.IsVisible = false;
          //  this.AddSceneBehavior(new FramesPerSecondBehavior(), SceneBehavior.Order.PostUpdate);
            
        }
        protected override void Start()
        {
            base.Start();
            Map.map.Owner.IsVisible = false;
        }
        /**
         * <summary>
         * Adds all stuff to GUI buttons
         * </summary>
         */
        private void SetButtons(Button button, EventHandler e)
        {
            EntityManager.Add(button);
            button.Click += e;
        }
        /**
         * <summary>
         * Moves to the key change scene
         * </summary>
         */
        private void ChangeKeys(object sender, EventArgs e)
        {
            if (!isConnected)
            {
                WaveServices.SoundPlayer.StopAllSounds();
                //We create a new scene of the same class (myscene, playablescene, test, etc.) of the current scene
                WaveServices.UnregisterService<NetworkService>();
                ScreenContext screenContext = new ScreenContext(new KeysScene(this.GetType()));
                WaveServices.ScreenContextManager.To(screenContext);
            }
        }
        
        private void StartMatch(object sender, EventArgs e)
        {
            if (isHost)
            {
                if (serverDispatcher.StartMatch())
                {
                    //We started successfully, disable readyButton
                    readyButton.IsVisible = false;
                }
            }
        }

        public void Cancel(object sender, EventArgs e)
        {
            Cancel();
        }
        public void Cancel() {
            if (!connectionInProcess)
            {
                if (isHost)
                {
                    networkService.ShutdownHost();
                }
                else if (isConnected)
                {
                    //We notify the server we will disconnect
                    var message = this.networkService.CreateClientMessage();
                    message.Write(REMOVE_PLAYER);
                    message.Write(this.networkService.ClientIdentifier);

                    this.networkService.SendToServer(message, DeliveryMethod.ReliableOrdered);
                }
                Disconnect();

            }else
            { 
                state.Text = "Cancelling...";
            }
        }
        private bool connectionInProcess = false;
        private async void CreateHost(object sender, EventArgs e)
        {
            if (!isHost)
            {
                EnableButtons(false);
                connectionInProcess = true;
                await InitializeNetworkConnection();
                if (isHost)
                {
                    state.Text = "Connection to host successful";
                    CallServer();
                    readyButton.IsVisible = true;
                    connectionInProcess = false;

                }
                else 
                {
                    state.Text = "There is already a local host";
                    connectionInProcess = false;
                    Cancel();
                }

            }
        }

        private async System.Threading.Tasks.Task InitializeNetworkConnection()
        {
            state.Text = "Looking for host";
            var discoveredHost = await this.WaitForDiscoverHostAsync(TimeSpan.FromSeconds(2));

            if (discoveredHost == null)
            {
                state.Text = "host not found. setting up host";
                try
                {
                    discoveredHost = InitializeHost();
                    await networkService.ConnectAsync(discoveredHost);
                    isHost = true;
                    isConnected = true;

                }
                catch (Exception e)
                {
                    state.Text = "there is already a local host";
                    Trace.WriteLine(e.Message);
                }
            }

        }
        private async void Connect(object sender, EventArgs e)
        {
            EnableButtons(false);
            connectionInProcess = true;
            await Connection();
            state.Text = "Connection to host successful";
           
            CallServer();
            connectionInProcess = false;
        }

        private void EnableButtons(bool enable)
        {
            foreach (CustomTextBox ip in ipGetters)
                ip.IsVisible = enable;
            playerName.IsVisible = enable;
            hostButton.IsVisible = enable;
            clientButton.IsVisible = enable;
            cancelButton.IsVisible = !enable;
            keysButton.IsVisible= enable;
        }
        private async System.Threading.Tasks.Task Connection()
        {
            state.Text = "Connecting as client";
            var discoveredHost = await this.WaitForDiscoverHostAsync(TimeSpan.FromSeconds(2));
            if (discoveredHost == null)
            {
                string[] ipPieces = new string[ipGetters.Length];
                for (int i = 0; i < ipPieces.Length; i++)
                {
                    ipPieces[i] = ipGetters[i].Text;
                }
                string s = string.Join(".",ipPieces);
                discoveredHost = new Host { Address = s, Port = Port };
                
            }
            state.Text = "Connecting to: "+ discoveredHost.Address;
            await this.networkService.ConnectAsync(discoveredHost);
            state.Text = "Connected";
            isConnected = true;
        }

    
        private Host InitializeHost()
        {
            var aux = Dns.GetHostEntry(Dns.GetHostName());
            String localIP=null;
            this.networkService.InitializeHost(ApplicationId, Port);
            foreach(var ip in aux.AddressList)
            {
                if(ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                }
            }
            
            if (localIP != null)
            {
                Trace.WriteLine("|" + localIP + "|");
                var host = new Host() { Address = localIP, Port = Port };
                return host;
            }
            else
            {
                var host = new Host() { Address = "127.0.0.1", Port = Port };
                return host;
            }
            
        }

        public void RemoveLobbyUI()
        {
            EntityManager.Remove(state);
            foreach (CustomTextBox ip in ipGetters)
                EntityManager.Remove(ip);
            EntityManager.Remove(playerName);
            EntityManager.Remove(clientButton);
            EntityManager.Remove(hostButton);
            EntityManager.Remove(readyButton);
            EntityManager.Remove(cancelButton);
            EntityManager.Remove(keysButton);

    }

    private async Task<Host> WaitForDiscoverHostAsync(TimeSpan timeOut)
        {
            Host discoveredHost = null;
            HostDiscovered hostDiscoveredHandler = (sender, host) =>
            {
                discoveredHost = host;
            };
            
            this.networkService.HostDiscovered += hostDiscoveredHandler;
            this.networkService.DiscoveryHosts(ApplicationId, Port);
            await System.Threading.Tasks.Task.Delay(timeOut);
            this.networkService.HostDiscovered -= hostDiscoveredHandler;

            return discoveredHost;
        }

      
    }
}


