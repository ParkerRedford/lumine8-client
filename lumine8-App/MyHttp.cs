using lumine8;
using Newtonsoft.Json;
using System.Text;

namespace lumine8.MyHttp
{
    public class MyHttp : HttpClient
    {
        private readonly SingletonVariables variables;
        public MyHttp(SingletonVariables variables)
        {
            this.variables = variables;
            BaseAddress = new Uri(this.variables.uri);
        }

        public async Task<T> GetFromJsonAsync<T>(string uri)
        {
            var json = await GetAsync(uri);
            var res = await json.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(res);
        }

        public async Task<HttpResponseMessage> PostJsonAsync(string uri, object obj)
        {
            var ser = JsonConvert.SerializeObject(obj);
            var content = new StringContent(ser, encoding: Encoding.UTF8, "application/json");
            return await PostAsync(uri, content);
        }

        public async Task<T> ReadFromJsonAsync<T>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(content);
        }
    }
}
