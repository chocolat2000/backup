using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Crypto
{
    public class Encrypt : IDisposable
    {
        private const char ENCRYPT_SPLIT = '$';
        private static readonly SHA256 sha256 = new SHA256Managed();
        private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();
        private readonly byte[] key;

        public Encrypt(byte[] key)
        {
            this.key = sha256.ComputeHash(key);
        }

        public string EnryptSync(string message)
        {
            byte[] encrypted;
            byte[] IV;

            using (var aesAlg = Aes.Create())
            {
                IV = new byte[aesAlg.BlockSize / 8];
                Rng.GetBytes(IV);
                aesAlg.IV = IV;
                aesAlg.Key = key;

                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, aesAlg.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        using (var swEncrypt = new StreamWriter(csEncrypt, Encoding.UTF8))
                        {
                            swEncrypt.Write(message);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }

            }

            return $"{Convert.ToBase64String(IV)}{ENCRYPT_SPLIT}{Convert.ToBase64String(encrypted)}";

        }

        public Task<string> Enrypt(string message)
        {
            return Task.Run(() =>
                    {
                        return EnryptSync(message);
                    });
        }

        public string DecryptSync(string message)
        {
            var messageSplited = message.Split(ENCRYPT_SPLIT);
            if (messageSplited.Length != 2)
                throw new ArgumentException("Encoded message format not recognised", nameof(message));

            var IV = Convert.FromBase64String(messageSplited[0]);
            var encrypted = Convert.FromBase64String(messageSplited[1]);

            string decrypted;

            using (var aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = IV;

                using (var msDecrypt = new MemoryStream(encrypted))
                {
                    using (var csDecrypt = new CryptoStream(msDecrypt, aesAlg.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        using (var srDecrypt = new StreamReader(csDecrypt, Encoding.UTF8))
                        {
                            decrypted = srDecrypt.ReadToEnd();
                        }
                    }
                }

            }

            return decrypted;
        }

        public Task<string> Decrypt(string message)
        {
            return Task.Run(() =>
            {
                return DecryptSync(message);
            });
        }

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
        // ~Encrypt() {
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
