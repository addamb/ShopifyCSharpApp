using FirstNumShopifyApp.Engines;
using Microsoft.AspNet.Identity.Owin;
using ShopifySharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace FirstNumShopifyApp.Controllers
{
    public class WebhooksController : Controller
    {
		/**************************************************************************************************************
		* Uninstalling an application also performs various cleanup tasks within Shopify. Registered Webhooks, 
		* ScriptTags and App Links will be destroyed as part of this operation. Also if an application is uninstalled 
		* during key rotation, both the old and new Access Tokens will be rendered useless.
		* 
		* For more onformation visit : https://help.shopify.com/api/guides/authentication/uninstalling-applications 
		***************************************************************************************************************/

		/// <summary>
		/// AppUninstalled will remove and clean up the application. 
		/// </summary>
		/// <param name="userId"></param>
		/// <returns></returns>
		public async Task<string> AppUninstalled(string userId)
		{

			if (!await ShopifyAuthorizationService.IsAuthenticWebhook(Request.Headers, Request.InputStream, ApplicationEngine.ShopifySecretKey))
			{
				throw new UnauthorizedAccessException("This request is not an authentic webhook request.");
			}

			//Pull in the user
			var usermanager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
			var user = await usermanager.FindByIdAsync(userId);

			//Remove User and Shop from database. 
			var remove = await usermanager.DeleteAsync(user);
			if (!remove.Succeeded)
			{
				// TODO: Log or handle exception in whatever way you see fit.
				string message = "Couldn't delete a user's Shopify details. Reason: " +
					string.Join(", ", remove.Errors);

				throw new Exception(message);
			}

			return "Successfully handled AppUninstalled webhook.";
		}

		/// <summary>
		/// This is an sample webhook.
		/// </summary>
		/// <returns></returns>
		public async Task<string> CreateOrder()
		{
			string requestBody = null;

			//Reset the input stream. MVC controllers often read the stream to determine which parameters to pass to an action.
			Request.InputStream.Position = 0;

			//Read the stream into a string
			using (StreamReader reader = new StreamReader(Request.InputStream))
			{
				requestBody = await reader.ReadToEndAsync();
			}

			if (!await ShopifyAuthorizationService.IsAuthenticWebhook(Request.Headers, Request.InputStream, ApplicationEngine.ShopifySecretKey))
			{
				throw new UnauthorizedAccessException("This request is not an authentic webhook request.");
			}

			//Pull in the user
			var usermanager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
			//var user = await usermanager.FindByIdAsync(userId);

			//Save changes

			//var remove = await usermanager.DeleteAsync(user);
			//if (!remove.Succeeded)
			//{
			//	// TODO: Log or handle exception in whatever way you see fit.
			//	string message = "Couldn't delete a user's Shopify details. Reason: " +
			//		string.Join(", ", remove.Errors);

			//	throw new Exception(message);
			//}

			return "Successfully handled AppUninstalled webhook.";
		}
	}
}