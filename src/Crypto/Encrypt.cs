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
        private const CipherMode ENCRYPT_CIPHERMODE = CipherMode.CTS;
        private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();
        private readonly byte[] key;

        public Encrypt(byte[] key)
        {
            this.key = key;
        }


        public Task<string> Enrypt(string message)
        {
            return Task.Run(() =>
                    {
                        byte[] encrypted;
                        byte[] IV;

                        using (var aesAlg = Aes.Create())
                        {
                            IV = new byte[aesAlg.BlockSize / 8];
                            Rng.GetBytes(IV);
                            aesAlg.IV = IV;
                            aesAlg.Key = key;
                            aesAlg.Mode = ENCRYPT_CIPHERMODE;

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
                    });
        }

        public Task<string> Decrypt(string message)
        {
            return Task.Run(() =>
            {
                var messageSplited = message.Split(ENCRYPT_SPLIT);
                if (messageSplited.Length != 2)
                    return null;

                var IV = Convert.FromBase64String(messageSplited[0]);
                var encrypted = Convert.FromBase64String(messageSplited[1]);

                string decrypted;

                using (var aesAlg = Aes.Create())
                {
                    aesAlg.Key = key;
                    aesAlg.IV = IV;
                    aesAlg.Mode = ENCRYPT_CIPHERMODE;
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
