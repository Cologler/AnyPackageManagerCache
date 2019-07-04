using AnyPackageManagerCache.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace AnyPackageManagerCache.Utils
{
    public struct HashResult
    {
        public HashResult(HashAlgorithmName name, string value)
        {
            this.Name = name;
            this.Value = value;
        }

        public HashAlgorithmName Name { get; }

        public string Value { get; }

        public bool Equals(byte[] buffer)
        {
            return buffer != null && buffer.Hash(this.Name).Equals(this.Value, StringComparison.OrdinalIgnoreCase);
        }
    }
}
