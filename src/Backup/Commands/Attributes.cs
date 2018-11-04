using System;
using System.Collections.Generic;
using System.Text;

namespace Backup.CommandsAttributes
{
    public class CommandAttribute : Attribute
    {
        public string Name { get; }

        public CommandAttribute(string name = null)
        {
            Name = name;
        }

    }

    public class ActionAttribute : Attribute
    {
        public string Name { get; }

        public ActionAttribute(string name = null)
        {
            Name = name;
        }

    }
}
