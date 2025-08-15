using System;
using System.Net;
using System.Text.Json;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using Foody.Models;

namespace Foody
{
    [TestFixture]
    public class FoodyTests
    {
        private RestClient client;
        private static string createdFoodId;

        private const string baseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:86";
        
        [OneTimeSetUp]
        public void Setup()
        {
            string token = GetJwtToken("Yoo", "123456");

            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };

            client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            var loginClient = new RestClient(baseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);

            request.AddJsonBody(new { username, password });
            var response = loginClient.Execute(request);

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);


            return json.GetProperty("accessToken").GetString() ?? string.Empty;
        }

        //Tests
        [Test, Order(1)]
        public void CreateFood_ShouldReturnCreated()
        {
            var food = new
            {
                Name = "New Food",
                Description = "Test food description",
                Url = ""
            };

            var request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(food);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            createdFoodId = json.GetProperty("foodId").GetString() ?? string.Empty;
            Assert.That(createdFoodId, Is.Not.Null.And.Not.Empty, "Food ID should no be null or empty");
        }

        [Test, Order(2)]
        public void EditFood_ShouldReturnOk()
        {
            var changes = new[]
            {
                new { path = "/name", op = "replace", value = "Updated Food Name" }
            };
            var patchRequest = new RestRequest($"/api/Food/Edit/{createdFoodId}", Method.Patch);
            patchRequest.AddJsonBody(changes);

            var response = client.Execute(patchRequest);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var json = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(json.Msg, Is.EqualTo("Successfully edited"));
        }

        [Test, Order(3)]
        public void GetAllFoods_ShouldReturnOk()
        {
            var request = new RestRequest("/api/Food/All", Method.Get);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var foods = JsonSerializer.Deserialize<List<object>>(response.Content);
            Assert.That(foods, Is.Not.Null);
        }

        [Test, Order(4)]
        public void DeleteFood_ShouldReturnOk()
        {
            var request = new RestRequest($"/api/Food/Delete/{createdFoodId}", Method.Delete);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var json = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(json.Msg, Is.EqualTo("Deleted successfully!"));
        }

        [Test, Order(5)]
        public void CreateFood_MissingRequiredFields_ShouldReturnBadRequest()
        {
            var incompleteFood = new { Name = "", Description = "" };
            var request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(incompleteFood);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test, Order(6)]
        public void EditNonExistingFood_ShouldReturnNotFound()
        {
            var patchRequest = new RestRequest("/api/Food/Edit/invalidFoodId", Method.Patch);
            patchRequest.AddJsonBody(new object[]
            {
                new { path = "/name", op = "replace", value = "Non Existing" }
            });

            var response = client.Execute(patchRequest);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            var json = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(json.Msg, Is.EqualTo("No food revues..."));
        }

        [Test, Order(7)]
        public void DeleteNonExistingFood_ShouldReturnBadRequest()
        {
            var request = new RestRequest("/api/Food/Delete/this-id-does-not-exist", Method.Delete);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            var dto = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content!);
            Assert.That(dto?.Msg, Is.EqualTo("Unable to delete this food revue!"));
        }




        [OneTimeTearDown]
        public void CleanUp()
        {
            client?.Dispose();
        }
    }
}