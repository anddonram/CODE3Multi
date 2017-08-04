using System;
using System.Collections.Generic;
using System.Text;
using WaveEngine.Framework;

namespace Codigo.Components
{

    /**
     * <summary>
     * This component handles the destruction of a castle part, destroying the whole castle if all parts have been destroyed
     * </summary>
     */
   public class CastlePart:Component
    {
        private Castle castle;
        [RequiredComponent]
        private WorldObject wo;

        private bool removed = false;
        protected override void Removed()
        {
            if (!removed)
            {
                base.Removed();
                castle.DestroyPart(wo);
                castle = null;
                removed = true;
            }
        }
        public void SetCastle(Castle castle)
        {
            this.castle = castle;
        }
    }
}
