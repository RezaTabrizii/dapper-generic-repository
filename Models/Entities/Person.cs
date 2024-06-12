using DapperGenericRepository.Models.Base;

namespace DapperGenericRepository.Models.Entities
{
    public class Person : TableBase
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string EmailAddress { get; set; }
        public string StateName { get; set; }
        public string CityName { get; set; }
        public string Address { get; set; }
        public int ZipCode { get; set; }
    }
}
