using System;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace BackupDatabase.FileSystem
{
    public class FileSystemDB : IDataDBAccess
    {
        internal const int DefaultBufferSize = 4096;
        internal readonly string BACKUP_FOLDER;

        public FileSystemDB(string baseLocation)
        {
            BACKUP_FOLDER = baseLocation;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<byte[]> ReadBlock(Guid id)
        {
            var guidArray = id.ToString();
            using (
                var fs = new FileStream(
                    $@"{BACKUP_FOLDER}\{guidArray[0]}\{guidArray[1]}\{guidArray[2]}\{guidArray[3]}\{guidArray[4]}\{guidArray}",
                    FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize,
                    FileOptions.Asynchronous | FileOptions.SequentialScan)
                )
            {
                var index = 0;
                var count = (int)fs.Length;
                var bytes = new byte[count];
                do
                {
                    var n = await fs.ReadAsync(bytes, index, count - index);
                    if (n == 0)
                    {
                        throw new EndOfStreamException($"End of file reached: {fs.Name}");
                    }

                    index += n;
                } while (index < count);

                return bytes;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task WriteBlock(Guid id, byte[] data)
        {
            return WriteBlock(id, data, data.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task WriteBlock(Guid id, byte[] data, int length)
        {
            var guidArray = id.ToString();
            var blockFileName = $@"{BACKUP_FOLDER}\{guidArray[0]}\{guidArray[1]}\{guidArray[2]}\{guidArray[3]}\{guidArray[4]}\{guidArray}";
            Directory.CreateDirectory(Path.GetDirectoryName(blockFileName));
            using (
                var fs = new FileStream(
                    blockFileName,
                    FileMode.Create, FileAccess.Write, FileShare.Read,
                    DefaultBufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan)
                )
            {
                await fs.WriteAsync(data, 0, length < 0 || length > data.Length ? data.Length : length);
                await fs.FlushAsync();
            }

        }

    }
}
