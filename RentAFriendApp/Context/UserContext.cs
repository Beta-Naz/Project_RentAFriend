using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using RentAFriendApp.Models;

namespace RentAFriendApp.Context
{
    class UserContext
    {
        private static readonly string _url = "https://localhost:7091/user";
        public static async Task<string?> Login(string email , string password)
        {
            using(HttpClient client = new ())
            {
                using(HttpRequestMessage request = new(HttpMethod.Post, _url + "login"))
                {
                    Dictionary<string, string> formData = new Dictionary<string, string>()
                    {
                        ["email"] = email,
                        ["password"] = password
                    };
                    FormUrlEncodedContent content = new FormUrlEncodedContent(formData);
                    request.Content = content;
                    var response = await client.SendAsync(request);
                    if(response.StatusCode == HttpStatusCode.OK)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        Auth? dataAuth = JsonConvert.DeserializeObject<Auth>(result);
                        if(dataAuth != null)
                        {
                            return dataAuth.Token;
                        }
                    }
                }
            }
            return null;
        }
    }
}
