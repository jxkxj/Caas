using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Hosting;

using Caas.Client;

namespace Caas.Test
{
    [TestClass]
    public class ConfigControllerTests   
    {
		readonly TestServer _server;
		readonly HttpClient _client;

        public ConfigControllerTests()
		{
			_server = new TestServer(new WebHostBuilder()
                .UseStartup<Web.Startup>());
			_client = _server.CreateClient();

			CaasManager.Init(_client);
		}

        [TestMethod]
        public async Task CheckIn()
        {
			await CaasManager.CheckInClient("UITestRunner", "UITest");
			Assert.IsTrue(true);
        }

        [TestMethod]
        public async Task GetConfig()
		{
			var config = await CaasManager.GetConfigAsync("Config1");

			Assert.AreEqual(1, config.ConfigId);
			Assert.AreEqual(DateTime.Parse("2018-05-15 02:50:30.087361"), config.Created);
			Assert.AreEqual("Config1", config.Key);
			Assert.AreEqual("Value1", config.Value);
		}

        [TestMethod]
        public async Task GetConfigForClient()
		{
			var config = await CaasManager.GetConfigForClientAsync("UITestRunner", "UITest", "Config2");

			Assert.AreEqual(2, config.ConfigId);
            Assert.AreEqual(DateTime.Parse("2018-05-15 02:50:30.087361"), config.Created);
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
