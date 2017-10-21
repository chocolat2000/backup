using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Crypto
{
    public class Hashing : IDisposable
    {
        private const int PBKDF2_ITERATIONS = 1000;
        private const int PBKDF2_SALTLENGTH = 16;
        private const int PBKDF2_HASHLENGTH = 16;
        private const char PBKDF2_HASHSPLIT = '$';
        private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

        public Task<string> HashPassword(string password)
        =>
            Task.Run(() =>
            {
                var salt = new byte[PBKDF2_SALTLENGTH];
                Rng.GetBytes(salt);

                byte[] hash;
                using (var k = new Rfc2898DeriveBytes(password, salt, PBKDF2_ITERATIONS))
                {
                    hash = k.GetBytes(PBKDF2_HASHLENGTH);
                }

                return $"{Convert.ToBase64String(salt)}{PBKDF2_HASHSPLIT}{Convert.ToBase64String(hash)}";
            });

        public Task<bool> VerifyPassword(string password, string hash)
        =>
            Task.Run(() =>
            {
                var hashParts = hash.Split(PBKDF2_HASHSPLIT);
                if (hashParts.Length != 2)
                    return false;

                var salt = Convert.FromBase64String(hashParts[0]);
                var dbHash = Convert.FromBase64String(hashParts[1]);

                // hash length in database has changed ?
                if (dbHash.Length != PBKDF2_HASHLENGTH)
                    return false;

                byte[] giventHash;
                using (var k = new Rfc2898DeriveBytes(password, salt, PBKDF2_ITERATIONS))
                {
                    giventHash = k.GetBytes(PBKDF2_HASHLENGTH);
                }

                var verified = true;
                for (int i = 0; i < PBKDF2_HASHLENGTH; i++)
                {
                    if (giventHash[i] != dbHash[i])
                        verified = false;
                }

                // If gone so far, password is verified.
                return verified;

            });

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Rng.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Hashing() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }
}
