using System.ComponentModel.DataAnnotations;

namespace HashMat.Models
{
    public class JTRCrackModel
    {
        [Required]
        public string? Name
        {
            get;
            set;
        }
    }
}
