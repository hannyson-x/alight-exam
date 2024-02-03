using alight_exam.Models;
using alight_exam.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
        public HttpResponseMessage Get(long id) {
            throw new NotImplementedException();
        }

        [HttpPost]
        [Route("")]
        public HttpResponseMessage Add([FromBody] User user)
        {
            throw new NotImplementedException();
        }

        [HttpPut]
        [Route("{Id}")]
        public HttpResponseMessage Update([FromBody] User user, int Id)
        {
            throw new NotImplementedException();
        }
    }
}
