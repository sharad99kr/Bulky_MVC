﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository.IRepository
{
	public interface IRepository<T> where T : class
	{

		//T - Category or any generic model on which we perform crud operation
		// Retriieve categories
		IEnumerable<T> GetAll(string? includeProperties = null);
		T Get(Expression<Func<T,bool>> filter, string? includeProperties = null); //this supports link operation to apply condition if needed
		void Add(T entity);
		void Remove(T entity);
		void RemoveRange(IEnumerable<T> entity);

		//void Update(T entity); //usually update has unique operation, like update few selected properties
		//and it varies from model to model, so we will keep update outside the repository 

	}
}
