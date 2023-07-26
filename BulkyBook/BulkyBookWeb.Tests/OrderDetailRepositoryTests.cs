using Bulky.DataAccess.Repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Tests
{
    public class OrderDetailRepositoryTests
    {
        // This method tests the Create method of OrderDetailRepository.
        [Fact]
        public void Create_ValidOrderDetail_OrderDetailAddedToDatabase()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDBContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;

            var newOrderDetail = new OrderDetail
            {
                OrderHeaderId = 1,
                ProductId = 2,
                Count = 3,
                Price = 10.99
            };

            // Act
            using (var dbContext = new ApplicationDBContext(options))
            {
                var orderDetailRepository = new OrderDetailRepository(dbContext);
                orderDetailRepository.Add(newOrderDetail);
                dbContext.SaveChanges();
            }

            // Assert
            using (var dbContext = new ApplicationDBContext(options))
            {
                var addedOrderDetail = dbContext.OrderDetails.FirstOrDefault(od => od.Id == newOrderDetail.Id);
                Assert.NotNull(addedOrderDetail);
                Assert.Equal(newOrderDetail.OrderHeaderId, addedOrderDetail.OrderHeaderId);
                Assert.Equal(newOrderDetail.ProductId, addedOrderDetail.ProductId);
                Assert.Equal(newOrderDetail.Count, addedOrderDetail.Count);
                Assert.Equal(newOrderDetail.Price, addedOrderDetail.Price);
                // Add more assertions for other properties
            }
        }
    }

}
