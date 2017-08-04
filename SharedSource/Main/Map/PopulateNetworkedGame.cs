using System;
using System.Collections.Generic;
using System.Text;
using WaveEngine.Common.Math;
using WaveEngine.Framework.Managers;
using WaveEngine.TiledMap;

namespace Codigo
{
    /**
     * <summary>
     * This class is used to populate a networked game
     * </summary>
     */
    public class PopulateNetworkedGame
    {
        private int width = 12;
        private int height = 20;

        private int woodStart = 4;
        private int woodEnd = 16;
        /**
         * <summary>
         * The extension of the wood relative to the total map
         * </summary>
         */
        private float woodPercentage = 0.6f;

        private float obstacleFrequency = 0.05f;
        private float emptyFrequency = 0.05f;
        private Random rnd = new Random();

        public const int castleWidth = 4;
        public const int castleHeight = 2;

        private int riverSize = 2;

        public PopulateNetworkedGame()
        {
            width = Map.map.width;
            height = Map.map.height;
            woodStart =(int) (height * (1 - woodPercentage) / 2);
            woodEnd=(int) (height * (1 + woodPercentage) / 2);
        }
        /**
         * The number of persons we will have available at the start of the game
         */
        private int personNum = 4;

        public void SetPlayer(ServerDispatcher disp,Player p, int playerNum,int maxPlayers)
        {
            SendData data = new SendData();
            data.clientId = p.Owner.Name.Substring(6);
            PopulateInitialPosition initial = PopulateInitialPosition.GetPlayerPosition(playerNum, maxPlayers,
                width,height,castleWidth,castleHeight);
            int x = initial.x;
            int y = initial.y;

    
            data.position = Map.map.GetTileByMapCoordinates(x, y).LocalPosition;
            data.creating = "Castle";
            disp.CreateAndSync(data);

            //create person
            data.creating = "Person";
            for (int i = 0; i < personNum; i++)
            {
                data.position = Map.map.GetTileByMapCoordinates(x+i, y).LocalPosition;
                disp.CreateAndSync(data);
            }
        }

        public void SetWoods(ServerDispatcher disp)
        {
            SendData data = new SendData();
            data.clientId = "";
            for (int i=woodStart;i<woodEnd;i++)
                for(int j = 0; j < width; j++)
                {
                    
                    data.position = Map.map.GetTileByMapCoordinates(j,i).LocalPosition;
                    if (rnd.NextDouble() <= obstacleFrequency)
                    {
                        data.creating = "Rock";
                        disp.CreateAndSync(data);
                    }
                    if (rnd.NextDouble() <= emptyFrequency)
                    {    
                    }
                    else {
                        data.creating = "Tree";
                        disp.CreateAndSync(data);
                    }
                }
        }
        public void SetRiver(ServerDispatcher disp)
        {
            SendData data = new SendData();
            data.clientId = "";
            for (int i = (height-riverSize)/2; i < (height + riverSize) / 2; i++)
                for (int j = 0; j < width; j++)
                {
                    data.position = Map.map.GetTileByMapCoordinates(j, i).LocalPosition;
                    data.creating = "Water";
                    disp.CreateAndSync(data);
                }
        }
        /**
         * <summary>
         * Sets the camera according to the initial position of the player
         * </summary>
         */
        public void SetCameraPosition(int number,int max,NetworkedScene ns)
        {
         PopulateInitialPosition init=   PopulateInitialPosition.GetPlayerPosition(number,max,width,height,castleWidth,castleHeight);
           LayerTile tile= Map.map.GetTileByMapCoordinates(init.x+castleWidth/2, init.y+castleHeight/2);
            if (tile != null)
            {
                Vector2 pos=tile.LocalPosition + WorldObjectData.center;
                ns.RenderManager.ActiveCamera2D.Transform.Position = pos;
            }
        }
    }
}

