using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Thinktecture.IdentityModel.Clients;
using Thinktecture.IdentityModel.Constants;

namespace BoxEventListenerMvc4.Controllers
{
    public class CallbackController : Controller
    {
        //
        // GET: /Callback/

        public ActionResult Index()
        {
            ViewBag.Code = Request.QueryString["code"] ?? "none";
            ViewBag.Error = Request.QueryString["error"] ?? "none";

            return View();
        }

        [HttpPost]
        [ActionName("Index")]
        public ActionResult Postback()
        {
            //var client = new OAuth2Client(new Uri(Constants.TokenEndPoint));

            var code = Request.QueryString["code"];

            //"XEJm9tmUSvrW0USeh4COfdp3OIxKJGKu";            

            //var response = client.RequestAccessTokenCode(code, new Dictionary<string, string>
            //    {
            //        {"client_id", Constants.ClientId},
            //        {"client_secret", Constants.ClientSecret}
            //    });

            AccessTokenResponse tokenResponse;
            using (var client = new HttpClient())
            {
                var postData = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("client_id", Constants.ClientId),
                        new KeyValuePair<string, string>("client_secret", Constants.ClientSecret),
                        new KeyValuePair<string, string>("code", code),
                        new KeyValuePair<string, string>("grant_type", "authorization_code")
                    };

                HttpContent content = new FormUrlEncodedContent(postData);

                var response = client.PostAsync(Constants.TokenEndPoint, content).Result;
                response.EnsureSuccessStatusCode();

                var responseString = response.Content.ReadAsStringAsync().Result;

                var isJson = responseString.IsJson();
                if (!isJson)
                {
                    ViewBag.Code = code;
                    ViewBag.Html = responseString;
                    return View("BoxError");
                }

                var json = JObject.Parse(responseString);
                tokenResponse = CreateResponseFromJson(json);
            }

            return View("Postback", tokenResponse);
        }

        private AccessTokenResponse CreateResponseFromJson(JObject json)
        {
            var response = new AccessTokenResponse
            {
                AccessToken = json["access_token"].ToString(),
                TokenType = json["token_type"].ToString(),
                ExpiresIn = int.Parse(json["expires_in"].ToString())
            };

            if (json["refresh_token"] != null)
            {
                response.RefreshToken = json["refresh_token"].ToString();
            }

            return response;
        }

        [HttpPost]
        [ActionName("CallService")]
        public ActionResult CallService(string token)
        {
            string responseString;
            using (var client = new HttpClient())
            {
                client.SetBearerToken(token);

                var response = client.GetAsync("https://www.box.com/api/2.0/folders/0").Result;
                response.EnsureSuccessStatusCode();

                responseString = response.Content.ReadAsStringAsync().Result;
            }

            var isJson = responseString.IsJson();
            if (!isJson)
            {
                ViewBag.Html = responseString;
                return View("BoxError");
            }

            return View("Folders", model: JObject.Parse(responseString).ToString(Formatting.Indented));
        }


        [HttpPost]
        public ActionResult RenewToken(string refreshToken)
        {
            //var client = new OAuth2Client(
            //    new Uri(Constants.TokenEndPoint),
            //    Constants.ClientId,
            //    Constants.ClientSecret);

            //var response = client.RequestAccessTokenRefreshToken(refreshToken);
            //return View("Postback", response);

            //{ OAuth2Constants.GrantType, OAuth2Constants.GrantTypes.RefreshToken },
            //    { OAuth2Constants.GrantTypes.RefreshToken, refreshToken}

            AccessTokenResponse tokenResponse;
            using (var client = new HttpClient())
            {
                var postData = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("client_id", Constants.ClientId),
                        new KeyValuePair<string, string>("client_secret", Constants.ClientSecret),
                        new KeyValuePair<string, string>("refresh_token", refreshToken),
                        new KeyValuePair<string, string>("grant_type", OAuth2Constants.GrantTypes.RefreshToken)
                    };

                HttpContent content = new FormUrlEncodedContent(postData);

                var response = client.PostAsync(Constants.TokenEndPoint, content).Result;
                response.EnsureSuccessStatusCode();

                var responseString = response.Content.ReadAsStringAsync().Result;

                var isJson = responseString.IsJson();
                if (!isJson)
                {
                    ViewBag.Code = refreshToken;
                    ViewBag.Html = responseString;
                    return View("BoxError");
                }

                var json = JObject.Parse(responseString);
                tokenResponse = CreateResponseFromJson(json);
            }

            return View("Postback", tokenResponse);
        }

        public PartialViewResult GetCurrentStreamPosition(string token)
        {
            using (var client = new HttpClient())
            {
                client.SetBearerToken(token);

                var response = client.GetAsync(Constants.EventsEndPoint+ "?stream_position=now").Result;
                response.EnsureSuccessStatusCode();

               var responseString = response.Content.ReadAsStringAsync().Result;

                var model = JObject.Parse(responseString).ToObject<CurrentStreamPositionResponse>();


               return PartialView(model);
            }
        }


        public ActionResult Updates()
        {
            //http://developers.box.com/docs/#events
            //registered ina time-sequeneced list -> stream_position
            //pass in a stream_position, get events that happened right before the stream position and up to the
            //stream_position or chunk_size = number of event records returned, whichever is less
            //may receive duplicates so we have event_id
            //stream_position = now or no stream_position (will default to 0)
            //store between 2 weeks and 2 months
            //stream_position given back, until no events
            //admin_logs stream_type

            return View();
        }

        public ActionResult Events()
        {
            return View();
        }
    }

    public static class Constants
    {
        public const string AuthorizeEndPoint = "https://www.box.com/api/oauth2/authorize";
        public const string TokenEndPoint = "https://www.box.com/api/oauth2/token";
        public const string ClientId = "ugbga7x1m4n2q4uclu6cl3sxk0ld5zwo";
        public const string ClientSecret = "0y9kflSZi1mpqozEfjGASy0leertdSJM";
        public const string Scope = "";
        public const string EventsEndPoint = "https://api.box.com/2.0/events";
        public static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
    }

    public static class Extensions
    {
        public static bool IsJson(this string jsonData)
        {
            return jsonData.Trim().Substring(0, 1).IndexOfAny(new[] { '[', '{' }) == 0;
        }
    }

    public class CurrentStreamPositionResponse
    {
        [JsonProperty("chunk_size")]
        public string ChunkSize { get; set; }

        [JsonProperty("next_stream_position")]
        public long NextStreamPosition { get; set; }

        [JsonProperty("entries")]
        public string Entries { get; set; }

        public DateTime NextStreamPositionDateTime { get { return NextStreamPosition == 0 ? Constants.Epoch : Constants.Epoch.AddSeconds(NextStreamPosition); } }
    }
}
