namespace DapperGenericRepository.Settings
{
    public class SqlSettings
    {
        public string ConnectionString { get; set; }
            = "YOUR_CONNECTION_STRING";
    }
}

//NOTE: Put the connection string in the appsettings.json in real project:
//
//  "SqlSettings": {
//      "ConnectionString": "YOUR_CONNECTION_STRING"
//  },
//