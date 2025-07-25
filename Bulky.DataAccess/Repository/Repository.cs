﻿using Bulky.DataAccess.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using Bulky.DataAccess.Data;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Bulky.DataAccess.Repository
{
	public class Repository<T> : IRepository<T> where T : class
	{
		private readonly ApplicationDbContext _db;
		internal DbSet<T> dbSet;

		public Repository(ApplicationDbContext db) { 
			
			_db = db;
			this.dbSet = _db.Set<T>();
			_db.Products.Include(u => u.Category);
			//Include : when it retrieves all the products, category will be automatically populated based on foreign key relation
			//It supports multiple include : _db.Products.Include(u => u.Category).Include(u => u.CoverType);
		}
		public void Add(T entity) {
			dbSet.Add(entity);
		}

		public T Get(Expression<Func<T, bool>> filter, string? includeProperties = null, bool tracked = false) {

			IQueryable<T> query;

            if(tracked) {
                query = dbSet;
            } else {
                query = dbSet.AsNoTracking();
            }

            query = query.Where(filter);
            if(!string.IsNullOrEmpty(includeProperties)) {
                foreach(var propertyProp in includeProperties
                    .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) {
                    //we are running loop because we can expect more than one include property in the string
                    query = query.Include(propertyProp);
                }
            }
            return query.FirstOrDefault();
        }

		
		public IEnumerable<T> GetAll(Expression<Func<T, bool>>? filter, string? includeProperties = null) { //If someone provides Category or CategryId based on that we can build include properties
			IQueryable<T> query = dbSet;
			if(filter != null) {
                query = query.Where(filter);
            }
            
            if(!string.IsNullOrEmpty(includeProperties)) {
				foreach(var propertyProp in includeProperties
					.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries )) { 
					//we are running loop because we can expect more than one include property in the string
					query = query.Include(propertyProp);
				}
			}
			return query.ToList();
		}

		public void Remove(T entity) {
			dbSet.Remove(entity);
		}

		public void RemoveRange(IEnumerable<T> entity) {
			dbSet.RemoveRange(entity);
		}
	}
}
