﻿using Bulky.DataAccess.Data;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.DbInitializer
{
    public class DbInitializer : IDbInitializer
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db; 
        public DbInitializer(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext db) {
            _roleManager = roleManager;
            _userManager = userManager;
            _db = db;
        }
        //method responsible for creating admin user and roles for the website
        public void Initialize() {
            //migrations if they are not applied
            try {
                if(_db.Database.GetPendingMigrations().Count() > 0) {
                    _db.Database.Migrate();
                }
            } catch(Exception ex) { 
            
            }
            //create roles if they are not created
            if(!_roleManager.RoleExistsAsync(SD.Role_Customer).GetAwaiter().GetResult()) {
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Customer)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Employee)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Company)).GetAwaiter().GetResult();

                //create  users as well
                _userManager.CreateAsync(new ApplicationUser {
                    UserName= "admin@sharad.com",
                    Email= "admin@sharad.com",
                    Name ="Sharad Kumar",
                    PhoneNumber="1231231234",
                    StreetAddress="5th Avenue",
                    State="NY",
                    PostalCode="123321",
                    City="NYC"
                }, "Abc!123").GetAwaiter().GetResult();

                //fetch the newly created user from db and assign admin role 
                ApplicationUser user=_db.ApplicationUsers.FirstOrDefault(u=>u.Email== "admin@sharad.com");
                _userManager.AddToRoleAsync(user,SD.Role_Admin).GetAwaiter().GetResult() ;
            }
            return;
        }
    }
}
