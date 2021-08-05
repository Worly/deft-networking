using System;
using System.Collections.Generic;
using System.Text;

namespace Deft.Utils.Settings
{
    public interface ISettings
    {
        bool HasKey(string key);
        string GetValue(string key);
        void SetValue(string key, string value);
    }
}
