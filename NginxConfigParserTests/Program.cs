using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NginxConfigParser;

namespace NginxConfigParserTests
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            // create new file
            NginxConfig.Create()
                .AddOrUpdate("http:server:listen", "80")
                .AddOrUpdate("http:server:root", "/var/wwwroot")
                // add location
                .AddOrUpdate("http:server:location", "/", true, comment: "default")
                .AddOrUpdate("http:server:location:root", "/app1")
                // add location
                .AddOrUpdate("http:server:location[1]", "~ ^/(images|javascript|js|css|flash|media|static)/", true)
                .AddOrUpdate("http:server:location[1]:root", "/app2")
                .AddOrUpdate("http:server:location[1]:expires", "/1d")
                // add location
                .AddOrUpdate("http:server:location[2]", "~/api", true, comment: "api")
                .AddOrUpdate("http:server:location[2]:proxy_pass", "http://server.com")
                // save file
                .Save("temp2.conf");

            Console.WriteLine("temp2.conf content: ");
            Console.WriteLine(File.ReadAllText("temp2.conf"));

            // load exist files
            var config = await NginxConfig.LoadFromAsync("test.conf");
            var rtmp = config.GetGroup("rtmp");
            var applications = config.GetTokens("rtmp:server:application");
            Console.WriteLine(applications.Count); //should be 2
            // read group
            var group = config.GetGroup("http");
            Console.WriteLine(config.GetGroup("http"));

            // read key
            Console.WriteLine(config["error_log"]);
            Console.WriteLine(config.GetToken("error_log"));

            // read key
            Console.WriteLine(config["http"]);

            // read key
            //Console.WriteLine(config["http:include"]);
            //Console.WriteLine(config["http:include[1]"]);

            // read key

            foreach (var item in config.GetTokens("http:include"))
            {
                Console.WriteLine(item);
            };

            // read
            Console.WriteLine(config["http:sendfile"]);
            Console.WriteLine(config.GetToken("http:sendfile"));

            // update value
            config.AddOrUpdate("http:sendfile", "off", comment: "updated!");
            Console.WriteLine(config["http:sendfile"]);

            // add value
            config.AddOrUpdate("http:root", "/var/wwwroot");
            Console.WriteLine(config["http:root"]);

            // add group and value
            config.AddOrUpdate("http:server2:root", "/var/wwwroot");
            Console.WriteLine(config["http:server2:root"]);

            //config.AddOrUpdate("http:server:root", "/var/wwwroot", "updated 1");
            //Console.WriteLine(config["http:server:root"]);

            // update
            config.AddOrUpdate("http:server[0]:root", "/var/wwwroot", comment: "updated 2");
            Console.WriteLine(config["http:server[0]:root"]);

            // update
            config.AddOrUpdate("http:server[1]:root", "/var/wwwroot", comment: "updated 3");
            Console.WriteLine(config["http:server[1]:root"]);

            // add group and value
            config.AddOrUpdate("http:server[3]:root", "/var/wwwroot", comment: "new");
            Console.WriteLine(config["http:server[3]:root"]);

            // add value to group
            config.AddOrUpdate("http:server[3]:location", "/", true, comment: "new loaction");
            config.AddOrUpdate("http:server[3]:location:root", "/var/wwwroot");
            Console.WriteLine(config["http:server[3]:location:root"]);

            // remove
            // config.Remove("http:upstream");

            // save as file
            await config.SaveAsync("temp.conf");

            var config3 = NginxConfig.Create()
                  .AddOrUpdate("user", "www-data")
                  .AddOrUpdate("worker_processes", "auto")
                  .AddOrUpdate("pid", "/run/nginx.pid")
                  .AddOrUpdate("include", "/etc/nginx/modules-enabled/*.conf")

                  // Events
                  .AddOrUpdate("events:worker_connections", "768")
                  .AddOrUpdate("events:multi_accept", "on", comment: "multi_accept on")  // Commented out in the actual configuration

                  // RTMP
                  .AddOrUpdate("rtmp:server:listen", "127.0.0.1:2001")

                  .AddOrUpdate("rtmp:server:application[0]", "live2", true)
                 .AddOrUpdate("rtmp:server:application[0]:live", "on")
                 .AddOrUpdate("rtmp:server:application[0]:allow publish", "all")
                 .AddOrUpdate("rtmp:server:application[0]:allow play", "all")
                 .AddOrUpdate("rtmp:server:application[0]:record", "off")
                 .AddOrUpdate("rtmp:server:application[0]:on_publish", "http://127.0.0.1:1935/rtmpauth")

                 .AddOrUpdate("rtmp:server:application[1]", "live3", true)
                 .AddOrUpdate("rtmp:server:application[1]:live", "on")
                 .AddOrUpdate("rtmp:server:application[1]:allow publish", "all")
                 .AddOrUpdate("rtmp:server:application[1]:allow play", "all")
                 .AddOrUpdate("rtmp:server:application[1]:record", "off")
                 .AddOrUpdate("rtmp:server:application[1]:on_publish", "http://127.0.0.1:1935/rtmpauth");
            // Save file
            var keys = config3.GetTokens("rtmp:server:application");
            var count = keys.Count;
            int found = -1;
            var idx = keys.FirstOrDefault(f => {
                found++;
                return f.Value == "live3";
            });

                await config3.SaveAsync("nginx3.conf");

        }
    }
}
