//using Microsoft.AspNetCore.Identity;
//using Microsoft.IdentityModel.Tokens;
//using System;
//using System.Collections.Generic;
//using System.IdentityModel.Tokens.Jwt;
//using System.Security.Claims;
//using System.Text;
//using HamidaniTree.Model;
//using HamidaniTree.Tools;
//using System.Linq;

//namespace HamidaniTree.Services
//{
//    public interface IJobConfiguration
//    {
//        string ConnectionString { get;  }
//    }
//    public class JobConfiguration : IJobConfiguration
//    {
//        public JobConfiguration()
//        {
//        }
//        public string ConnectionString { get; set; }
//    }

//    public interface IIdentityService
//    {
//        string GenerateUserJWT(AppUser user, UserManager<AppUser> _userManager);
//    }

//    public class IdentityService : IIdentityService
//    {
//        private readonly JwtSettings jwtSettings;
//        //private readonly UserManager<AppUser> userManager;

//        public IdentityService(JwtSettings jwtSettings)
//                               //UserManager<AppUser> userManager)
//        {
//            this.jwtSettings = jwtSettings;
//            //this.userManager = userManager;
//        }

//        public  string GenerateUserJWT(AppUser user, UserManager<AppUser> _userManager)
//        {
//            var tokenHandler = new JwtSecurityTokenHandler();
//            var key = Encoding.ASCII.GetBytes(jwtSettings.Secret);

//            List<Claim> claims = new List<Claim> {
//                            new Claim(ClaimTypes.Name, user.UserName),
//                            new Claim(JwtRegisteredClaimNames.Sub, !string.IsNullOrEmpty(user.Email) ? user.Email : user.UserName),
//                            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
//                            new Claim(nameof(AppUser.Id), user.Id.ToString()),
//                            };

//            var roles =  new string[] {"Admin"};

//            if (roles != null && roles.Length > 0)
//            {
//                foreach (var role in roles)
//                {
//                    claims.Add(new Claim(ClaimTypes.Role, role));
//                }
//            }
//            var tokenDescriptor = new SecurityTokenDescriptor
//            {
//                //Claims are related to getCurrentUser in MyControllerBase
//                Subject = new ClaimsIdentity(claims),
//                Expires = DateTime.UtcNow.AddDays(30),
//                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
//            };

//            var token = tokenHandler.CreateToken(tokenDescriptor);
//            return tokenHandler.WriteToken(token);
//        }
//    }
//}