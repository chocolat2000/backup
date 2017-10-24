using BackupDatabase.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BackupDatabase
{
    public interface IUsersDBAccess : IDisposable
    {
        Task<DBUser> GetUser(string login, string password = null);

        Task AddUser(string login, string password, IEnumerable<string> roles = null);
    }
}
