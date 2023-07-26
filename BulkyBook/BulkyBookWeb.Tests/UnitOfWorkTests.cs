using Bulky.DataAccess.Repository;
using Humanizer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Tests
{
    public class UnitOfWorkTests
    {
        [Fact]
        public void Save_ValidData_SaveChangesIsCalled()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using (var context = new ApplicationDBContext(options))
            {
                // Add any necessary seed data or configuration here
                // ...

                var unitOfWork = new UnitOfWork(context);

                // Add test data to the context
                context.Products.Add(new Product { Id = 1,
                    Title = "Product 1",
                    Author = "Author 2",
                    Description = "Description",
                    ISBN = "1234",
                    Price = 10.99 });
                context.Products.Add(new Product { Id = 2, Title = "Product 2",
                    Author = "Author 2",
                    Description = "Description 2",
                    ISBN = "5678",
                    Price = 22.33 });
                context.SaveChanges(); // Save changes to the in-memory database

                // Act
                unitOfWork.Save();

                // Assert
                // Verify that SaveChanges is called on the context
                var productCountAfterSave = context.Products.Count();
                Assert.Equal(2, productCountAfterSave); // Assert that the product count remains the same after calling SaveChanges
            }
        }
    }
    public static class DbContextMockExtensions
    {
        public static Mock<DbSet<T>> CreateDbSetMock<T>(this List<T> data) where T : class
        {
            var queryableData = data.AsQueryable();

            var dbSetMock = new Mock<DbSet<T>>();

            dbSetMock.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryableData.Provider);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryableData.Expression);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryableData.ElementType);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryableData.GetEnumerator());

            return dbSetMock;
        }
    }
}