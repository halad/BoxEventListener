using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Thinktecture.IdentityModel.Clients;

namespace BoxEventListenerMvc4.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {
            var url = OAuth2Client.CreateCodeFlowUrl(Constants.AuthorizeEndPoint,
                                                     Constants.ClientId,
                                                     Constants.Scope,
                                                     Url.Action("Index", "Callback", null, "https"));

            ViewBag.AuthorizeUrl = url;
            return View();
        }
    }
}
