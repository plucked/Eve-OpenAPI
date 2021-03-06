using EveOpenApi;
using EveOpenApi.Enums;
using EveOpenApi.Api;
using EveOpenApi.Interfaces;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Tests
{
	public class EveLoginTests
	{
		EveLogin login;
		API esi;

		[SetUp]
		public void Setup()
		{
			SetupAsync().GetAwaiter().GetResult();
		}

		async Task SetupAsync()
		{
			const string EnvironmentVariable = "TestDataDirectory";
			string testDataDir = Environment.GetEnvironmentVariable(EnvironmentVariable);

			login = await EveLogin.FromFile(testDataDir + "/Save.json");
			//await login.SaveToFile(testDataDir + "/Save.json");
			//esi = await ESI.CreateVersioned(EsiVersion.Latest, Datasource.Tranquility, login);
			esi = API.CreateEsi(EsiVersion.Latest, Datasource.Tranquility, login);

			esi.ChangeLogin(login);
		}

		[Test]
		public void Scope()
		{
			Scope scope1 = "esi-calendar.respond_calendar_events.v1";
			Scope scope11 = "esi-calendar.respond_calendar_events.v1";
			Scope scope2 = "esi-calendar.read_calendar_events.v1";
			Scope scope3 = "esi-location.read_location.v1";

			string scopeString = $"{scope1} {scope2}";
			Scope scope = scopeString;

			Assert.AreEqual(scope1, scope11);
			Assert.AreEqual(scopeString, scope.ScopeString);
			Assert.IsTrue(scope.Scopes[0] == scope1);
			Assert.IsTrue(scope.Scopes[1] == scope2);

			scope.AddScope(scope3);

			Assert.IsTrue(scope.Scopes[2] == scope3);

			scope.RemoveScope(scope2);

			Assert.IsTrue(scope.Scopes[1] == scope3);

			Assert.IsFalse(scope.IsSubset(scope2));
			Assert.IsTrue((string)scope == $"{scope1} {scope3}");

			try
			{
				scope.RemoveScope(scope2);
				Assert.Fail();
			}
			catch (Exception)
			{
				Assert.Pass();
			}
		}

		[Test]
		public async Task Token()
		{
			Assert.IsTrue(login.TryGetToken("Prople Dudlestreis", (Scope)"", out IToken token));
			Assert.IsFalse(login.TryGetToken("Prople Dudlestreis", (Scope)"esi-location.read_location.v1", out token));

			Assert.IsTrue(login.TryGetToken("Prople Dudlestreis", (Scope)"esi-mail.read_mail.v1", out token));

			Assert.IsTrue(token.Scope.IsSubset((Scope)"esi-mail.read_mail.v1"));

			await token.RefreshToken((Scope)"");

			Assert.IsFalse(token.Scope.IsSubset((Scope)"esi-mail.read_mail.v1"));

			await token.GetToken();

			List<string> users = login.GetUsers();
			Assert.AreEqual(users.Count, 1);
		}

		[Test]
		public void PathFail()
		{
			try
			{
				esi.Path("/characters/{character_id1}/mail/");
				Assert.Fail();
			}
			catch (Exception)
			{
				Assert.Pass();
			}
		}

		[Test]
		public async Task PathSuccsess()
		{
			ApiPath path = esi.Path("/alliances/");

			ApiResponse response = await path.Get();
			List<string> alliances = (await path.Get<List<string>>()).Response;

			Assert.AreEqual("1354830081", alliances[0]);

			ApiPath aPath = esi.Path("/alliances/{alliance_id}/");
			await aPath.Get<dynamic>(("alliance_id", alliances[0]));
			await aPath.GetBatch<dynamic>(("alliance_id", new List<object> { alliances[0], alliances[1] }));

			Assert.Pass();
		}
	}
}