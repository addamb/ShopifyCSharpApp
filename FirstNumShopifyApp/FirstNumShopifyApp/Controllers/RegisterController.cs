using FirstNumShopifyApp.Engines;
using FirstNumShopifyApp.Models;
using Microsoft.AspNet.Identity.Owin;
using ShopifySharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace FirstNumShopifyApp.Controllers
{
    public class RegisterController : Controller
    {
		/// <summary>
		/// Index
		/// We setup the shops account here.
		/// </summary>
		/// <returns></returns>
		public async Task<ActionResult> Index()
		{
			//Get Cookie and setup account.
			var cookie = Request.Cookies.Get(".App.Handshake.ShopUrl");
			//Pass the cookie's value to the viewbag
			string shopUrl = cookie?.Value;
			if (!string.IsNullOrEmpty(shopUrl) && ModelState.IsValid)
			{
				var userManager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
				string pass = Membership.GeneratePassword(12, 1);
				var user = new ApplicationUser { UserName = shopUrl, Email = "temp@" + shopUrl };
				//Try to create the new account
				var create = await userManager.CreateAsync(user, pass);
				if (create.Succeeded)
				{
					await HttpContext.GetOwinContext().Get<ApplicationSignInManager>()
					.SignInAsync(user, true, true);
					//User must now connect their Shopify shop
					return RedirectToAction("Connect", new { shopUrl });
				}
				else
				{
					foreach (var error in create.Errors)
					{
						ModelState.AddModelError("", error);
					}
				}
			}
			else
			{
				return RedirectToAction("Connect", new { string.Empty });
			}

			//We should never get here. 
			return View();
		}

		/// <summary>
		/// Connect
		/// This is currently not needed. But will be needed if not
		/// installing the app from the shopify app store.
		/// </summary>
		/// <returns></returns>
		//[Authorize]
		//public ActionResult Connect()
		//{
		//	//If the user came from the Shopify app store, we'll have stored their shop URL in a cookie.
		//	//Try to pull in that cookie value so we can autofill the form on this page.
		//	var cookie = Request.Cookies.Get(".App.Handshake.ShopUrl");
		//	//Pass the cookie's value to the viewbag
		//	ViewBag.ShopUrl = cookie?.Value;
		//	return View();
		//}

		/// <summary>
		/// Index - Post
		/// This is here to help automate account creation
		/// without any outside input needed 
		/// This assumes that they are installing from the 
		/// shopify appstore
		/// </summary>
		/// <param name="model"></param>
		/// <returns></returns>
		[HttpPost, ValidateAntiForgeryToken]
		public async Task<ActionResult> Index(RegisterViewModel model)
		{
			if (ModelState.IsValid)
			{
				var userManager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
				var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
				//Try to create the new account
				var create = await userManager.CreateAsync(user, model.Password);
				if (create.Succeeded)
				{
					await HttpContext.GetOwinContext().Get<ApplicationSignInManager>()
					.SignInAsync(user, true, true);
					//User must now connect their Shopify shop
					return RedirectToAction("Connect");
				}
				else
				{
					foreach (var error in create.Errors)
					{
						ModelState.AddModelError("", error);
					}
				}
			}
			// If we got this far, something failed. Return the view with the same model to redisplay form.
			return View(model);
		}

		/// <summary>
		/// Connect
		/// This installs the app to the store with the neede permissions. 
		/// </summary>
		/// <param name="shopUrl"></param>
		/// <returns>Authentication that app was installed correctly.</returns>
		public async Task<ActionResult> Connect(string shopUrl)
		{
			if (!await ShopifyAuthorizationService.IsValidMyShopifyUrl(shopUrl))
			{
				ModelState.AddModelError("", "The URL you entered is not a valid *.myshopify.com URL.");
				//Preserve the user's shopUrl so they don't have to type it in again.
				ViewBag.ShopUrl = shopUrl;
				return View();
			}
			//Determine the permissions that your app will need and request them here.
			var permissions = new List<string>()
				{
				"read_orders",
				"write_orders",
				"read_products",
				"write_products"
				};

			//Prepare the redirect URL. This is case-sensitive and must match a redirection URL in
			//your Shopify app's settings.
			string redirectUrl = string.Format("{0}/shopify/authresult", ApplicationEngine.ShopifyAppUrl);
			//Build the authorization URL
			var authUrl = ShopifyAuthorizationService
			.BuildAuthorizationUrl(permissions, shopUrl, ApplicationEngine.ShopifyApiKey, redirectUrl);
			//Redirect the user to the authorization URL
			return Redirect(authUrl.ToString());
		}		/// <summary>
		/// Charge
		/// This is where we create the charge for the app
		/// Removed Test = true when out of testing.
		/// </summary>
		/// <returns>Charge Confirmation</returns>		[Authorize]
		public async Task<ActionResult> Charge()
		{
			//Grab the user object
			var usermanager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
			var user = await usermanager.FindByNameAsync(User.Identity.Name);
			string domain = user.MyShopifyDomain;
			string token = user.ShopifyAccessToken;
			//Ensure the user has connected a Shopify store
			if (string.IsNullOrEmpty(token))
			{
				return RedirectToAction("Connect");
			}
			//Build a test Shopify charge with a free, 14-day trial
			var charge = new ShopifyRecurringCharge()
			{
				Name = "Pro Plan",
				Price = 29.95,
				TrialDays = 14,
				Test = true,
				ReturnUrl = string.Format("{0}/shopify/chargeresult", ApplicationEngine.ShopifyAppUrl)
			};
			//Create the charge
			charge = await new ShopifyRecurringChargeService(domain, token).CreateAsync(charge);
			//Redirect the user to accept the charge.
			return Redirect(charge.ConfirmationUrl);
		}
	}
}