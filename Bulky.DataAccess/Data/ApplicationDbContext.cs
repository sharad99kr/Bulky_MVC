using Microsoft.EntityFrameworkCore;
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
		public DbSet<ProductImage> ProductImages { get; set; }
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

            modelBuilder.Entity<Product>()
            .Property(p => p.SearchEmbeddingData)
            .HasColumnType("varbinary(max)")
            .HasColumnName("SearchEmbedding");

            modelBuilder.Entity<Product>().HasData(
                    new Product {
                        Id = 1,
                        Title = "Fortune of Time",
                        Author = "Billy Spark",
                        Description = "A sweeping time-travel adventure that spans ancient civilizations " +
                                      "and modern dilemmas. Perfect for readers who love historical fiction " +
                                      "with philosophical depth, fast-paced plots, and questions about fate, " +
                                      "free will, and the consequences of changing the past.",
                        ISBN = "SWD9999001",
                        ListPrice = 99,
                        Price = 90,
                        Price50 = 85,
                        Price100 = 80,
                        CategoryId = 1,
                    },
                    new Product {
                        Id = 2,
                        Title = "Dark Skies",
                        Author = "Nancy Hoover",
                        Description = "A gripping psychological thriller set in a remote mountain town where " +
                                      "a detective unravels a decades-old conspiracy. Dark, atmospheric, and " +
                                      "relentlessly tense — ideal for fans of crime fiction, murder mystery, " +
                                      "and slow-burn suspense that keeps you guessing until the final page.",
                        ISBN = "CAW777777701",
                        ListPrice = 40,
                        Price = 30,
                        Price50 = 25,
                        Price100 = 20,
                        CategoryId = 2,
                    },
                    new Product {
                        Id = 3,
                        Title = "Vanish in the Sunset",
                        Author = "Julian Button",
                        Description = "A heartwarming romance set against the backdrop of a small coastal " +
                                      "village in summer. Two strangers with complicated pasts find unexpected " +
                                      "connection. A cozy, emotional read perfect for a relaxing weekend — " +
                                      "uplifting, character-driven, and quietly unforgettable.",
                        ISBN = "RITO5555501",
                        ListPrice = 55,
                        Price = 50,
                        Price50 = 40,
                        Price100 = 35,
                        CategoryId = 3,
                    },
                    new Product {
                        Id = 4,
                        Title = "Cotton Candy",
                        Author = "Abby Muscles",
                        Description = "A lighthearted coming-of-age story following a teenager navigating " +
                                      "friendship, first love, and family secrets during one unforgettable " +
                                      "summer at a travelling carnival. Funny, tender, and full of heart — " +
                                      "a feel-good read suitable for young adults and anyone who loves " +
                                      "nostalgic, optimistic fiction.",
                        ISBN = "WS3333333301",
                        ListPrice = 70,
                        Price = 65,
                        Price50 = 60,
                        Price100 = 55,
                        CategoryId = 1,
                    },
                    new Product {
                        Id = 5,
                        Title = "Rock in the Ocean",
                        Author = "Ron Parker",
                        Description = "A powerful literary novel about isolation, resilience, and survival. " +
                                      "A lone geologist stranded on a remote island must confront nature and " +
                                      "his own past to find a way home. Thoughtful and introspective — " +
                                      "recommended for readers who enjoy character studies, survival stories, " +
                                      "and beautifully written literary fiction.",
                        ISBN = "SOTJ1111111101",
                        ListPrice = 30,
                        Price = 27,
                        Price50 = 25,
                        Price100 = 20,
                        CategoryId = 2,
                    },
                    new Product {
                        Id = 6,
                        Title = "Leaves and Wonders",
                        Author = "Laura Phantom",
                        Description = "A enchanting collection of nature-inspired fantasy short stories " +
                                      "exploring magical forests, ancient spirits, and the hidden lives of " +
                                      "plants and animals. Lyrical, imaginative, and deeply atmospheric — " +
                                      "perfect for fans of fairy tales, folklore, and quiet fantasy that " +
                                      "feels closer to poetry than plot.",
                        ISBN = "FOT000000001",
                        ListPrice = 25,
                        Price = 23,
                        Price50 = 22,
                        Price100 = 20,
                        CategoryId = 3,
                    }
                 );
        }
    }
}
