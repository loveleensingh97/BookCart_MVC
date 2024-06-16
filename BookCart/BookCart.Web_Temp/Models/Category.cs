using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace BookCart.Web_Temp.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(30)]
        [DisplayName("Category Name")]
        public string Name { get; set; }

        [DisplayName("Display Order")]
        [Range(1, 100, ErrorMessage = "Display Order must be between 1 & 100")]
        //If we have multiple categories, which categories should be displayed first on the page
        public int DisplayOrder { get; set; }
    }
}
