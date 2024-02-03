using alight_exam.Models;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.ConstrainedExecution;

namespace alight_exam.Service
{
    public class UserService
    {
        private readonly IConfiguration _configuration;
        private readonly string? _connectionString;
        public UserService(IConfiguration configuration) {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        public User GetUserById(int userId) {
            var user = new User();

            using (SqlConnection mainCnn = new SqlConnection(_connectionString)) {
                mainCnn.Open();

                var sqlCommand = new SqlCommand($"SELECT TOP 1 Id, FirstName, LastName, Email FROM [User] WHERE Id = {userId}", mainCnn);

                using (var sqlDataReader = sqlCommand.ExecuteReader()) {
                    while (sqlDataReader.Read()) {
                        user.Id = sqlDataReader.GetInt32(0);
                        user.FirstName = sqlDataReader.GetString(1);
                        user.LastName = sqlDataReader.GetString(2);
                        user.Email = sqlDataReader.GetString(3);
                    }
                }

                user.Address = GetAddressByUserId(userId, mainCnn);
                user.Employments = GetEmploymentsByUserId(userId, mainCnn);
            }

            return user;
        }

        private Address GetAddressByUserId(int userId, SqlConnection conn) {
            Address address = new Address();
            var sqlCommand = new SqlCommand($"SELECT TOP 1 Id, Street, City, PostCode FROM [Address] WHERE UserId = {userId}", conn);
            using (var sqlDataReader = sqlCommand.ExecuteReader())
            {
                while (sqlDataReader.Read())
                {
                    address.Id = sqlDataReader.GetInt32(0);
                    address.Street = sqlDataReader.GetString(1);
                    address.City = sqlDataReader.GetString(2);
                    address.PostCode = sqlDataReader.IsDBNull(3) ? null : sqlDataReader.GetInt32(3);
                    address.UserId = userId;
                }
            }

            return address;
        }

        private List<Employment> GetEmploymentsByUserId(int userId, SqlConnection conn) { 
            List<Employment> employments = new List<Employment>();

            var sqlCommand = new SqlCommand($"SELECT Id, Company, MonthsOfExperience, Salary, StartDate, EndDate FROM [Employment] WHERE UserId = {userId}", conn);
            using (var sqlDataReader = sqlCommand.ExecuteReader())
            {
                while (sqlDataReader.Read())
                {
                    employments.Add(new Employment() { 
                        Id = sqlDataReader.GetInt32(0),
                        Company = sqlDataReader.GetString(1),
                        MonthsOfExperience = Convert.ToUInt32(sqlDataReader.GetInt32(2)),
                        Salary = Convert.ToUInt32(sqlDataReader.GetInt32(3)),
                        StartDate =sqlDataReader.GetDateTime(4),
                        EndDate = sqlDataReader.IsDBNull(5) ? null : sqlDataReader.GetDateTime(5),
                        UserId = userId
                    });
                }
            }

            return employments;
        }

        public User CreateUser(User newUser, ref Dictionary<string, string> errorMsgDict)
        {
            try
            {
                if (!ValidateUser(newUser, null, ref errorMsgDict)) return null;

                string insertSQL = "INSERT INTO [User](FirstName, LastName, Email) output INSERTED.ID VALUES('{0}','{1}','{2}')";
                insertSQL = string.Format(insertSQL, newUser.FirstName, newUser.LastName, newUser.Email);

                using (SqlConnection mainCnn = new SqlConnection(_connectionString))
                {
                    mainCnn.Open();

                    var sqlCommand = new SqlCommand(insertSQL, mainCnn);

                    newUser.Id = (int)sqlCommand.ExecuteScalar();
                    newUser.Employments = CreateEmployments(newUser.Employments, newUser.Id, mainCnn);
                    newUser.Address = CreateAddress(newUser.Address, newUser.Id, mainCnn);
                }


                return newUser;
            }
            catch (Exception ex) {
                errorMsgDict["ServerError"] = ex.Message;
                return null;
            }
            
        }

        public User UpdateUser(User newUser, ref Dictionary<string, string> errorMsgDict)
        {
            User oldUser = GetUserById(newUser.Id);

            if (!ValidateUser(newUser, oldUser, ref errorMsgDict, "edit")) return null;
            
            using (SqlConnection mainCnn = new SqlConnection(_connectionString)) {
                mainCnn.Open();
                string updateSQL = $"UPDATE [User] SET FirstName = '{newUser.FirstName}', LastName = '{newUser.LastName}', Email = '{newUser.Email}' WHERE Id = {newUser.Id}";
                var sqlCommand = new SqlCommand(updateSQL, mainCnn);

                if (sqlCommand.ExecuteNonQuery() > 0) {
                    newUser.Employments = UpdateEmployments(newUser, oldUser, mainCnn);
                    newUser.Address = UpdateAddress(newUser.Address, newUser.Id, mainCnn);
                }
            }

            return newUser;
        }

        private List<Employment> UpdateEmployments(User user, User oldUser, SqlConnection conn) {
            List<Employment> newEmployments = new List<Employment>();
            List<Employment> addEmployments = new List<Employment>();

            // DELETE EMPLOYMENTS THAT IS NOT ON THE LIST
            List<int> newEmploymentIds = user.Employments.Select(a => a.Id).ToList();
            string employmentIdToBeDeleted = String.Join(",", oldUser.Employments.Where(x => !newEmploymentIds.Contains(x.Id)).Select(a => a.Id.ToString()));

            if (!string.IsNullOrEmpty(employmentIdToBeDeleted))
            {
                string deleteEmploymentSQL = $"DELETE [Employment] WHERE Id IN ({employmentIdToBeDeleted}) AND UserId ={user.Id}"; ;
                var sqlDeleteCommand = new SqlCommand(deleteEmploymentSQL, conn);
                sqlDeleteCommand.ExecuteNonQuery();
            }

            //UPDATE OR INSERT
            foreach (Employment employment in user.Employments) {

                if (employment.Id == 0) {
                    addEmployments.Add(employment);
                    continue;
                }
                string endDate = employment.EndDate == null ? "null" : $"'{employment.EndDate}'";
                string updateEmploymentSQL = $"UPDATE [Employment] SET Company = '{employment.Company}', MonthsOfExperience = {employment.MonthsOfExperience}, Salary = {employment.Salary}, StartDate = '{employment.StartDate}', EndDate = {endDate} WHERE Id = {employment.Id}";
                var sqlUpdateCommand = new SqlCommand(updateEmploymentSQL, conn);

                if (sqlUpdateCommand.ExecuteNonQuery() > 0)
                    newEmployments.Add(employment);
            }

            newEmployments.AddRange(CreateEmployments(addEmployments, user.Id, conn));

            return newEmployments;
        }

        private Address UpdateAddress(Address address, int userId, SqlConnection conn) {
            var currentAddress = GetAddressByUserId(userId, conn);

            if (address != null && address.Id == 0 && currentAddress.Id == 0) 
                return CreateAddress(address, userId, conn);

            if (currentAddress.Id > 0 && address == null) {
                DeleteAddress(currentAddress.Id, conn);
                return null;
            }

            string postalCode = address.PostCode == null ? "null" : $"{address.PostCode}";
            string updateAddressSQL = $"UPDATE [Address] SET Street = '{address.City}', City = '{address.City}', PostCode = {postalCode} WHERE Id = {address.Id}";
            var sqlUpdateCommand = new SqlCommand(updateAddressSQL, conn);
            sqlUpdateCommand.ExecuteNonQuery();
            return address;
        }

        private void DeleteAddress(int addressId, SqlConnection conn) {
            string updateAddressSQL = $"DELETE [Address] WHERE Id = {addressId}";
            var sqlUpdateCommand = new SqlCommand(updateAddressSQL, conn);
            sqlUpdateCommand.ExecuteNonQuery();
        }        

        private List<Employment> CreateEmployments(List<Employment> employments, int userId, SqlConnection conn) {
            List<Employment> newlyAddedEmployments = new List<Employment>();

            foreach (var employment in employments)
            {
                string insertEmploymentSQL = "INSERT INTO [Employment](Company, MonthsOfExperience, Salary, StartDate, EndDate, UserId) output INSERTED.ID VALUES('{0}',{1},{2},'{3}',{4},{5})";
                insertEmploymentSQL = string.Format(insertEmploymentSQL, employment.Company, employment.MonthsOfExperience, employment.Salary, employment.StartDate, employment.EndDate == null ? "null" : $"'{employment.EndDate}'", userId);

                var sqlEmploymentCommand = new SqlCommand(insertEmploymentSQL, conn);

                employment.Id = (int)sqlEmploymentCommand.ExecuteScalar();
                employment.UserId = userId;
                newlyAddedEmployments.Add(employment);
            }

            return newlyAddedEmployments;
        }

        private Address CreateAddress(Address address, int userId, SqlConnection conn) {

            if (address == null) return null;

            string insertAddressSQL = "INSERT INTO [Address](Street, City, PostCode, UserId) output INSERTED.ID VALUES('{0}','{1}',{2},{3})";
            string postCode = address.PostCode == null ? "null" : address.PostCode?.ToString();
            insertAddressSQL = string.Format(insertAddressSQL, address.Street, address.City, postCode, userId);

            var sqlAddressCommand = new SqlCommand(insertAddressSQL, conn);

            address.Id = (int)sqlAddressCommand.ExecuteScalar();
            address.UserId = userId;
            return address;
        }

        private bool ValidateUser(User newUser, User oldUser, ref Dictionary<string, string> errorMsgDict, string taskCode = "add")
        {
            using (SqlConnection mainCnn = new SqlConnection(_connectionString))
            {
                mainCnn.Open();

                if (taskCode == "edit") {

                    if (oldUser.Id == 0)
                        errorMsgDict["Id"] = "User does not exist";
                }
                
                if(string.IsNullOrEmpty(newUser.FirstName))
                    errorMsgDict["FirstName"] = "First Name is required";

                if (string.IsNullOrEmpty(newUser.LastName))
                    errorMsgDict["LastName"] = "Last Name is required";

                if (string.IsNullOrEmpty(newUser.Email))
                    errorMsgDict["Email"] = "Email is required";
                else {
                    var sqlCommand = new SqlCommand($"SELECT Id FROM [User] WHERE Email = '{newUser.Email}'", mainCnn);
                    int emailUserId = Convert.ToInt32(sqlCommand.ExecuteScalar());
                    
                    if (emailUserId > 0) {
                        if(oldUser == null || (oldUser.Id != emailUserId))
                            errorMsgDict["Email"] = "Email already registered";
                    }
                        
                }

                ValidateEmployment(newUser, ref errorMsgDict, taskCode, mainCnn);

                if(newUser.Address != null)
                    ValidateAddress(newUser, ref errorMsgDict, taskCode, mainCnn);

            }

            return errorMsgDict.Count == 0;
        }

        private void ValidateEmployment(User user, ref Dictionary<string, string> errorMsgDict, string taskCode, SqlConnection conn) {
            List<Employment> employments = user.Employments;

            for (int ctr = 0; ctr < employments.Count(); ctr++)
            {
                var employment = employments[ctr];

                if (taskCode == "edit" && employment.Id > 0) {
                    var sqlCommand = new SqlCommand($"SELECT Id FROM [Employment] WHERE Id = {employment.Id}", conn);

                    if (Convert.ToInt32(sqlCommand.ExecuteScalar()) < 1)
                        errorMsgDict[$"Employment[{ctr}]"] = "Employment does not exist";
                }

                if (employment.StartDate == null)
                    errorMsgDict[$"StartDate[{ctr}]"] = "Start Date is required";
                else
                {
                    if (employment.EndDate != null && (employment.StartDate > employment.EndDate))
                        errorMsgDict[$"StartDate[{ctr}]"] = "Start Date should be earlier than End Date";
                }

                if (string.IsNullOrEmpty(employment.Company))
                    errorMsgDict[$"Company[{ctr}]"] = "Company is required";

                if (employment.MonthsOfExperience == null)
                    errorMsgDict[$"MonthsOfExperience[{ctr}]"] = "Months Of Experience is required";

                if (employment.Salary == null)
                    errorMsgDict[$"Salary[{ctr}]"] = "Salary is required";
            }
        }

        private void ValidateAddress(User user, ref Dictionary<string, string> errorMsgDict, string taskCode, SqlConnection conn) {

            if (user.Address != null) {
                
                if (taskCode == "edit")
                {
                    Address currentAddress = GetAddressByUserId(user.Id, conn);

                    if (user.Address.Id > 0 && (currentAddress.Id != user.Address.Id))
                        errorMsgDict[$"Address"] = "Address does not exist";
                    else if (currentAddress.Id > 0 && (currentAddress.Id != user.Address.Id))
                        errorMsgDict[$"Address"] = "User already have an address";
                }

                if (string.IsNullOrEmpty(user.Address.Street))
                    errorMsgDict[$"Street"] = "Street is required";

                if (string.IsNullOrEmpty(user.Address.City))
                    errorMsgDict[$"City"] = "City is required";
            }
        }
    }
}
