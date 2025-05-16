using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Voltyks.Application.ServicesManager.ServicesManager;
using Voltyks.Core;


namespace Voltyks.Presentation
{
    [ApiController]
    [Route("api/[Controller]")]
    public class AuthController(IServiceManager serviceManager) : ControllerBase
    {
        // Login
        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginDTO loginDTO)
        {
            var result = await serviceManager.AuthService.LoginAsync(loginDTO);
            return Ok(result);
        }


        // Register
        [HttpPost("Register")]
        public async Task<IActionResult> Register(RegisterDTO registerDTO)
        {
            var result = await serviceManager.AuthService.RegisterAsync(registerDTO);
            return Ok(result);
        }
    }
}
