using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace couch_backend.Utilities
{
    /// <summary>
    /// Helper methods
    /// </summary>
    public class Helper
    {
        private static readonly Random _random = new Random();

        /// <summary>
        /// Get Random Token
        /// </summary>
        /// <param name="length">Token length</param>
        public static string GetRandomToken(int length)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[_random.Next(s.Length)]).ToArray());
        }

        /// <summary>
        /// Get Random Hexadecimal token
        /// </summary>
        /// <param name="length">Token length</param>
        public static string GetRandomHexToken(int length = 24)
        {
            const string chars = "0123456789abcdef";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[_random.Next(s.Length)]).ToArray());
        }

        /// <summary>
        /// Generates a Random Password
        /// </summary>
        /// <returns>A random password</returns>
        public static string GenerateRandomPassword()
        {
            var opts = new PasswordOptions()
            {
                RequiredLength = 8,
                RequiredUniqueChars = 4,
                RequireDigit = true,
                RequireLowercase = true,
                RequireNonAlphanumeric = true,
                RequireUppercase = true
            };

            string[] randomChars = new[] {
                "ABCDEFGHJKLMNOPQRSTUVWXYZ",    // uppercase 
                "abcdefghijkmnopqrstuvwxyz",    // lowercase
                "0123456789",                   // digits
                "!@$?_-"                        // non-alphanumeric
            };

            Random rand = new Random(Environment.TickCount);
            List<char> chars = new List<char>();

            chars.Insert(rand.Next(0, chars.Count), randomChars[0][rand.Next(0, randomChars[0].Length)]);
            chars.Insert(rand.Next(0, chars.Count), randomChars[1][rand.Next(0, randomChars[1].Length)]);
            chars.Insert(rand.Next(0, chars.Count), randomChars[2][rand.Next(0, randomChars[2].Length)]);
            chars.Insert(rand.Next(0, chars.Count), randomChars[3][rand.Next(0, randomChars[3].Length)]);

            for (int i = chars.Count; i < opts.RequiredLength
                || chars.Distinct().Count() < opts.RequiredUniqueChars; i++)
            {
                string rcs = randomChars[rand.Next(0, randomChars.Length)];

                chars.Insert(rand.Next(0, chars.Count),
                    rcs[rand.Next(0, rcs.Length)]);
            }

            return new string(chars.ToArray());
        }

        /// <summary>
        /// Verify a base64 string
        /// </summary>
        /// <param name="encodedString"></param>
        /// <returns></returns>
        public static bool IsValidBase64String(string encodedString)
        {
            Span<byte> buffer = new Span<byte>(new byte[encodedString.Length]);

            return Convert.TryFromBase64String(encodedString, buffer, out int bytesParsed);
        }

        /// <summary>
        /// Verify a url string
        /// </summary>
        /// <param name="url"></param>
        public static bool IsValidUrl(string url)
        {
            Uri uriResult;

            bool result = Uri.TryCreate(url, UriKind.Absolute, out uriResult) &&
                                       (uriResult.Scheme == Uri.UriSchemeHttp ||
                                        uriResult.Scheme == Uri.UriSchemeHttps);

            return result;
        }

        /// <summary>
        /// Verify a date string can be converted to a valid date
        /// </summary>
        /// <param name="dateString"></param>
        public static bool IsValidDateString(string dateString)
        {
            return DateTime.TryParseExact(dateString.Replace('/', '-'), "yyyy-M-d",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        }

        /// <summary>
        /// Verify a DateTime string can be converted to a valid DateTime
        /// </summary>
        /// <param name="dateTimeString"></param>
        public static bool IsValidDateTimeString(string dateTimeString)
        {
            return DateTime.TryParseExact(dateTimeString.Replace('/', '-'), "yyyy-M-dTH:m",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        }
    }

    /// <summary>
    /// Limits a model string property to a predefined set of values
    /// </summary>
    public class StringRangeAttribute : ValidationAttribute
    {
        public string[] AllowableValues { get; set; }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (AllowableValues?.Contains(value?.ToString()) == true)
            {
                return ValidationResult.Success;
            }

            var msg = $"Please enter one of the allowable values: {string.Join(", ", (AllowableValues ?? new string[] { "No allowable values found" }))}.";
            return new ValidationResult(msg);
        }
    }

    public static class ThreadSafeRandom
    {
        [ThreadStatic] private static Random Local;

        public static Random ThisThreadsRandom
        {
            get { return Local ?? (Local = new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId))); }
        }
    }

    static class MyExtensions
    {
        public static void Shuffle<T>(this IEnumerable<T> list)
        {
            list = list.OrderBy(x => new Random().Next());
        }
    }
}
