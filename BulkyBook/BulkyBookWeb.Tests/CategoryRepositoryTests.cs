using Bulky.DataAccess.Repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Tests
{
    public class CategoryRepositoryTests
    {
        [Fact]
        public void Update_CategoryExists_CategoryUpdated()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDBContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;

            // Create an instance of the in-memory database context
            using (var dbContext = new ApplicationDBContext(options))
            {
                // Add test data to the in-memory database
                dbContext.Categories.Add(new Category
                {
                    Id = 1,
                    Name = "Sample Category",
                    DisplayOrder = 1,
                    CreatedDateTime = DateTime.Now
                });
                dbContext.SaveChanges();
            }

            // Create the CategoryRepository with the in-memory database context
            using (var dbContext = new ApplicationDBContext(options))
            {
                var categoryRepository = new CategoryRepository(dbContext);

                // Act
                var updatedCategory = new Category
                {
                    Id = 1,
                    Name = "Updated Category",
                    DisplayOrder = 2,
                    CreatedDateTime = DateTime.Now
                };
                categoryRepository.Update(updatedCategory);
                dbContext.SaveChanges();

                // Assert
                using (var dbContextAfterUpdate = new ApplicationDBContext(options))
                {
                    var categoryFromDb = dbContextAfterUpdate.Categories.Find(1);
                    Assert.NotNull(categoryFromDb);
                    Assert.Equal("Updated Category", categoryFromDb.Name);
                    Assert.Equal(2, categoryFromDb.DisplayOrder);
                    // Add more assertions for other properties
                }
            }
        }
    }
}
