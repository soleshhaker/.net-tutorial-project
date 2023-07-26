using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository
{
    public class ApplicationUserRepository : Repository<ApplicationUser>, IApplicationUserRepository
    {
        private ApplicationDBContext _db;
        public ApplicationUserRepository(ApplicationDBContext db) : base(db)
        {
            _db = db;
        }

        public void Update(ApplicationUser obj)
        {
            _db.ApplicationUsers.Update(obj);
        }
        public void UpdateRoles(ApplicationUser applicationUser, string newRole, string oldRole)
        {
            // Remove the old role
            if (!string.IsNullOrEmpty(oldRole))
            {
                var removeResult = _db.UserRoles.Remove(new IdentityUserRole<string>
                {
                    UserId = applicationUser.Id,
                    RoleId = _db.Roles.FirstOrDefault(r => r.Name == oldRole)?.Id
                });
            }

            // Add the new role
            if (!string.IsNullOrEmpty(newRole))
            {
                var addResult = _db.UserRoles.Add(new IdentityUserRole<string>
                {
                    UserId = applicationUser.Id,
                    RoleId = _db.Roles.FirstOrDefault(r => r.Name == newRole)?.Id
                });
            }

            // Save the changes to the context
            _db.SaveChanges();
        }
    }
}
