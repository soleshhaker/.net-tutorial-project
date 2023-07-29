using Bulky.DataAccess.Repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Tests
{
    public class ShoppingCartRepositoryTests
    {
        private string GetRandomDatabaseName()
        {
            // Generate a random suffix for the database name using a Guid
            return "TestDb_" + Guid.NewGuid().ToString();
        }
        [Fact]
        public void Create_ValidShoppingCart_ShoppingCartAddedToDatabase()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDBContext>()
                .UseInMemoryDatabase(databaseName: GetRandomDatabaseName())
                .Options;

            using (var dbContext = new ApplicationDBContext(options))
            {
                // Add test data to the in-memory database
                var product = new Product
                {
                    Id = 1,
                    Title = "Sample Product",
                    Author = " Author",
                    Description = "Description",
                    ISBN = "1234",
                    // Set other properties
                };

                // Set a valid price for the product
                product.Price = 19.99;

                dbContext.Products.Add(product);
                dbContext.SaveChanges();
            }

            using (var dbContext = new ApplicationDBContext(options))
            {
                var shoppingCartRepository = new ShoppingCartRepository(dbContext);

                // Act
                var shoppingCart = new ShoppingCart
                {
                    ProductId = 1,
                    Count = 2,
                    ApplicationUserId = "123",
                    // Add other properties
                };

                shoppingCartRepository.Add(shoppingCart);
                dbContext.SaveChanges();

                // Assert
                using (var dbContextAfterAdd = new ApplicationDBContext(options))
                {
                    var shoppingCartFromDb = dbContextAfterAdd.ShoppingCarts
                        .Include(sc => sc.Product)
                        .FirstOrDefault();

                    Assert.NotNull(shoppingCartFromDb);
                    Assert.Equal(2, shoppingCartFromDb.Count);

                    // Additional checks for the Product property
                    Assert.NotNull(shoppingCartFromDb.Product);
                    Assert.Equal(1, shoppingCartFromDb.ProductId); // Just to ensure it is associated correctly
                    Assert.Equal(19.99, shoppingCartFromDb.Product.Price);
                }
            }
        }
    }
}
