#region Using Statements
using System;
using WaveEngine.Common;
using WaveEngine.Common.Graphics;
using WaveEngine.Framework;
using WaveEngine.Framework.Services;
using WaveEngine.Networking;
#endregion

namespace Codigo
{
    public class Game : WaveEngine.Framework.Game
    {
        public override void Initialize(IApplication application)
        {
            base.Initialize(application);

            ScreenContext screenContext = new ScreenContext(new NetworkedScene());
            //ScreenContext screenContext = new ScreenContext(new MyScene());
            //ScreenContext screenContext = new ScreenContext(new PlayableScene());
       
            //ScreenContext screenContext = new ScreenContext(new TestScene(0));	
            WaveServices.ScreenContextManager.To(screenContext);
            
            //This enables an fps counter, lol it had one and we needn't do it ourselves
            //WaveServices.ScreenContextManager.SetDiagnosticsActive(true);
        }
    }
}
