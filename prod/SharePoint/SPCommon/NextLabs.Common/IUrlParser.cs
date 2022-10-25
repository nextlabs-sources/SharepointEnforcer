using System;
using System.Collections.Generic;
using System.Text;

namespace NextLabs.Common
{
    public enum SPUrlType
    {
        SPUrlTypeUnknown = 0,
        SPUrlTypeWeb = 1,
        SPUrlTypeList = 2,
        SPUrlTypeListView = 3,
        SPUrlTypeListItem = 4
    }

    interface IUrlParser
    {
        void Parse();
    }
}
