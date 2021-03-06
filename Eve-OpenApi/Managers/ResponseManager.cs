﻿using EveOpenApi.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EveOpenApi.Managers
{
	internal class ResponseManager : BaseManager
	{
		int errorRemain = 100;
		DateTime errorReset;

		public ResponseManager(HttpClient client, API api) : base(client, api)
		{
		}

		public async Task<ApiResponse> GetResponse(ApiRequest request, int index)
		{
			HttpResponseMessage response = await GetHttpResponse(request, index);
			ApiResponse esiResponse = await GetEsiResponse(response);

			CheckRateLimit(response);
			return esiResponse;
		}

		public async Task<ApiResponse<T>> GetResponse<T>(ApiRequest request, int index)
		{
			ApiResponse esiResponse = await GetResponse(request, index);
			return esiResponse.ToType<T>();
		}

		async Task<HttpResponseMessage> GetHttpResponse(ApiRequest request, int index)
		{
			Uri requestUri = new Uri(request.GetRequestUrl(index));
			HttpRequestMessage requestMessage = new HttpRequestMessage(request.Method, requestUri);

			foreach (var item in request.Parameters.Headers)
				requestMessage.Headers.TryAddWithoutValidation(item.Key, item.Value);

			// Throttle requests if users send too many errors.
			if (errorRemain <= 0 && errorReset > DateTime.Now)
			{
				if (API.Config.RateLimitThrotle)
					await Task.Delay(errorReset - DateTime.Now);
				else
					throw new Exception("Rate limit reached.");
			}

			return await Client.SendAsync(requestMessage);
		}

		async Task<ApiResponse> GetEsiResponse(HttpResponseMessage response)
		{
			string eTag = TryGetHeaderValue(response.Headers, "etag");
			string expires = TryGetHeaderValue(response.Content.Headers, "expires");
			string cacheControl = TryGetHeaderValue(response.Content.Headers, "cache-control");
			string json = await response.Content.ReadAsStringAsync();

			DateTime parsedExpiery;
			if (!string.IsNullOrEmpty(expires))
				parsedExpiery = DateTime.ParseExact(expires, "ddd, dd MMM yyyy HH:mm:ss 'GMT'", System.Globalization.CultureInfo.InvariantCulture);
			else
				parsedExpiery = default;

			switch (response.StatusCode)
			{
				case HttpStatusCode.OK:
					return new ApiResponse(eTag, json, parsedExpiery, cacheControl);
				default:
					return new ApiError(eTag, json, parsedExpiery, cacheControl, response.StatusCode);
			}
		}

		void CheckRateLimit(HttpResponseMessage response)
		{
			string errorRemainString = TryGetHeaderValue(response.Headers, "x-esi-error-limit-remain");
			string errorResetString = TryGetHeaderValue(response.Headers, "x-esi-error-limit-reset");

			int.TryParse(errorRemainString, out errorRemain);
			int.TryParse(errorResetString, out int errorResetTime);
			errorReset = DateTime.Now + new TimeSpan(0, 0, errorResetTime);
		}

		string TryGetHeaderValue(HttpHeaders header, string name)
		{
			if (header.TryGetValues(name, out IEnumerable<string> list))
				return list.FirstOrDefault();

			return "";
		}
	}
}
