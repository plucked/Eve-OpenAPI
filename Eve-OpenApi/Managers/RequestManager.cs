﻿using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EveOpenApi.Managers
{
	internal class RequestManager : BaseManager
	{
		public RequestManager(HttpClient client, ESI esiNet) : base(client, esiNet)
		{
		}

		/// <summary>
		/// Request multiple queries for the same path.
		/// </summary>
		/// <param name="path">Esi path</param>
		/// <param name="user">User preforming this query.</param>
		/// <param name="type">Operation Type.</param>
		/// <param name="parameters">Parameters supplide by the user.</param>
		/// <param name="operation">OpenAPI operation for this path.</param>
		/// <returns></returns>
		public async Task<List<EsiResponse>> RequestBatch(string path, string user, OperationType type, Dictionary<string, List<string>> parameters, OpenApiOperation operation)
		{
			EsiRequest request = GetRequest(path, user, type, parameters, operation);
			return await EsiNet.CacheManager.GetResponse(request);
		}

		/// <summary>
		/// Request multiple queries for the same path.
		/// </summary>
		/// <typeparam name="T">Esi response return type.</typeparam>
		/// <param name="path">Esi path</param>
		/// <param name="user">User preforming this query.</param>
		/// <param name="type">Operation Type.</param>
		/// <param name="parameters">Parameters supplide by the user.</param>
		/// <param name="operation">OpenAPI operation for this path.</param>
		/// <returns></returns>
		public async Task<List<EsiResponse<T>>> RequestBatch<T>(string path, string user, OperationType type, Dictionary<string, List<string>> parameters, OpenApiOperation operation)
		{
			EsiRequest request = GetRequest(path, user, type, parameters, operation);
			return await EsiNet.CacheManager.GetResponse<T>(request);
		}

		EsiRequest GetRequest(string path, string user, OperationType type, Dictionary<string, List<string>> parameters, OpenApiOperation operation)
		{
			var parsed = ParseParameters(operation, parameters);
			string esiBaseUrl = $"{EsiNet.Spec.Servers[0].Url}";
			string scope = GetScope(operation);
			HttpMethod httpMethod = OperationToMethod(type);

			return new EsiRequest(esiBaseUrl, path, user, scope, httpMethod, parsed);
		}

		/// <summary>
		/// Sort parameters into their respective group.
		/// </summary>
		/// <param name="operation"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		ParsedParameters ParseParameters(OpenApiOperation operation, Dictionary<string, List<string>> parameters)
		{
			int maxLength = 1;
			var queries = new List<KeyValuePair<string, List<string>>>();
			var headers = new List<KeyValuePair<string, List<string>>>();
			var pathParameters = new List<KeyValuePair<string, List<string>>>();

			foreach (var item in operation.Parameters)
			{
				bool found = parameters.TryGetValue(item.Name, out List<string> value);

				if (found)
				{
					if (maxLength == 1 && value.Count > maxLength)
						maxLength = value.Count;
					else if (maxLength > 1 && value.Count != maxLength)
						throw new Exception("Every batch parameter must have 1 or the same count");

					switch (item.In)
					{
						case ParameterLocation.Query:
							queries.Add(new KeyValuePair<string, List<string>>(item.Name, value));
							break;
						case ParameterLocation.Path:
							pathParameters.Add(new KeyValuePair<string, List<string>>(item.Name, value));
							break;
						case ParameterLocation.Header:
							headers.Add(new KeyValuePair<string, List<string>>(item.Name, value));
							break;
						default:
							break;
					}
				}
				else if (item.Required)
					throw new Exception($"Required parameter '{item.Name}' not supplied.");
			}

			return new ParsedParameters(maxLength, queries, headers, pathParameters);
		}

		/// <summary>
		/// Try get scope from operation.
		/// </summary>
		/// <param name="operation"></param>
		/// <returns></returns>
		string GetScope(OpenApiOperation operation)
		{
			List<string> scopes = operation.Security?.FirstOrDefault()?.FirstOrDefault().Value as List<string>;

			if (scopes != null && scopes.Count > 0)
				return scopes[0];

			return "";
		}

		/// <summary>
		/// Convert OperationType to HttpMethod
		/// </summary>
		/// <param name="operation"></param>
		/// <returns></returns>
		HttpMethod OperationToMethod(OperationType operation)
		{
			switch (operation)
			{
				case OperationType.Get:
					return HttpMethod.Get;
				case OperationType.Put:
					return HttpMethod.Put;
				case OperationType.Post:
					return HttpMethod.Post;
				case OperationType.Delete:
					return HttpMethod.Delete;
				case OperationType.Options:
					return HttpMethod.Options;
				case OperationType.Head:
					return HttpMethod.Head;
				case OperationType.Patch:
					return HttpMethod.Patch;
				case OperationType.Trace:
					return HttpMethod.Trace;
				default:
					throw new Exception("Dafuq");
			}
		}
	}
}