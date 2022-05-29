using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace brock.Services
{
    public class ConfigService
    {
        private IConfiguration _config;
        public void Initialize()
        {
            Console.WriteLine("[[ ConfigService.Initialize() ]]");

            IHost host = Host.CreateDefaultBuilder().Build();
            _config = host.Services.GetRequiredService<IConfiguration>();
            //foreach (KeyValuePair<string, string> pair in _config.AsEnumerable())
            //{
            //    Console.WriteLine($"\t{pair.Key}={pair.Value}");
            //}
        }

        public T Get<T>(string key)
        {
            return _config.GetValue<T>(key);
        }
    }
}
