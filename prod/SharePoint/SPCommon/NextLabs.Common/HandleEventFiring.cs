using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SharePoint;

namespace NextLabs.Common
{
    public class HandleEventFiring : SPItemEventReceiver
    {
        public HandleEventFiring()
        {
        }

        public void CustomDisableEventFiring()
        {
            this.EventFiringEnabled = false;
        }

        public void CustomEnableEventFiring()
        {
            this.EventFiringEnabled = true;
        }
    }
}
