//using Microsoft.AspNetCore.Identity;
//using System.IO;
//using System.Security.Cryptography;
//using System.Text;
//using HamidaniTree.Model;

//namespace HamidaniTree.Tools
//{
//    public class MD5PasswordHasher : IPasswordHasher<AppUser>
//    {
//        public string HashPassword(AppUser user, string password)
//        {
//            return GetMd5Hash(password);
//        }

//        public PasswordVerificationResult VerifyHashedPassword(AppUser user, string hashedPassword, string providedPassword)
//        {
//            if (hashedPassword == GetMd5Hash(providedPassword))
//            {
//                return PasswordVerificationResult.Success;
//            }

//            return PasswordVerificationResult.Failed;
//        }

//        public static string GetMd5Hash(string input)
//        {
//            MD5 md5Hash = MD5.Create();
//            // Convert the input string to a byte array and compute the hash.
//            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
//            StringBuilder sBuilder = new StringBuilder();
//            for (int i = 0; i < data.Length; i++)
//            {
//                sBuilder.Append(data[i].ToString("X2"));
//            }
//            return sBuilder.ToString();
//        }
//    }
//}
