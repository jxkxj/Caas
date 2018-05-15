using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Linq;
using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

using Caas.Models;
using Caas.Client;
using Caas.Web;
using Newtonsoft.Json;

namespace Caas.Test
{
    [TestClass]
    public class ConfigControllerTests   
    {
		TestServer testServer;
		HttpClient client;

		public ConfigControllerTests()
		{
			var host = WebHost.CreateDefaultBuilder(null)
							  .UseStartup<Startup>();
			testServer = new TestServer(host);
			client = testServer.CreateClient();

#if TEST
			CaasManager.Init(client);
#endif
		}
              
        [TestInitialize]
		public void BuildConfigs()
		{
			var connection = new SqliteConnection("Data Source = tests.db");
			connection.Open();

			var options = new DbContextOptionsBuilder<DatabaseContext>()
				.UseSqlite(connection)
				.Options;

            using(var context = new DatabaseContext(options))
			{
				var config1 = context.Config.Add(new Config()
				{
					Key = "Config1",
					Value = "Value1",
					Created = DateTime.UtcNow
				}).Entity;
				var config2 = context.Config.Add(new Config()
				{
					Key = "Config2",
					Value = "Value2",
					Created = DateTime.UtcNow
				}).Entity;
				var config3 = context.Config.Add(new Config()
				{
					Key = "Config3",
					Value = "Value3",
					Created = DateTime.UtcNow
				}).Entity;
				var clientType = context.ClientType.Add(new ClientType()
				{
					Name = "UITest",
					Created = DateTime.UtcNow
				}).Entity;
				var client = context.Client.Add(new Models.Client()
				{
					Identifier = "UITestRunner",
					ClientType = clientType,
					Created = DateTime.UtcNow
				}).Entity;
				context.ConfigAssociation.Add(new ConfigAssociation()
				{
					Client = client,
					Config = config2,
					Created = DateTime.UtcNow
				});
				context.ConfigAssociation.Add(new ConfigAssociation()
                {
                    Client = client,
                    Config = config3,
                    Created = DateTime.UtcNow
                });

				context.SaveChanges();
			}

			connection.Close();
		}

        [TestCleanup]
        public void CleanUp()
		{
			//Remove SQLite DB
			File.Delete("tests.db");
		}      

        CheckIn GetLastCheckIn()
		{
			var connection = new SqliteConnection("Data Source = tests.db");
            connection.Open();

            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseSqlite(connection)
                .Options;

			using (var context = new DatabaseContext(options))
			{
				return context.CheckIn.OrderByDescending(x => x.CheckInTime)
										 .Include(x => x.Client)
										 .Include(x => x.Client.ClientType)
										 .FirstOrDefault();
			}
		}      

        [TestMethod]
        public async Task CheckIn()
        {
			await CaasManager.CheckInClient("UITestRunner", "UITest");
            
			var lastCheckIn = GetLastCheckIn();
			Assert.AreEqual("UITestRunner", lastCheckIn.Client.Identifier);
			Assert.AreEqual("UITest", lastCheckIn.Client.ClientType.Name);
			Assert.IsNull(lastCheckIn.ExtraData);
        }

        [TestMethod]
        public async Task CheckInWithExtraData()
		{
			Tuple<double, double> model = new Tuple<double, double>(45, -45);

			await CaasManager.CheckInClient("UITestRunner", "UITest", model);
            
			var lastCheckIn = GetLastCheckIn();
            Assert.AreEqual("UITestRunner", lastCheckIn.Client.Identifier);
            Assert.AreEqual("UITest", lastCheckIn.Client.ClientType.Name);
			var extraData = JsonConvert.DeserializeObject<Tuple<double, double>>(lastCheckIn.ExtraData);
			Assert.AreEqual(45, extraData.Item1);
			Assert.AreEqual(-45, extraData.Item2);
		}

        [TestMethod]
        public async Task GetConfig()
		{
			var config = await CaasManager.GetConfigAsync("Config1");

			Assert.AreEqual(1, config.ConfigId);         
			Assert.AreEqual("Config1", config.Key);
			Assert.AreEqual("Value1", config.Value);
		}

        [TestMethod]
        public async Task GetConfigForClient()
		{
			var config = await CaasManager.GetConfigForClientAsync("UITestRunner", "UITest", "Config2");

			Assert.AreEqual(2, config.ConfigId);
            Assert.AreEqual("Config2", config.Key);
            Assert.AreEqual("Value2", config.Value);
		}

        [TestMethod]
        public async Task GetAllConfigsForClient()
		{
			var configs = await CaasManager.GetAllConfigsForClientAsync("UITestRunner", "UITest");

			Assert.AreEqual(2, configs.Count());
		}

        [TestMethod]
        public async Task GetAllConfigs()
		{
			var configs = await CaasManager.GetAllConfigsAsync();

			Assert.AreEqual(3, configs.Count());
		}      
    }
}
