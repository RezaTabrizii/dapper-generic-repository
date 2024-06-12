using Dapper;

namespace DapperGenericRepository.Extensions
{
    public static class DynamicParametersExtension
    {
        public static DynamicParameters ToDynamicParameters(this object param)
        {
            var dynamicParameters = new DynamicParameters();

            if (param != null)
            {
                if (param is not DynamicParameters)
                {
                    foreach (var property in param.GetType().GetProperties().Where(q => q.Name != "CreatedMoment" && q.Name != "ModifiedMoment"))
                        dynamicParameters.Add(property.Name, property.GetValue(param));
                }
                else
                    dynamicParameters.AddDynamicParams(param);
            }

            return dynamicParameters;
        }
    }
}
