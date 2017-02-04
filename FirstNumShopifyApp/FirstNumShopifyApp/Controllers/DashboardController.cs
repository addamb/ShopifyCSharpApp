using FirstNumShopifyApp.Attributes;
using FirstNumShopifyApp.Engines;
using Microsoft.AspNet.Identity.Owin;
using ShopifySharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FirstNumShopifyApp.Controllers
{
    public class DashboardController : Controller
    {
		/// <summary>
		/// Dashboard - Index
		/// This is the main Screen in shopify cart. 
		/// </summary>
		/// <returns></returns>
		[RequireSubscription]
		public async System.Threading.Tasks.Task<ActionResult> Index()
		{
			var cookie = Request.Cookies.Get(".App.Handshake.ShopUrl");
			//Pass the cookie's value to the viewbag
			string shop = cookie?.Value;

			string userName = shop;
			if(string.IsNullOrEmpty(userName))
			{
				userName = User.Identity.Name;
			}

			var usermanager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
			var user = await usermanager.FindByNameAsync(userName);
			string domain = user.MyShopifyDomain;
			string token = user.ShopifyAccessToken;

			if (string.IsNullOrEmpty(shop))
			{
				return Redirect(domain + "/admin/apps/first-x-of-orders-get-y");
			}

			var productservice = new ShopifyProductService(shop, token);
			IEnumerable<ShopifyProduct> products = await productservice.ListAsync();

			var ordersService = new ShopifyOrderService(shop, token);
			IEnumerable<ShopifyOrder> orders = await ordersService.ListAsync();

			ViewBag.Product = products;
			ViewBag.ShopName = domain;
			ViewBag.Api = ApplicationEngine.ShopifyApiKey;

			int orderCount = orders.Count();

			//Optionally filter the results
			//var filter = new ShopifyProductFilterOptions()
			//{
			//	Ids = new[]
			//	{
			//		productId1,
			//		productId2,
			//		productId3
			//	}
			//};
			//products = await service.ListAsync(filter);

			return View();
		}
	}
}