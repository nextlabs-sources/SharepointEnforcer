using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;

namespace Nextlabs.SPE.Console
{
    interface ISpeAdminCommand
    {
        string GetHelpString(string feature);

        void Run(string feature, StringDictionary keyValues);
    }
}
