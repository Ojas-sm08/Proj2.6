// Utility/PasswordHasher.cs
using System.Security.Cryptography;
using System.Text;
using System;

namespace HospitalManagementSystem.Utility
{
    public static class PasswordHasher
    {
        // Hashes a plain-text password using SHA256.
        // In a real application, consider more robust hashing algorithms like Argon2 or BCrypt
        // with proper salt generation and storage for higher security.
        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                // Convert the password string to a byte array
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

                // Convert the byte array to a hexadecimal string
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        // Verifies a plain-text password against a stored hash.
        public static bool VerifyPassword(string enteredPassword, string storedHash)
        {
            // Hash the entered password and compare it with the stored hash
            return HashPassword(enteredPassword) == storedHash;
        }
    }
}
