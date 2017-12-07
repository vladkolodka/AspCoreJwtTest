using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using JwtTest.Database.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace JwtTest.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;

        public HomeController(SignInManager<User> signInManager, UserManager<User> userManager,
            RoleManager<Role> roleManager, IConfiguration configuration)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;

            
        }

        public IActionResult Index()
        {
            return Content($"Server launched. Data: {_configuration["Jwt:Issuer"]}");
        }

        public async Task<IActionResult> Register()
        {
            await _roleManager.CreateAsync(new Role
            {
                Name = "TestRole"
            });

            var u = new User
            {
                UserName = "User1"
            };

            var r = await _userManager.CreateAsync(u, "TestPass1!");

            await _userManager.AddToRoleAsync(u, "TestRole");

            return Json(r.Succeeded);
        }

        public async Task<IActionResult> Login()
        {
            var r = await _signInManager.PasswordSignInAsync("User1", "TestPass1!", false, false);

            return Json(r.Succeeded);
        }

        public async Task<IActionResult> Token()
        {
            var user = await GetClaimsForUser("User1");

            var jwt = new JwtSecurityToken(_configuration["Jwt:Issuer"], _configuration["Jwt:Audience"],
                notBefore: DateTime.UtcNow, claims: user.Claims,
                expires: DateTime.UtcNow.AddDays(60),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])),
                    SecurityAlgorithms.HmacSha256)
            );

            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            return Json(new
            {
                Token = encodedJwt,
                user.Name
            });
        }

        private async Task<ClaimsIdentity> GetClaimsForUser(string userName)
        {
//            var user = await _userManager.FindByNameAsync(userName);

            var user = new User
            {
                UserName = "User1",
                Id = Guid.NewGuid(),
                SecurityStamp = Guid.NewGuid().ToString()
            };

//            if (user == null) return null;
            
            return new ClaimsIdentity(new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, user.Id.ToString()),
                }, JwtBearerDefaults.AuthenticationScheme, ClaimsIdentity.DefaultNameClaimType,
                ClaimsIdentity.DefaultRoleClaimType);
        }
        [Authorize]
        public IActionResult Secure1()
        {
            var claims = User.Claims;

            return Content("Secured by cookies.");
        }



        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public IActionResult Secure()
        {
            var claims = User.Claims;


            return Content("This is secure page. If you read this then you are authorized in our system.");
        }
    }
}