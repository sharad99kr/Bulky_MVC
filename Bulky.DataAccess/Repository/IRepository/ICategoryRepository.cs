using Bulky.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository.IRepository
{
	public interface ICategoryRepository : IRepository<Category>
	{
		//This interface will get all the methods defined in IRepository
		//and in addition, implement 2 new methods i.e. Update and Save
		void Update(Category category);
		void Save();
	}
}
