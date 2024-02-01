using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class Startup
{
  private readonly IMongoCollection<BsonDocument> _paymentCollection;
  private readonly HttpClient _httpClient;
  private readonly string _userServiceBaseUrl;

  public Startup()
  {
    // MongoDB setup
    var client = new MongoClient(Environment.GetEnvironmentVariable("DB_HOST"));
    var database = client.GetDatabase("paymentsdb");
    _paymentCollection = database.GetCollection<BsonDocument>("payment");

    // Demo: Insert documents if the collection is empty
    if (_paymentCollection.CountDocuments(new BsonDocument()) == 0)
    {
      _paymentCollection.InsertOne(new BsonDocument { { "from", 1 }, { "to", 2 }, { "amount", 100 }, { "currency", "$" } });
      _paymentCollection.InsertOne(new BsonDocument { { "from", 1 }, { "to", 2 }, { "amount", 200 }, { "currency", "$" } });
      _paymentCollection.InsertOne(new BsonDocument { { "from", 2 }, { "to", 1 }, { "amount", 100 }, { "currency", "$" } });
    }

    // HTTP Client setup
    _httpClient = new HttpClient();
    _userServiceBaseUrl = Environment.GetEnvironmentVariable("USERS_SERVICE");
  }

  public void Configure(IApplicationBuilder app)
  {
    app.UseRouting();

    app.UseEndpoints(endpoints =>
    {
      endpoints.MapGet("/ping", async context =>
          {
            await context.Response.WriteAsync("pong");
          });

      endpoints.MapGet("/payments_from/{userid:int}", async context =>
          {
            var userid = int.Parse(context.Request.RouteValues["userid"].ToString());
            var paymentsList = _paymentCollection.Find(Builders<BsonDocument>.Filter.Eq("from", userid)).ToList();

            // Fetch user details from external service
            var userResponse = await _httpClient.GetAsync($"http://{_userServiceBaseUrl}/user/{userid}");
            var userDetails = await userResponse.Content.ReadAsStreamAsync();

            var paymentDetails = new
            {
              Version = "v1",
              User = await JsonSerializer.DeserializeAsync<object>(userDetails),
              Payments = paymentsList.ConvertAll(BsonTypeMapper.MapToDotNetValue)
            };

            await context.Response.WriteAsJsonAsync(paymentDetails);
          });
    });
  }
}

public class Program
{
  public static Task Main(string[] args) =>
      Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
        .Build().RunAsync();
}
