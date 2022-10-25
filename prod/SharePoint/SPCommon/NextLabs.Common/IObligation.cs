using System;
using System.Collections.Generic;
using System.Text;

namespace NextLabs.Common
{
    public interface IObligation
    {
        void Process(List<Obligation> obligations, IntPtr hConnect);
    }
}
