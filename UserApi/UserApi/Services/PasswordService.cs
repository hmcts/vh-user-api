using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;

namespace UserApi.Services
{
    public class PasswordService : IPasswordService
    {
        public string GenerateRandomPasswordWithDefaultComplexity()
        {
            return Generate(new PasswordOptions
            {
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = true,
                RequireNonAlphanumeric = true,
                RequiredLength = 12
            });
        }

        private static string Generate(PasswordOptions passwordOptions)
        {
            var randomChars = new[]
            {
                "ABCDEFGHJKLMNOPQRSTUVWXYZ", // uppercase 
                "abcdefghijkmnopqrstuvwxyz", // lowercase
                "0123456789", // digits
                "!@$?_-" // non-alphanumeric
            };
            
            var bytes = new byte[64];
            var chars = new List<char>();

            if (passwordOptions.RequireUppercase)
            {
                chars.Insert(GetRandomInt(bytes) % (chars.Count + 1), randomChars[0][GetRandomInt(bytes) % randomChars[0].Length]);
            }

            if (passwordOptions.RequireLowercase)
            {
                chars.Insert(GetRandomInt(bytes) % (chars.Count + 1), randomChars[1][GetRandomInt(bytes) % randomChars[1].Length]);
            }

            if (passwordOptions.RequireDigit)
            {
                chars.Insert(GetRandomInt(bytes) % (chars.Count + 1), randomChars[2][GetRandomInt(bytes) % randomChars[2].Length]);
            }

            if (passwordOptions.RequireNonAlphanumeric)
            {
                chars.Insert(GetRandomInt(bytes) % (chars.Count + 1), randomChars[3][GetRandomInt(bytes) % randomChars[3].Length]);
            }

            for (var i = chars.Count; i < passwordOptions.RequiredLength || chars.Distinct().Count() < passwordOptions.RequiredUniqueChars; i++)
            {
                var rcs = randomChars[GetRandomInt(bytes) % randomChars.Length];
                chars.Insert(GetRandomInt(bytes) % (chars.Count + 1), rcs[GetRandomInt(bytes) % rcs.Length]);
            }

            return new string(chars.ToArray());
        }
        
        private static int GetRandomInt(byte[] bytes)
        {
            RandomNumberGenerator.Fill(bytes);
            return BitConverter.ToInt32(bytes, 0) & int.MaxValue; // Ensure non-negative integer
        }
    }
}