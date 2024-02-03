using alight_exam.Models;
using alight_exam.Service;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace alight_exam.Controllers
{
    [Route("api/user")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly IConfiguration _configuration;

        public UserController(UserService userService, IConfiguration configuration) {
            _userService = userService;
            _configuration = configuration;
        }

        [HttpGet]        
        [Route("{Id}")]
        public IActionResult Get(int id) {
            return Ok(_userService.GetUserById(id));
        }

        [HttpPost]
        [Route("")]
        public IActionResult Add([FromBody] User user)
        {
            Dictionary<string, string> errorMsgDict = new Dictionary<string, string>();
            var newUser = _userService.CreateUser(user, ref errorMsgDict);

            if (newUser == null)
                return BadRequest(errorMsgDict);

            return Created("", newUser);
        }

        [HttpPut]
        [Route("")]
        public IActionResult Update([FromBody] User user)
        {
            Dictionary<string, string> errorMsgDict = new Dictionary<string, string>();
            var newUser = _userService.UpdateUser(user, ref errorMsgDict);

            if (newUser == null)
                return BadRequest(errorMsgDict);

            return Ok(newUser);
        }
    }
}
