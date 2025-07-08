using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ProjectCore.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [DisplayName("Category Name")] //this annotation directly affects tag helpers ('asp-for') on the client side in views
        [MaxLength(130)]
        public string Name { get; set; }

        [DisplayName("Display Order")]
        [Range(1,100, ErrorMessage ="out of range")] //we have provided custom message as third argument. It can be omitted to have default error message
        public int DisplayOrder { get; set; }


    }
}
