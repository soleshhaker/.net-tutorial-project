using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository;
using Bulky.Models;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Tests
{
    public class ProductRepositoryTests
    {
        private string GetRandomDatabaseName()
        {
            // Generate a random suffix for the database name using a Guid
            return "TestDb_" + Guid.NewGuid().ToString();
        }

        // This method tests the Update method of ProductRepository.
        [Fact]
        public void Update_ProductExists_ProductUpdated()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDBContext>()
             .UseInMemoryDatabase(databaseName: GetRandomDatabaseName()) // Use the random database name
             .Options;


            // Create an instance of the in-memory database context
            using (var dbContext = new ApplicationDBContext(options))
            {
                // Add test data to the in-memory database
                dbContext.Products.Add(new Product
                {
                    Id = 1,
                    Title = "Sample Product",
                    Author = " Author",
                    Description = "Description",
                    ISBN = "1234"
                    // Add other properties
                });
                dbContext.SaveChanges();
            }

            // Create the ProductRepository with the in-memory database context
            using (var dbContext = new ApplicationDBContext(options))
            {
                var productRepository = new ProductRepository(dbContext);

                // Act
                var updatedProduct = new Product
                {
                    Id = 1,
                    Title = "Updated Title",
                    Author = "Updated Author",
                    Description = "Updated Description",
                    ISBN = "81324124"
                    // Add other properties
                };
                productRepository.Update(updatedProduct);
                dbContext.SaveChanges();
                // Assert
                using (var dbContextAfterUpdate = new ApplicationDBContext(options))
                {
                    var productFromDb = dbContextAfterUpdate.Products.Find(1);
                    Assert.NotNull(productFromDb);
                    Assert.Equal("Updated Title", productFromDb.Title);
                    Assert.Equal("Updated Author", productFromDb.Author);
                    Assert.Equal("Updated Description", productFromDb.Description);
                    Assert.Equal("81324124", productFromDb.ISBN);

                    // Add more assertions for other properties
                }
            }
        }
    }

}
