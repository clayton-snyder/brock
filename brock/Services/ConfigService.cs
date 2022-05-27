using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace brock.Services
{
    public class ConfigService
    {
        private IConfiguration _config;
        public void Initialize()
        {
            IHost host = Host.CreateDefaultBuilder().Build();
            _config = host.Services.GetRequiredService<IConfiguration>();
            Console.WriteLine("Config loaded:");
            foreach (KeyValuePair<string, string> pair in _config.AsEnumerable())
            {
                Console.WriteLine($"\t{pair.Key}={pair.Value}");
            }
        }

        public T Get<T>(string key)
        {
            return _config.GetValue<T>(key);
        }
    }
}
