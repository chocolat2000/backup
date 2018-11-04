using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Backup.Commands
{
    public interface ICommand
    {
        Task Default();
        Task PrintUsage();
    }
}
