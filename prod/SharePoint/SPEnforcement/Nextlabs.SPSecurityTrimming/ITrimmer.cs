using System;
using System.Collections.Generic;
using System.Text;

namespace Nextlabs.SPSecurityTrimming
{
    interface ITrimmer
    {
        // true: Trimmed; false: not trimmed.
        bool DoTrimming();
    }
}
