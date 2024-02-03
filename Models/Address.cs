using System.ComponentModel.DataAnnotations;

namespace alight_exam.Models
{
    public class Address
    {
        public int Id { get; set; }        
        public string? Street { get; set; }
        public string? City { get; set; }
        public int? PostCode { get; set; }
        public int UserId { get; set; }
    }
}
