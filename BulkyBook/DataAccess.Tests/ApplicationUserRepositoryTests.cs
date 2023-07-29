using Bulky.DataAccess.Repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Tests
{
    public class ApplicationUserRepositoryTests
    {
        // This method tests the Create method of ApplicationUserRepository.
        [Fact]
        public void Create_ValidUser_UserAddedToDatabase()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDBContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;

            var newUser = new ApplicationUser
            {
                Name = "John Doe",
                UserName = "johndoe@example.com",
                Email = "johndoe@example.com",
                StreetAddress = "123 Main St",
                City = "New York",
                State = "NY",
                PostalCode = "10001",
                CompanyId = 1,
                Role = "User"
            };

            // Act
            using (var dbContext = new ApplicationDBContext(options))
            {
                var userRepository = new ApplicationUserRepository(dbContext);
                userRepository.Add(newUser);
                dbContext.SaveChanges();
            }

            // Assert
            using (var dbContext = new ApplicationDBContext(options))
            {
                var addedUser = dbContext.ApplicationUsers.FirstOrDefault(u => u.Name == newUser.Name);
                Assert.NotNull(addedUser);
                Assert.Equal(newUser.Name, addedUser.Name);
                Assert.Equal(newUser.UserName, addedUser.UserName);
                Assert.Equal(newUser.Email, addedUser.Email);
                // Add more assertions for other properties
            }
        }
    }
}
