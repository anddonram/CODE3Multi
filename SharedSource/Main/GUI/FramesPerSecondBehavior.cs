using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using WaveEngine.Framework;
using WaveEngine.Framework.Services;

namespace Codigo.Behaviors
{
    class FramesPerSecondBehavior : SceneBehavior
    {
        private Boolean cooldown = false;
        private int count = 0;
        protected override void ResolveDependencies()
        {
            
        }

        protected override void Update(TimeSpan gameTime)
        {
            if (!cooldown)
            {
                Trace.WriteLine(count);
                count = 0;
                cooldown = true;
                WaveServices.TimerFactory.CreateTimer(TimeSpan.FromSeconds(1), () => { cooldown = false; }).Looped = false;
            }
            else
            {
                count++;
            }
        }
    }
}
