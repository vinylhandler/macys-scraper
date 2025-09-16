using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using Xunit;
using Moq;
using Moq.Protected;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MacysScraperTests
{
    public class MacysScraperTests
    {
        private string LoadMockResponse()
        {
            var testDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var mockFile = Path.Combine(testDir!, "..", "..", "..", "..", "shared", "mock_responses.json");
            var fullPath = Path.GetFullPath(mockFile);
            return File.ReadAllText(fullPath);
        }

        [Fact]
        public void TestPayloadStructure()
        {
            var parameters = new Dictionary<string, string>()
            {
                { "source", "universal" },
                { "url", "https://www.macys.com/shop/sale?id=3536&trackingid=407x733169&m_sc=sem&m_sb=google&m_tp=trademark&m_ac=google_trademark_international&m_ag=macy%27score_exact&m_cn=ggl_trademark_intl_lithuania_exact&m_pi=go_cmp-94807774_adg-154238318312_ad-674544461209_kwd-252677959_dev-c_ext-102882313401_prd-&gad_source=1&gclid=cj0kcqiayewrbhddarisagp1mwsg3z6ogoqrztdycjyqio5togc316ldkwuqkkbhrmiv4i_ho0gcjlkaagl6ealw_wcb"},
            };

            Assert.Equal("universal", parameters["source"]);
            Assert.Contains("macys.com", parameters["url"]);
            Assert.Equal(2, parameters.Count);
        }

        [Fact]
        public void TestAuthenticationSetup()
        {
            const string username = "test_user";
            const string password = "test_pass";

            var authenticationString = $"{username}:{password}";
            var base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.UTF8.GetBytes(authenticationString));
            var expectedAuthHeader = "Basic " + base64EncodedAuthenticationString;

            Assert.NotEmpty(base64EncodedAuthenticationString);
            Assert.StartsWith("Basic ", expectedAuthHeader);
            
            var decodedBytes = Convert.FromBase64String(base64EncodedAuthenticationString);
            var decodedString = Encoding.UTF8.GetString(decodedBytes);
            Assert.Equal($"{username}:{password}", decodedString);
        }

        [Fact]
        public async Task TestHTTPClientConfiguration()
        {
            var mockResponse = LoadMockResponse();
            
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", 
                    ItExpr.IsAny<HttpRequestMessage>(), 
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(mockResponse, Encoding.UTF8, "application/json")
                })
                .Callback<HttpRequestMessage, CancellationToken>((request, token) =>
                {
                    Assert.Equal(HttpMethod.Post, request.Method);
                    Assert.Equal("/v1/queries", request.RequestUri?.AbsolutePath);
                    Assert.NotNull(request.Headers.Authorization);
                    Assert.Equal("Basic", request.Headers.Authorization?.Scheme);
                });

            var client = new HttpClient(mockHandler.Object);
            client.BaseAddress = new Uri("https://realtime.oxylabs.io");

            var parameters = new Dictionary<string, string>()
            {
                { "source", "universal" },
                { "url", "https://www.macys.com/shop/sale"},
            };

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/v1/queries");
            requestMessage.Content = JsonContent.Create(parameters);

            const string username = "user";
            const string password = "pass1";
            var authenticationString = $"{username}:{password}";
            var base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.UTF8.GetBytes(authenticationString));
            requestMessage.Headers.Add("Authorization", "Basic " + base64EncodedAuthenticationString);

            var response = await client.SendAsync(requestMessage);
            
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            mockHandler.Verify();
        }

        [Fact]
        public async Task TestAPIResponseStructure()
        {
            var mockResponse = LoadMockResponse();
            
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", 
                    ItExpr.IsAny<HttpRequestMessage>(), 
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(mockResponse, Encoding.UTF8, "application/json")
                });

            var client = new HttpClient(mockHandler.Object);
            client.BaseAddress = new Uri("https://realtime.oxylabs.io");

            var parameters = new Dictionary<string, string>()
            {
                { "source", "universal" },
                { "url", "https://www.macys.com/shop/sale"},
            };

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/v1/queries");
            requestMessage.Content = JsonContent.Create(parameters);

            var response = await client.SendAsync(requestMessage);
            var contents = await response.Content.ReadAsStringAsync();

            var jsonResponse = JObject.Parse(contents);
            
            Assert.True(jsonResponse.ContainsKey("results"));
            Assert.True(jsonResponse["results"] is JArray);
            Assert.True(((JArray)jsonResponse["results"]!).Count > 0);

            var firstResult = ((JArray)jsonResponse["results"]!)[0] as JObject;
            Assert.NotNull(firstResult);
            Assert.True(firstResult!.ContainsKey("content"));
            Assert.True(firstResult.ContainsKey("job_id"));
            Assert.True(firstResult.ContainsKey("status_code"));
            Assert.Equal(200, firstResult["status_code"]!.Value<int>());
        }
    }
}
