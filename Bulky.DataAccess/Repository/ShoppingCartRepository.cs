﻿using Bulky.DataAccess.Data;
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
	public class ShoppingCartRepository : Repository<ShoppingCart>, IShoppingCartRepository
	{
		private ApplicationDbContext _db;
		public ShoppingCartRepository(ApplicationDbContext db) : base(db) {
			_db = db;
		}
		public void Update(ShoppingCart shoppingCart) {
			_db.ShoppingCarts.Update(shoppingCart);
		}
	}
}
