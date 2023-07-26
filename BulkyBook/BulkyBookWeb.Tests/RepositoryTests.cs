using Bulky.DataAccess.Repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Tests
{
    public class RepositoryTests
    {
        private string GetRandomDatabaseName()
        {
            return "TestDb_" + Guid.NewGuid().ToString();
        }

        [Fact]
        public void Add_ValidEntity_EntityAddedToDatabase()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDBContext>()
                .UseInMemoryDatabase(databaseName: GetRandomDatabaseName())
                .Options;

            using (var dbContext = new ApplicationDBContext(options))
            {
                var repository = new Repository<Product>(dbContext);

                // Act
                var product = new Product
                {
                    Id = 1,
                    Title = "Sample Product",
                    Author = " Author",
                    Description = "Description",
                    ISBN = "1234",
                    Price = 19.99
                    // Add other properties
                };
                repository.Add(product);
                dbContext.SaveChanges();

                // Assert
                using (var dbContextAfterAdd = new ApplicationDBContext(options))
                {
                    var productFromDb = dbContextAfterAdd.Products.Find(1);
                    Assert.NotNull(productFromDb);
                    // Assert other properties are saved correctly
                }
            }
        }

        [Fact]
        public void GetAll_NoFilter_ReturnsAllEntities()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDBContext>()
                .UseInMemoryDatabase(databaseName: GetRandomDatabaseName())
                .Options;

            using (var dbContext = new ApplicationDBContext(options))
            {
                var repository = new Repository<Product>(dbContext);

                // Add test data to the in-memory database
                dbContext.Products.Add(new Product
                {
                    Id = 1,
                    Title = "Product 1",
                    Author = " Author",
                    Description = "Description",
                    ISBN = "1234",
                    // Add other properties
                });

                dbContext.Products.Add(new Product
                {
                    Id = 2,
                    Title = "Product 2",
                    Author = " Author 2",
                    Description = "Description 2",
                    ISBN = "5678",
                    // Add other properties
                });

                dbContext.SaveChanges();
            }

            using (var dbContext = new ApplicationDBContext(options))
            {
                var repository = new Repository<Product>(dbContext);

                // Act
                var products = repository.GetAll();

                // Assert
                Assert.NotNull(products);
                Assert.Equal(2, products.Count());
                // Assert other expectations for the returned entities
            }
        }

        [Fact]
        public void GetFirstOrDefault_WithFilter_ReturnsMatchingEntity()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDBContext>()
                .UseInMemoryDatabase(databaseName: GetRandomDatabaseName())
                .Options;

            using (var dbContext = new ApplicationDBContext(options))
            {
                var repository = new Repository<Product>(dbContext);

                // Add test data to the in-memory database
                dbContext.Products.Add(new Product
                {
                    Id = 1,
                    Title = "Product 1",
                    Author = " Author",
                    Description = "Description",
                    ISBN = "1234",
                    // Add other properties
                });

                dbContext.Products.Add(new Product
                {
                    Id = 2,
                    Title = "Product 2",
                    Author = " Author 2",
                    Description = "Description 2",
                    ISBN = "5678",
                    // Add other properties
                });

                dbContext.SaveChanges();
            }

            using (var dbContext = new ApplicationDBContext(options))
            {
                var repository = new Repository<Product>(dbContext);

                // Act
                var product = repository.GetFirstOrDefault(p => p.Title == "Product 2");

                // Assert
                Assert.NotNull(product);
                Assert.Equal(2, product.Id);
                // Assert other expectations for the matching entity
            }
        }

        [Fact]
        public void Remove_ValidEntity_EntityRemovedFromDatabase()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDBContext>()
                .UseInMemoryDatabase(databaseName: GetRandomDatabaseName())
                .Options;

            using (var dbContext = new ApplicationDBContext(options))
            {
                var repository = new Repository<Product>(dbContext);

                // Add test data to the in-memory database
                var product = new Product
                {
                    Id = 1,
                    Title = "Sample Product",
                    Author = " Author",
                    Description = "Description",
                    ISBN = "1234",
                    Price = 19.99
                    // Add other properties
                };
                dbContext.Products.Add(product);
                dbContext.SaveChanges();
            }

            using (var dbContext = new ApplicationDBContext(options))
            {
                var repository = new Repository<Product>(dbContext);

                // Act
                var product = dbContext.Products.Find(1);
                repository.Remove(product);
                dbContext.SaveChanges();

                // Assert
                using (var dbContextAfterRemove = new ApplicationDBContext(options))
                {
                    var productFromDb = dbContextAfterRemove.Products.Find(1);
                    Assert.Null(productFromDb);
                }
            }
        }

        [Fact]
        public void RemoveRange_EntitiesToRemove_EntitiesRemovedFromDatabase()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDBContext>()
                .UseInMemoryDatabase(databaseName: GetRandomDatabaseName())
                .Options;

            using (var dbContext = new ApplicationDBContext(options))
            {
                var repository = new Repository<Product>(dbContext);

                // Add test data to the in-memory database
                var products = new List<Product>
                {
                    new Product
                    {
                        Id = 1,
                        Title = "Product 1",
                        Author = "Author",
                        Description = "Description",
                        ISBN = "1234",
                        // Add other properties
                    },
                    new Product
                    {
                        Id = 2,
                        Title = "Product 2",
                        Author = " Author 2",
                        Description = "Description 2",
                        ISBN = "5678",
                        // Add other properties
                    }
                };
                dbContext.Products.AddRange(products);
                dbContext.SaveChanges();
            }

            using (var dbContext = new ApplicationDBContext(options))
            {
                var repository = new Repository<Product>(dbContext);

                // Act
                var products = dbContext.Products.ToList();
                repository.RemoveRange(products);
                dbContext.SaveChanges();

                // Assert
                using (var dbContextAfterRemove = new ApplicationDBContext(options))
                {
                    var productsFromDb = dbContextAfterRemove.Products.ToList();
                    Assert.Empty(productsFromDb);
                }
            }
        }

        // You can add similar tests for Update, including testing for tracked and untracked entities.
    }
}
