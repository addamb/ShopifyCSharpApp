using FirstNumShopifyApp.Engines;
using FirstNumShopifyApp.Models;
using Microsoft.AspNet.Identity.Owin;
using ShopifySharp;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace FirstNumShopifyApp.Controllers
{
    public class ShopifyController : Controller
    {
		/// <summary>
		/// Handshake
		/// This is the first call that Shopify makes. 
		/// This will determine if the shop is new or not.
		/// </summary>
		/// <param name="shop"></param>
		/// <returns></returns>
		public async Task<ActionResult> Handshake(string shop)
		{
			//Store the shop URL in a cookie.
			Response.SetCookie(new HttpCookie(".App.Handshake.ShopUrl", shop)
			{
				Expires = DateTime.Now.AddDays(30)
			});
			//Open a connection to our database
			using (var db = new ApplicationDbContext())
			{
				//Check if any user in the database has already connected this shop.
				if (await db.Users.AnyAsync(s => s.MyShopifyDomain == shop))
				{
					//This shop already exists, and the user is trying to log in.
					//Redirect them to the dashboard.
					return RedirectToAction("Index", "Dashboard", new { shop });
				}
				else
				{
					//This shop does not exist, and the user is trying to install the app.
					//Redirect them to registration.
					return RedirectToAction("Index", "Register");
				}
			}
		}

		/// <summary>
		/// AuthResut
		/// This will Validate the Shop and add needed 
		/// webhooks
		/// </summary>
		/// <param name="shop"></param>
		/// <param name="code"></param>
		/// <returns></returns>
		[Authorize]
		public async Task<ActionResult> AuthResult(string shop, string code)
		{
			string apiKey = ApplicationEngine.ShopifyApiKey;
			string secretKey = ApplicationEngine.ShopifySecretKey;
			//Validate the signature of the request to ensure that it's valid
			if (!ShopifyAuthorizationService.IsAuthenticRequest(Request.QueryString, secretKey))
			{
				//The request is invalid and should not be processed.
				throw new Exception("Request is not authentic.");
			}
			else
			{
				//The request is valid. Exchange the temporary code for a permanent access token
				string accessToken;
				try
				{
					accessToken = await ShopifyAuthorizationService.Authorize(code, shop, apiKey, secretKey);
				}
				catch (ShopifyException e)
				{
					// Failed to authorize app installation.
					// TODO: Log or handle exception in whatever way you see fit.
					throw e;
				}

				//Get the Shop Info
				var service = new ShopifyShopService(shop, accessToken);
				var shopInfo = await service.GetAsync();

				//Get the user
				var usermanager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
				var user = await usermanager.FindByNameAsync(User.Identity.Name);
				user.Email = shopInfo.Email;
				user.ShopifyAccessToken = accessToken;
				user.MyShopifyDomain = shop;

				/*Create Web hooks needed for the Application*/
				//Create the AppUninstalled webhook
				var webHookUninstallService = new ShopifyWebhookService(shop, accessToken);
				var hook = new ShopifyWebhook()
				{
					Address = "https://2043bb28.ngrok.io/webhooks/appuninstalled?userId=" + user.Id,
					CreatedAt = DateTime.Now,
					Format = "json",
					Topic = "app/uninstalled",
				};

				//Create Create cart webhook
				var cartHook = new ShopifyWebhook()
				{
					Address = "https://2043bb28.ngrok.io/webhooks/CreateOrder",
					CreatedAt = DateTime.Now,
					Format = "json",
					Topic = "carts/create",
				};

				try
				{
					hook = await webHookUninstallService.CreateAsync(hook);
					cartHook = await webHookUninstallService.CreateAsync(cartHook);
				}
				catch (ShopifyException e)
				{
					// TODO: Log or handle exception in whatever way you see fit.

					throw e;
				}

				var update = await usermanager.UpdateAsync(user);
				if (!update.Succeeded)
				{
					// TODO: Log or handle exception in whatever way you see fit.
					string message = "Couldn't save a user's access token and shop domain. Reason: " +
					string.Join(", ", update.Errors);
					throw new Exception(message);
				}
				return RedirectToAction("Charge", "Register");
			}
		}

		/// <summary>
		/// ChargeResult
		/// This get thes the charge. 
		/// </summary>
		/// <param name="shop"></param>
		/// <param name="charge_id"></param>
		/// <returns></returns>
		[Authorize]
		public async Task<ActionResult> ChargeResult(string shop, long charge_id)
		{
			//Get the user
			var usermanager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
			var user = await usermanager.FindByNameAsync(User.Identity.Name);
			//Create the billing service, which will be used to pull in the charge id
			var service = new ShopifyRecurringChargeService(user.MyShopifyDomain, user.ShopifyAccessToken);
			ShopifyRecurringCharge charge;
			//Try to get the charge. If a "404 Not Found" exception is thrown, the charge has been deleted.
			try
			{
				charge = await service.GetAsync(charge_id);
			}
			catch (ShopifyException e)
			when ((int)e.HttpStatusCode == 404 /* Not found */)
			{
				//The charge has been deleted. Redirect the user to accept a new charge.
				return RedirectToAction("Charge", "Register");
			}
			//Ensure the charge can be activated
			if (charge.Status != "accepted")
			{
				//Charge has not been accepted. Redirect the user to accept a new charge.
				return RedirectToAction("Charge", "Register");
			}
			//Activate the charge
			await service.ActivateAsync(charge_id);
			//Save the charge to the user model
			user.ShopifyChargeId = charge_id;
			var update = await usermanager.UpdateAsync(user);
			if (!update.Succeeded)
			{
				// TODO: Log or handle exception in whatever way you see fit.
				string message = "Couldn't save a user's activated charge id. Reason: " +
				string.Join(", ", update.Errors);
				throw new Exception(message);
			}
			//User's subscription charge has been activated and they can now use the app.
			return RedirectToAction("Index", "Dashboard");
		}
	}
}