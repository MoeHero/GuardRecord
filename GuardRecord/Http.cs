using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GuardRecord
{
    internal static class Http
    {
        private const int MAX_RETRY_COUNT = 10;

        public static string SendRequest(HttpRequest request) {
            using var client = new HttpClient();
            using var message = new HttpRequestMessage(request.Method, request.Url);
            if(request.Method == HttpMethod.Post) {
                message.Content = new StringContent(request.Body, Encoding.UTF8, "application/json");
            }
            try {
                using var response = client.Send(message);
                return response.Content.ReadAsStringAsync().Result;
            } catch(Exception e) {
                if(request.RetryCount == MAX_RETRY_COUNT) throw e;
                return SendRequest(request);
            }
        }


        public static string Get(string url) {
            var request = new HttpRequest(url);
            return SendRequest(request);
        }

        public static JObject GetJson(string url) {
            return JObject.Parse(Get(url));
        }

        public static string Post(string url, string body) {
            var request = new HttpRequest(url) { Method = HttpMethod.Post, Body = body };
            return SendRequest(request);
        }

        public static JObject PostJson(string url, string body) {
            return JObject.Parse(Post(url, body));
        }
    }

    internal class HttpRequest
    {
        public HttpRequest(string url) {
            Url = url;
        }

        public string Url { get; }

        public string Body { get; set; }

        public HttpMethod Method { get; set; } = HttpMethod.Get;

        public int RetryCount { get; set; }
    }
}
