using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Organic.Plugins
{
    public interface IPlugin
    {
        string Name { get; }
        string Description { get; }
        Version Version { get; }

        void Loaded(Assembler assembler);
    }
}
