using System;
using System.Collections.Generic;
using System.Text;

namespace NextLabs.Common
{
    interface IEvaluation
    {
        string ReConstructUrl();

        bool Run();
    }
}
