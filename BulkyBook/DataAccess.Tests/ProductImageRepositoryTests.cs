using Bulky.DataAccess.Repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Tests
{
    public class ProductImageRepositoryTests
    {
        private DbContextOptions<ApplicationDBContext> GetOptions(string databaseName)
        {
            return new DbContextOptionsBuilder<ApplicationDBContext>()
                .UseInMemoryDatabase(databaseName: databaseName)
                .Options;
        }

        [Fact]
        public void Update_ProductImageExists_ProductImageUpdated()
        {
            // Arrange
            var options = GetOptions("Update_ProductImageExists_ProductImageUpdated");

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
                });

                dbContext.ProductImages.Add(new ProductImage
                {
                    Id = 1,
                    ImageUrl = "SampleImageUrl",
                    ProductId = 1
                });
                dbContext.SaveChanges();
            }

            // Create the ProductImageRepository with the in-memory database context
            using (var dbContext = new ApplicationDBContext(options))
            {
                var productImageRepository = new ProductImageRepository(dbContext);

                // Act
                var updatedProductImage = new ProductImage
                {
                    Id = 1,
                    ImageUrl = "UpdatedImageUrl",
                    ProductId = 1
                };
                productImageRepository.Update(updatedProductImage);
                dbContext.SaveChanges();

                // Assert
                using (var dbContextAfterUpdate = new ApplicationDBContext(options))
                {
                    var productImageFromDb = dbContextAfterUpdate.ProductImages.Find(1);
                    Assert.NotNull(productImageFromDb);
                    Assert.Equal("UpdatedImageUrl", productImageFromDb.ImageUrl);
                    // Add more assertions for other properties
                }
            }
        }
    }
}
