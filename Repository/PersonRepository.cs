using DapperGenericRepository.Contracts;
using DapperGenericRepository.Models.Entities;
using Pluralize.NET;

namespace DapperGenericRepository.Repository
{
    public class PersonRepository(ISqlConnection sqlConnection)
        : DapperBaseRepository<Person>(sqlConnection), IPersonRepository;
}
