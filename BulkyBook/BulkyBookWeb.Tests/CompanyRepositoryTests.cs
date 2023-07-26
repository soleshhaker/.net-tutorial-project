using Bulky.DataAccess.Repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Tests
{
    public class CompanyRepositoryTests
    {
        [Fact]
        public void Update_CompanyExists_CompanyUpdated()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDBContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;

            // Create an instance of the in-memory database context
            using (var dbContext = new ApplicationDBContext(options))
            {
                // Add test data to the in-memory database
                dbContext.Companies.Add(new Company
                {
                    Id = 1,
                    Name = "Sample Company",
                    StreetAddress = "Sample Street",
                    City = "Sample City",
                    State = "Sample State",
                    PostalCode = "12345",
                    PhoneNumber = "123-456-7890"
                });
                dbContext.SaveChanges();
            }

            // Create the CompanyRepository with the in-memory database context
            using (var dbContext = new ApplicationDBContext(options))
            {
                var companyRepository = new CompanyRepository(dbContext);

                // Act
                var updatedCompany = new Company
                {
                    Id = 1,
                    Name = "Updated Company",
                    StreetAddress = "Updated Street",
                    City = "Updated City",
                    State = "Updated State",
                    PostalCode = "98765",
                    PhoneNumber = "987-654-3210"
                };
                companyRepository.Update(updatedCompany);
                dbContext.SaveChanges();

                // Assert
                using (var dbContextAfterUpdate = new ApplicationDBContext(options))
                {
                    var companyFromDb = dbContextAfterUpdate.Companies.Find(1);
                    Assert.NotNull(companyFromDb);
                    Assert.Equal("Updated Company", companyFromDb.Name);
                    Assert.Equal("Updated Street", companyFromDb.StreetAddress);
                    // Add more assertions for other properties
                }
            }
        }
    }
}
