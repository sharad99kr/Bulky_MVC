using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository
{
	public class ProductRepository : Repository<Product>, IProductRepository
    {
		//CategoryRepository Inherits shared data access logic methods from a generic base class (Repository<Category>) and
		//Implements custom logic for methods defined in the interface ICategoryRepository

		private ApplicationDbContext _db;
		public ProductRepository(ApplicationDbContext db) : base(db) {
			//process flow
			//step 1: We create an instance of CategoryRepository, probably via Dependency Injection:
			//var categoryRepo = new CategoryRepository(applicationDbContext);
			//step 2: Constructor of CategoryRepository is called with applicationDbContext as the db parameter
			//step 3: : base(db) passes this db up to the constructor of the base class Repository<T>
			//step 4: db is stored in base class and in CategoryRepository
			//Step 5: This db reference will be used to perform crud operation in both the classes
			_db = db;
		}

		public void Update(Product product) {
			_db.Products.Update(product);
		}
	}
}
