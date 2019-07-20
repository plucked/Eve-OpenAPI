﻿using System;
using System.Collections.Generic;
using System.Text;

namespace EveOpenApi.Authentication
{
	public class LoginConfig : ILoginConfig
	{
		public string AuthenticationEndpoint { get; set; }

		public string TokenEndpoint { get; set; }

		public string JwtKeySetEndpoint { get; set; }

		public string AuthType { get; set; }

		public string TokenLocation { get; set; }

		public string TokenName { get; set; }

		public string RateLimitHeader { get; set; }

		public string RateLimitRemainHeader { get; set; }

		public string RateLimitResetHeader { get; set; }

		public static LoginConfig Eve
		{
			get
			{
				return new LoginConfig()
				{
					AuthenticationEndpoint = "https://login.eveonline.com/v2/oauth/authorize/",
					TokenEndpoint = "https://login.eveonline.com/v2/oauth/token/",
					JwtKeySetEndpoint = "https://login.eveonline.com/oauth/jwks",
					AuthType = "basic",

					TokenLocation = "query",
					TokenName = "token",
					RateLimitRemainHeader = "x-esi-error-limit-remain",
					RateLimitResetHeader = "x-esi-error-limit-reset"
				};
			}
		}
	}
}