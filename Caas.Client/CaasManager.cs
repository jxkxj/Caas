using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Caas.Models;

namespace Caas.Client
{
	/// <summary>
	/// Manages all calls back to Caas so all you have to worry
	/// about is handling the <see cref="Config"/>
	/// </summary>   
	public static class CaasManager
	{
		private static bool isInitialized;
		private static HttpClient httpClient;

		/// <summary>
		/// Initialize <see cref="CaasManager"/> with your endpoint and optional <see cref="HttpMessageHandler"/>
		/// </summary>
		/// <param name="endpoint">Root endpoint to Caas</param>
		/// <param name="httpHandler">Optional <see cref="HttpMessageHandler"/> for <see cref="HttpClient"/></param>
		public static void Init(string endpoint, HttpMessageHandler httpHandler = null)
		{
			if (Uri.TryCreate(endpoint, UriKind.Absolute, out Uri endpointUri))
			{
				if (httpHandler == null)
					httpClient = new HttpClient();
				else
					httpClient = new HttpClient(httpHandler);
				httpClient.BaseAddress = endpointUri;
				isInitialized = true;
			}
			else
			{
				isInitialized = false;
				throw new CaasException(CaasException.INVALID_URI);
			}
		}

#if TEST
		public static void Init(HttpClient client)
		{
			httpClient = client;
			isInitialized = true;
		}
#endif

		private static async Task<T> GetResponseAsync<T>(string url, params string[] values) where T : class
		{
			if (!isInitialized)
				throw new CaasException(CaasException.NOT_INITIALIZED);

			HttpResponseMessage result = await httpClient.GetAsync(string.Format(url, values)).ConfigureAwait(false);

			if (result.IsSuccessStatusCode)
			{
				try
				{
					return JsonConvert.DeserializeObject<T>(await result.Content.ReadAsStringAsync().ConfigureAwait(false));
                }
                catch (JsonSerializationException jsex)
                {
                    throw new CaasException(CaasException.INVALID_SERVER_RESPONSE, jsex.Message);
                }
            }
            else if (result.StatusCode == System.Net.HttpStatusCode.NoContent)
                return null;
            else if (result.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                throw new CaasException(CaasException.SERVER_ERROR, result.ReasonPhrase);
            else if (result.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new CaasException(CaasException.SERVER_NOT_FOUND, result.ReasonPhrase);
            else
                throw new CaasException(CaasException.UNKNOWN_SERVER_RESPONSE, result.ReasonPhrase);
        }
#region Config
        /// <summary>
        /// Get a specific <see cref="Config"/> by <see cref="Config.Key"/> for a <see cref="Models.Client"/>
        /// </summary>
        /// <param name="identifier">The <see cref="Models.Client.Identifier"/></param>
        /// <param name="type">The <see cref="ClientType.Name"/></param>
        /// <param name="key">The <see cref="Config.Key"/></param>
        /// <returns>The <see cref="Config"/> or null</returns>
        public static Task<Config> GetConfigForClientAsync(string identifier, string type, string key) => GetResponseAsync<Config>("/api/config/getconfigforclient?identifier={0}&type={1}&key={2}", identifier, type, key);

        /// <summary>
        /// Get a specific <see cref="Config"/> by <see cref="Config.Key"/>
        /// </summary>
        /// <param name="key">The <see cref="Config.Key"/></param>
        /// <returns>The <see cref="Config"/> or null</returns>
        public static Task<Config> GetConfigAsync(string key) => GetResponseAsync<Config>("/api/config/getconfig?key={0}", key);

        /// <summary>
        /// Get all <see cref="Config"/>
        /// </summary>
        /// <returns>All <see cref="Config"/></returns>
        public static Task<IEnumerable<Config>> GetAllConfigsAsync() => GetResponseAsync<IEnumerable<Config>>("/api/config/getallconfigs");

        /// <summary>
        /// Get all <see cref="Config"/> for a <see cref="Models.Client"/>
        /// </summary>
		/// <param name="identifier">The <see cref="Models.Client.Identifier"/></param>
        /// <param name="type">The <see cref="ClientType.Name"/></param>
        /// <returns>All <see cref="Config"/> or null</returns>
        public static Task<IEnumerable<Config>> GetAllConfigsForClientAsync(string identifier, string type) => GetResponseAsync<IEnumerable<Config>>("/api/config/getallconfigsforclient?identifier={0}&type={1}", identifier, type);

		/// <summary>
        /// Check In <see cref="Models.Client"/>
        /// </summary>
        /// <param name="identifier">The <see cref="Models.Client.Identifier"/></param>
        /// <param name="type">The <see cref="ClientType.Name"/></param>
        /// <returns></returns>
        public static Task CheckInClient(string identifier, string type) => CheckInClient<string>(identifier, type, null);      

        /// <summary>
        /// Check In <see cref="Models.Client"/> with extra data
        /// </summary>
        /// <param name="identifier">The <see cref="Models.Client.Identifier"/></param>
        /// <param name="type">The <see cref="ClientType.Name"/></param>
		/// <param name="extraData">Extra data you want stored with the <see cref=" Models.CheckIn"/></param>
        /// <returns></returns>
        public static async Task CheckInClient<T>(string identifier, string type, T extraData)
        {
            if (!isInitialized)
                throw new CaasException(CaasException.NOT_INITIALIZED);

            var client = new Models.Client()
            {
                Identifier = identifier,
                ClientType = new ClientType()
                {
                    Name = type
                }
            };

			var checkIn = new Models.CheckIn<T>()
			{
				Client = client,
				ConvertedExtraData = extraData
			};
            HttpResponseMessage result = await httpClient.PostAsync("/api/config/checkin", new StringContent(JsonConvert.SerializeObject(checkIn), System.Text.Encoding.UTF8, "application/json")).ConfigureAwait(false);

            if (result.IsSuccessStatusCode)
                return;
            else if (result.StatusCode == System.Net.HttpStatusCode.NoContent)
                return;
            else if (result.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                throw new CaasException(CaasException.SERVER_ERROR, result.ReasonPhrase);
            else if (result.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new CaasException(CaasException.SERVER_NOT_FOUND, result.ReasonPhrase);
            else
                throw new CaasException(CaasException.UNKNOWN_SERVER_RESPONSE, result.ReasonPhrase);
        }
#endregion
    }
}
