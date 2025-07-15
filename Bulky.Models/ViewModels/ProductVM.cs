using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.Models.ViewModels
{
	public class ProductVM
	{
		public Product Product { get; set; }

		[ValidateNever]
		public IEnumerable<SelectListItem> CategoryList { get; set; }
		//nuget packages are depreciated. To use SelectListItem we add FrameworkReference as Microsoft.AspNetCore.App in Bulky.Models project file
	}
}
