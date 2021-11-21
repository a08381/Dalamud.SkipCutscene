using Dalamud.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugins.a08381.SkipCutscene
{
    class Config : IPluginConfiguration
    {

        public int Version { get; set; }

        public bool IsEnabled { get; set; }
    }
}
