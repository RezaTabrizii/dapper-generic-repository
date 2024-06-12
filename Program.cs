using DapperGenericRepository.Contracts;
using DapperGenericRepository.Models.Entities;
using DapperGenericRepository.Models.Parameters;
using DapperGenericRepository.Repository;
using DapperGenericRepository.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DapperGenericRepository
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using IHost host = CreateHostBuilder(args).Build();

            Random random = new();
            var _personRepository = host.Services.GetRequiredService<IPersonRepository>();

            Person person = new()
            {
                Id = random.Next(100000, 9999999),
                FirstName = "Yui",
                LastName = "Tanaka",
                Address = "123 Sakura Lane",
                CityName = "Kyoto",
                StateName = "Osaka",
                EmailAddress = "yui@gmail.com",
                PhoneNumber = "456789123",
                ZipCode = 54321
            };

            List<Person> people =
            [
                new()
                {
                    Id = random.Next(100000, 9999999),
                    FirstName = "Satoshi",
                    LastName = "Yamamoto",
                    Address = "789 Pine Street",
                    CityName = "Tokyo",
                    StateName = "Tokyo",
                    EmailAddress = "satoshi@gmail.com",
                    PhoneNumber = "912345678",
                    ZipCode = 54321
                },
                new()
                {
                    Id = random.Next(100000, 9999999),
                    FirstName = "Riku",
                    LastName = "Kobayashi",
                    Address = "789 Pine Street",
                    CityName = "Osaka",
                    StateName = "Osaka",
                    EmailAddress = "kobayashi@gmail.com",
                    PhoneNumber = "981234567",
                    ZipCode = 6006006
                }
            ];

            await _personRepository.InsertAsync(person, true);

            await _personRepository.InsertManyAsync(people, true);

            await _personRepository.ReplaceAsync(person);

            await _personRepository.ReplaceAsync(person, new ConditionParams
            {
                ConditionLeftSide = ["FirstName"],
                ComparisonOperators = ["="],
                ConditionRightSide = ["a"],
            });

            await _personRepository.ReplaceManyAsync(people);

            await _personRepository.ReplaceManyAsync(people,
                [
                    new()
                    {
                        ConditionLeftSide = ["EmailAddress"],
                        ComparisonOperators = ["="],
                        ConditionRightSide = ["Xleylo@gmail.com"],
                    },
                    new()
                    {
                        ConditionLeftSide = ["EmailAddress"],
                        ComparisonOperators = ["="],
                        ConditionRightSide = ["AsgarMedusa@gmail.com"],
                    }
                ]);


            await _personRepository.UpdateAsync(new
            {
                Id = 2824662,
                FirstName = "Hana",
                LastName = "Mori"
            });

            await _personRepository.UpdateAsync(new
            {
                FirstName = "Hana",
                LastName = "Mori"
            }, new ConditionParams
            {
                ConditionLeftSide = ["FirstName"],
                ComparisonOperators = ["="],
                ConditionRightSide = ["Satoshi"],
            });

            await _personRepository.UpdateManyAsync(new List<object>
                {
                    new
                    {
                        Id = 9789305,
                        EmailAddress = "Ito@gmail.com"
                    },
                    new
                    {
                        Id = 8785715,
                        EmailAddress = "Mori@gmail.com"
                    }
                });

            await _personRepository.UpdateManyAsync(new List<object>
                {
                    new {
                        EmailAddress = "Ito@gmail.com"
                    },
                    new {
                        EmailAddress = "Mori@gmail.com"
                    }
                },
                [
                   new()
                   {
                       ConditionLeftSide = ["FirstName"],
                       ComparisonOperators = ["="],
                       ConditionRightSide = ["Satoshi"],
                   },
                    new()
                    {
                        ConditionLeftSide = ["FirstName"],
                        ComparisonOperators = ["="],
                        ConditionRightSide = ["Yui"],
                    }
                ]);

            await _personRepository.DeleteAsync(new { id = "8785715" });

            await _personRepository.DeleteAsync(new ConditionParams
            {
                ConditionLeftSide = ["FirstName", "Id"],
                ComparisonOperators = ["=", "="],
                ConditionRightSide = ["Yui", "9789305"],
                LogicOperators = ["or"]
            });

            var personData = await _personRepository.FindOneWithQueryAsync("SELECT * FROM PEOPLE WHERE LastName = @LastName",
                new { LastName = "Tanaka" });

            var personsData = await _personRepository.GetWithQueryAsync("SELECT * FROM PEOPLE WHERE LastName = @LastName AND FirstName = @FirstName",
                new { FirstName = "Yui", LastName = "Tanaka" });

            var pagedPersonsData = await _personRepository.GetWithPagingAsync(new GetWithPagingParams
            {
                SqlQuery = "SELECT * FROM PEOPLE",
                Page = 1,
                PageSize = 10,
                OrderByDescending = false,
                OrderColumnName = "FirstName",
                ConditionLeftSide = ["FirstName", "Id"],
                ComparisonOperators = ["=", "="],
                ConditionRightSide = ["Yui", "8785715"],
                LogicOperators = ["or"]
            });

            var personsData1 = await _personRepository.GetWithStoreProcedureAsync("spPeople_Get", new { PersonId = "8785715" });

            await host.RunAsync();
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((builder, services) =>
                {
                    //services.Configure<SqlSettings>(builder.Configuration.GetSection(nameof(SqlSettings)));
                    services.AddSingleton(sp => sp.GetRequiredService<IOptions<SqlSettings>>().Value);

                    services.AddSingleton<IPersonRepository, PersonRepository>();
                    services.AddSingleton<ISqlConnection, SqlConnectionProvider>();
                });
    }
}
