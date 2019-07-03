using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace AnyPackageManagerCache.Extensions
{
    public static class BytesExtensions
    {
        public static string Hash(this byte[] buffer, HashAlgorithmName name)
        {
            using (var algorithm = HashAlgorithm.Create(name.Name))
            {
                var hashBytes = algorithm.ComputeHash(buffer);
                var hashString = BitConverter.ToString(hashBytes).Replace("-", String.Empty);
                return hashString;
            }
        }
    }
}
