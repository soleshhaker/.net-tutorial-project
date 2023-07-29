using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using BulkyBookWeb.Areas.Admin.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBookWeb.Tests
{
    public class UserControllerTests
    {
        [Fact]
        public void Details_ReturnsViewResult_WithUserViewModel()
        {
            // arrange
            var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(databaseName: "TestDb")
            .Options;

            using (var dbContext = new ApplicationDBContext(options))
            {
                var unitOfWork = new UnitOfWork(dbContext);
                dbContext.ApplicationUsers.Add(new ApplicationUser { Id = "testId", Name = "testName", Role = SD.Role_Customer });
                dbContext.SaveChanges();
                // mock the UserManager
                var userStoreMock = new Mock<IUserStore<IdentityUser>>();

                IEnumerable<IUserValidator<IdentityUser>> userValidators = new List<IUserValidator<IdentityUser>>();
                IEnumerable<IPasswordValidator<IdentityUser>> passwordValidators = new List<IPasswordValidator<IdentityUser>>();

                List<IdentityUser> _users = new List<IdentityUser>
                 {
                      new IdentityUser() { Id = "testId" },
                      new IdentityUser() { Id = "2" }
                 };

                var _userManager = MockUserManager<IdentityUser>(_users).Object;
                // mock the RoleManager

                var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();
                var roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                    roleStoreMock.Object,
                    new IRoleValidator<IdentityRole>[0],
                    new Mock<ILookupNormalizer>().Object,
                    new Mock<IdentityErrorDescriber>().Object,
                    new Mock<ILogger<RoleManager<IdentityRole>>>().Object);

                var controller = new UserController(unitOfWork, _userManager, roleManagerMock.Object);
                _userManager.AddToRoleAsync(_userManager.Users.Where(x => x.Id == "testId").First(), SD.Role_Customer).GetAwaiter().GetResult();
                // Act
                var result = controller.Details("testId");

                // Assert
                var viewResult = Assert.IsType<ViewResult>(result);
                var model = Assert.IsType<UserViewModel>(viewResult.ViewData.Model);
                var roles = _userManager.GetRolesAsync(_userManager.Users.First(x => x.Id == "testId")).Result;
                Assert.Contains(SD.Role_Customer, roles);
            }
        }
        [Fact]
        public void DetailsPOST_Updates_Role()
        {
            var options = new DbContextOptionsBuilder<ApplicationDBContext>()
           .UseInMemoryDatabase(databaseName: "TestDb")
           .Options;

            using (var dbContext = new ApplicationDBContext(options))
            {
                var unitOfWork = new Mock<IUnitOfWork>();
                dbContext.ApplicationUsers.Add(new ApplicationUser { Id = "testId2", Name = "testName", Role = SD.Role_Customer });
                dbContext.SaveChanges();
                // mock the UserManager
                var userStoreMock = new Mock<IUserStore<IdentityUser>>();

                IEnumerable<IUserValidator<IdentityUser>> userValidators = new List<IUserValidator<IdentityUser>>();
                IEnumerable<IPasswordValidator<IdentityUser>> passwordValidators = new List<IPasswordValidator<IdentityUser>>();

                List<IdentityUser> _users = new List<IdentityUser>
                 {
                      new IdentityUser() { Id = "testId" },
                      new IdentityUser() { Id = "2" }
                 };

                var _userManager = MockUserManager<IdentityUser>(_users);
                // mock the RoleManager

                var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();
                var roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                    roleStoreMock.Object,
                    new IRoleValidator<IdentityRole>[0],
                    new Mock<ILookupNormalizer>().Object,
                    new Mock<IdentityErrorDescriber>().Object,
                    new Mock<ILogger<RoleManager<IdentityRole>>>().Object);
                var controller = new UserController(unitOfWork.Object, _userManager.Object, roleManagerMock.Object);

                // Arrange
                var oldRole = SD.Role_Customer;
                var newRole = SD.Role_Company;
                var oldCompanyId = 1;
                var newCompanyId = 2;


                var applicationUserFromDb = new ApplicationUser
                {
                    Id = "testId2",
                    Role = oldRole,
                    CompanyId = oldCompanyId
                };
                var userViewModel = new UserViewModel
                {
                    ApplicationUser = new ApplicationUser
                    {
                        Id = "testId2",
                        Role = newRole,
                        CompanyId = newCompanyId
                    }
                };
                var mockApplicationUserRepository = new Mock<IApplicationUserRepository>();
                mockApplicationUserRepository.Setup(u => u.GetFirstOrDefault(
                    It.IsAny<Expression<Func<ApplicationUser, bool>>>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>()
                )).Returns(applicationUserFromDb);

                unitOfWork.Setup(u => u.ApplicationUser).Returns(mockApplicationUserRepository.Object);

                _userManager.Setup(u => u.GetRolesAsync(It.IsAny<ApplicationUser>()))
                    .ReturnsAsync(new List<string> { oldRole });

                controller.UserViewModel = userViewModel;

                // Act
                var result = controller.Details_POST();

                // Assert
                Assert.Equal(newRole, applicationUserFromDb.Role);
                Assert.Equal(newCompanyId, applicationUserFromDb.CompanyId);
                unitOfWork.Verify(u => u.ApplicationUser.Update(applicationUserFromDb), Times.Once);
                unitOfWork.Verify(u => u.Save(), Times.Once);
            }
        }
        public static Mock<UserManager<TUser>> MockUserManager<TUser>(List<TUser> ls) where TUser : IdentityUser
        {
            var store = new Mock<IUserStore<TUser>>();
            var mgr = new Mock<UserManager<TUser>>(store.Object, null, null, null, null, null, null, null, null);
            mgr.Object.UserValidators.Add(new UserValidator<TUser>());
            mgr.Object.PasswordValidators.Add(new PasswordValidator<TUser>());

            mgr.Setup(u => u.Users).Returns(ls.AsQueryable());
            mgr.Setup(m => m.AddToRoleAsync(It.IsAny<TUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
            var userRoles = new Dictionary<string, string>
            {
                { "testId", "Customer" },
                // Add other users and their roles here
            };
            mgr.Setup(m => m.GetRolesAsync(It.IsAny<TUser>()))
             .ReturnsAsync((TUser user) => new List<string> { userRoles[user.Id] });


            return mgr;
        }
    }
}
