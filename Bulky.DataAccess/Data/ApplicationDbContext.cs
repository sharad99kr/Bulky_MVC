﻿using Microsoft.EntityFrameworkCore;
using Bulky.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace Bulky.DataAccess.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {//use nuget package manager to install packages related to IdentityDbContext. This package will break existing code at places which needs to be fixed after adding it
     //This helps in using default Identity management provided by .NetCore
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {
            
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }

        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<OrderHeader> OrderHeaders { get; set; }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Action", DisplayOrder = 1 },
                new Category { Id = 2, Name = "SciFi", DisplayOrder = 2 },
                new Category { Id = 3, Name = "History", DisplayOrder = 3 }
                );

            modelBuilder.Entity<Company>().HasData(
                new Company { Id = 1, Name = "Tech Soln", StreetAddress = "123 tech street", City="Tech City",PostalCode="121121",State="FL",PhoneNumber="1231231234" },
                new Company { Id = 2, Name = "BI Soln", StreetAddress = "456 tech street", City = "Tech City", PostalCode = "121122", State = "MX", PhoneNumber = "1231231234" },
                new Company { Id = 3, Name = "Data Soln", StreetAddress = "789 tech street", City = "Tech City", PostalCode = "121123", State = "OH", PhoneNumber = "1231231234" }
                );

            modelBuilder.Entity<Product>().HasData(
                    new Product {
                                    Id = 1,
                                    Title = "Fortune of Time",
                                    Author = "Billy Spark",
                                    Description = "Praesent vitae sodales libero. Praesent molestie orci augue, vitae euismod velit sollicitudin ac. Praesent vestibulum facilisis nibh ut ultricies.\r\n\r\nNunc malesuada viverra ipsum sit amet tincidunt. ",
                                    ISBN = "SWD9999001",
                                    ListPrice = 99,
                                    Price = 90,
                                    Price50 = 85,
                                    Price100 = 80,
                                    CategoryId = 1,
                                    ImageUrl = ""
                                },
                    new Product {
                        Id = 2,
                        Title = "Dark Skies",
                        Author = "Nancy Hoover",
                        Description = "Praesent vitae sodales libero. Praesent molestie orci augue, vitae euismod velit sollicitudin ac. Praesent vestibulum facilisis nibh ut ultricies.\r\n\r\nNunc malesuada viverra ipsum sit amet tincidunt. ",
                        ISBN = "CAW777777701",
                        ListPrice = 40,
                        Price = 30,
                        Price50 = 25,
                        Price100 = 20,
					    CategoryId = 2,
						ImageUrl = ""
					},
                    new Product {
                        Id = 3,
                        Title = "Vanish in the Sunset",
                        Author = "Julian Button",
                        Description = "Praesent vitae sodales libero. Praesent molestie orci augue, vitae euismod velit sollicitudin ac. Praesent vestibulum facilisis nibh ut ultricies.\r\n\r\nNunc malesuada viverra ipsum sit amet tincidunt. ",
                        ISBN = "RITO5555501",
                        ListPrice = 55,
                        Price = 50,
                        Price50 = 40,
                        Price100 = 35,
					    CategoryId = 3,
						ImageUrl = ""
					},
                    new Product {
                        Id = 4,
                        Title = "Cotton Candy",
                        Author = "Abby Muscles",
                        Description = "Praesent vitae sodales libero. Praesent molestie orci augue, vitae euismod velit sollicitudin ac. Praesent vestibulum facilisis nibh ut ultricies.\r\n\r\nNunc malesuada viverra ipsum sit amet tincidunt. ",
                        ISBN = "WS3333333301",
                        ListPrice = 70,
                        Price = 65,
                        Price50 = 60,
                        Price100 = 55,
					    CategoryId = 1,
						ImageUrl = ""
					},
                    new Product {
                        Id = 5,
                        Title = "Rock in the Ocean",
                        Author = "Ron Parker",
                        Description = "Praesent vitae sodales libero. Praesent molestie orci augue, vitae euismod velit sollicitudin ac. Praesent vestibulum facilisis nibh ut ultricies.\r\n\r\nNunc malesuada viverra ipsum sit amet tincidunt. ",
                        ISBN = "SOTJ1111111101",
                        ListPrice = 30,
                        Price = 27,
                        Price50 = 25,
                        Price100 = 20,
					    CategoryId = 2,
						ImageUrl = ""
					},
                    new Product {
                        Id = 6,
                        Title = "Leaves and Wonders",
                        Author = "Laura Phantom",
                        Description = "Praesent vitae sodales libero. Praesent molestie orci augue, vitae euismod velit sollicitudin ac. Praesent vestibulum facilisis nibh ut ultricies.\r\n\r\nNunc malesuada viverra ipsum sit amet tincidunt. ",
                        ISBN = "FOT000000001",
                        ListPrice = 25,
                        Price = 23,
                        Price50 = 22,
                        Price100 = 20,
					    CategoryId = 3,
						ImageUrl = ""
					}
                );
        }
    }
}
