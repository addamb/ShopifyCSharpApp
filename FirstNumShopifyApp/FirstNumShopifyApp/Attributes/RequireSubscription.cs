using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace FirstNumShopifyApp.Attributes
{
	public class RequireSubscription : AuthorizeAttribute
	{
		public override void OnAuthorization(AuthorizationContext filterContext)
		{
			//Let the attribute preform its default authorization, ensuring the user is logged in.
			base.OnAuthorization(filterContext);
			//If default authorization failed, filterContext.Result will be set.
			//Ensure it's null before continuing.
			if (filterContext.Result == null)
			{
				var context = filterContext.HttpContext;
				var usermanager = context.GetOwinContext().GetUserManager<ApplicationUserManager>();
				var user = usermanager.FindByNameAsync(context.User.Identity.Name).Result;

				if (user.ShopifyChargeId.HasValue && string.IsNullOrEmpty(user.ShopifyAccessToken) == false)
				{
					//Assume subscription is valid.
				}
				else if (string.IsNullOrEmpty(user.ShopifyAccessToken) == false)
				{
					//User has connected their Shopify shop, but they haven't accepted a subscription charge.
					filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary() {
						{ "controller", "Register" },
						{ "action", "Charge" }
						});
				}
				else
				{
					//User has created an account, but they haven't connected their Shopify shop.
					filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary() {
						{ "controller", "Register" },
						{ "action", "Connect" }
						});
				}
			}
		}
	}
}