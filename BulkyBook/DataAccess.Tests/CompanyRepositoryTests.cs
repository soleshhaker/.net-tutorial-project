using AutoBogus;
using Bogus;
using Bulky.DataAccess.Repository;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

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

            using (var dbContext = new ApplicationDBContext(options))
            {
                var faker = new AutoFaker<Company>();

                // Add the initial company using fake data
                var initialCompany = faker.Generate();
                var companyRepository = new CompanyRepository(dbContext);
                companyRepository.Add(initialCompany);
                dbContext.SaveChanges();
            }

            // Act
            using (var dbContext = new ApplicationDBContext(options))
            {
                var companyRepository = new CompanyRepository(dbContext);

                // Fetch the company from the repository
                var companyToUpdate = companyRepository
             .GetAll().First();
                companyToUpdate.Name = "Updated Company";
                companyToUpdate.StreetAddress = "Updated Street";
                // Modify other properties as needed

                companyRepository.Update(companyToUpdate);
                dbContext.SaveChanges();
            }

            // Assert
            using (var dbContext = new ApplicationDBContext(options))
            {
                var companyFromDb = dbContext.Companies.First();

                companyFromDb.Should().NotBeNull();
                companyFromDb.Name.Should().Be("Updated Company");
                companyFromDb.StreetAddress.Should().Be("Updated Street");
                // Add more assertions for other properties
            }
        }

    }
}
