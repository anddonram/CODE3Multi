using System;
using System.Collections.Generic;
using System.Text;
using WaveEngine.Common.Math;

namespace Codigo
{
    /**
      * <summary>
      * Auxiliar class for recovering data from messages
      * </summary>
      */
    public class SendData
    {
        /**
         * <summary>
         * The name of the world object we want to create-update
         * </summary>
         */
        public string creating;
        /**
        * <summary>
        * The position of the world object in world coordinates
        * </summary>
        */
        public Vector2 position;
        /**
        * <summary>
        * The player who owns this entity
        * </summary>
        */
        public string clientId;
        /**
         * <summary>
         * The name of the entity in the entity manager
         * </summary>
         */
        public string realName;
    }
}
