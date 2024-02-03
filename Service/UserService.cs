using alight_exam.Models;
using Microsoft.Extensions.Configuration;

namespace alight_exam.Service
{
    public class UserService
    {
        private readonly IConfiguration _configuration;

        public UserService(IConfiguration configuration) {
            _configuration = configuration;
        }

        public User GetUsers(int userId) {
            throw new NotImplementedException();   
        }

        public User CreateUser(User newUser)
        {
            throw new NotImplementedException();
        }

        public User UpdateUser(User newUser)
        {
            throw new NotImplementedException();
        }

        private bool Validate()
        {
            throw new NotImplementedException();
        }
    }
}
