using Bulky.DataAccess.Repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Tests
{
    public class OrderHeaderRepositoryTests
    {
        private string GetRandomDatabaseName()
        {
            return "TestDb_" + Guid.NewGuid().ToString();
        }

        [Fact]
        public void Update_ValidOrderHeader_OrderHeaderUpdated()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDBContext>()
                .UseInMemoryDatabase(databaseName: GetRandomDatabaseName())
                .Options;

            using (var dbContext = new ApplicationDBContext(options))
            {
                // Add test data to the in-memory database
                var orderHeader = new OrderHeader
                {
                    Id = 1,
                    ApplicationUserId = "user_id",
                    OrderDate = DateTime.Now,
                    ShippingDate = DateTime.Now,
                    OrderTotal = 100.0,
                    OrderStatus = "Pending",
                    PaymentStatus = "Unpaid",
                    PhoneNumber = "1234567890",
                    StreetAddress = "123 Main St",
                    City = "New York",
                    State = "NY",
                    PostalCode = "10001",
                    Name = "John Doe"
                };

                dbContext.OrderHeaders.Add(orderHeader);
                dbContext.SaveChanges();
            }

            using (var dbContext = new ApplicationDBContext(options))
            {
                var orderHeaderRepository = new OrderHeaderRepository(dbContext);

                // Act
                var updatedOrderHeader = new OrderHeader
                {
                    Id = 1,
                    // Update other properties
                    OrderStatus = "Shipped",
                    PaymentStatus = "Paid"
                };

                orderHeaderRepository.Update(updatedOrderHeader);
                dbContext.SaveChanges();

                // Assert
                using (var dbContextAfterUpdate = new ApplicationDBContext(options))
                {
                    var orderHeaderFromDb = dbContextAfterUpdate.OrderHeaders.Find(1);
                    Assert.NotNull(orderHeaderFromDb);
                    Assert.Equal("Shipped", orderHeaderFromDb.OrderStatus);
                    Assert.Equal("Paid", orderHeaderFromDb.PaymentStatus);
                    // Assert other properties are updated correctly
                }
            }
        }

        [Fact]
        public void UpdateStatus_ValidData_StatusUpdated()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDBContext>()
                .UseInMemoryDatabase(databaseName: GetRandomDatabaseName())
                .Options;

            using (var dbContext = new ApplicationDBContext(options))
            {
                // Add test data to the in-memory database
                var orderHeader = new OrderHeader
                {
                    Id = 1,
                    ApplicationUserId = "user_id",
                    OrderDate = DateTime.Now,
                    ShippingDate = DateTime.Now,
                    OrderTotal = 100.0,
                    OrderStatus = "Pending",
                    PaymentStatus = "Unpaid",
                    PhoneNumber = "1234567890",
                    StreetAddress = "123 Main St",
                    City = "New York",
                    State = "NY",
                    PostalCode = "10001",
                    Name = "John Doe"
                };

                dbContext.OrderHeaders.Add(orderHeader);
                dbContext.SaveChanges();
            }

            using (var dbContext = new ApplicationDBContext(options))
            {
                var orderHeaderRepository = new OrderHeaderRepository(dbContext);

                // Act
                orderHeaderRepository.UpdateStatus(1, "Shipped", "Paid");
                dbContext.SaveChanges();

                // Assert
                using (var dbContextAfterUpdate = new ApplicationDBContext(options))
                {
                    var orderHeaderFromDb = dbContextAfterUpdate.OrderHeaders.Find(1);
                    Assert.NotNull(orderHeaderFromDb);
                    Assert.Equal("Shipped", orderHeaderFromDb.OrderStatus);
                    Assert.Equal("Paid", orderHeaderFromDb.PaymentStatus);
                    // Assert other properties remain unchanged
                }
            }
        }

        [Fact]
        public void UpdateStripePaymentId_ValidData_PaymentIdUpdated()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDBContext>()
                .UseInMemoryDatabase(databaseName: GetRandomDatabaseName())
                .Options;

            using (var dbContext = new ApplicationDBContext(options))
            {
                // Add test data to the in-memory database
                var orderHeader = new OrderHeader
                {
                    Id = 1,
                    ApplicationUserId = "user_id",
                    OrderDate = DateTime.Now,
                    ShippingDate = DateTime.Now,
                    OrderTotal = 100.0,
                    OrderStatus = "Pending",
                    PaymentStatus = "Unpaid",
                    PhoneNumber = "1234567890",
                    StreetAddress = "123 Main St",
                    City = "New York",
                    State = "NY",
                    PostalCode = "10001",
                    Name = "John Doe",
                    SessionId = "session_id"
                };

                dbContext.OrderHeaders.Add(orderHeader);
                dbContext.SaveChanges();
            }

            using (var dbContext = new ApplicationDBContext(options))
            {
                var orderHeaderRepository = new OrderHeaderRepository(dbContext);

                // Act
                orderHeaderRepository.UpdateStripePaymentId(1, "updated_session_id", "payment_intent_id");
                dbContext.SaveChanges();

                // Assert
                using (var dbContextAfterUpdate = new ApplicationDBContext(options))
                {
                    var orderHeaderFromDb = dbContextAfterUpdate.OrderHeaders.Find(1);
                    Assert.NotNull(orderHeaderFromDb);
                    Assert.Equal("updated_session_id", orderHeaderFromDb.SessionId);
                    Assert.Equal("payment_intent_id", orderHeaderFromDb.PaymentIntentId);
                    // Assert other properties remain unchanged
                }
            }
        }
    }
}
