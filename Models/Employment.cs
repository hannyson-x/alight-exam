using System.ComponentModel.DataAnnotations;

namespace alight_exam.Models
{
    public class Employment
    {
        public int Id { get; set; }
        public string Company { get; set; }
        public uint MonthsOfExperience { get; set; }
        public uint Salary { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
