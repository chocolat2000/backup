using System;
using System.Threading.Tasks;

namespace BackupDatabase
{
    public interface IDataDBAccess
    {
        Task<byte[]> ReadBlock(Guid id);
        Task WriteBlock(Guid id, byte[] data, int length = -1);
    }
}
