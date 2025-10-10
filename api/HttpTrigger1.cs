using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WebPush;

namespace Company.Function;

public class HttpTrigger1
{
    private readonly ILogger<HttpTrigger1> _logger;
    private static readonly string VAPID_PUBLIC_KEY = "BCrMZpWrJhviBTe76eDmqd9kOGxnHZeIS-iPNGBvd6KjhcLlN6jIprlXLJ519j3B3QybhoNxx3d_AzC-zKiigec";
    private static readonly string VAPID_PRIVATE_KYE = "Ve_SCxfZxHNI5ElUXP4suC30mBqM9PizvAWxdWGMcSI";
    private static readonly string VAPID_SUBJECT = "mailto:verlorenesiege@gmail.com";

    public HttpTrigger1(ILogger<HttpTrigger1> logger)
    {
        _logger = logger;
    }

    [Function("save-subscription")]
    public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        _logger.LogInformation("requestBody : " + requestBody);

        try
        {
            Subscribe sub = JsonConvert.DeserializeObject<Subscribe>(requestBody);

            _logger.LogInformation("Endpoint : " + sub.endpoint);
            _logger.LogInformation("Keys.p256dh : " + sub.keys.p256dh);
            _logger.LogInformation("Keys.auth : " + sub.keys.p256dh);

            var subscription = new PushSubscription(sub.endpoint, sub.keys.p256dh, sub.keys.p256dh);

            var options = new Dictionary<string, object>();
            options["vapidDetails"] = new VapidDetails(VAPID_SUBJECT, VAPID_PUBLIC_KEY, VAPID_PRIVATE_KYE);
            //options["gcmAPIKey"] = @"[your key here]";

            var webPushClient = new WebPushClient();
            await webPushClient.SendNotificationAsync(subscription, "payload", options);


        } catch (WebPushException exception)
        {
            _logger.LogInformation(exception.Message);

        }
        catch (Exception e)
        {
            _logger.LogInformation(e.Message);
        }
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
    class Subscribe { 

        public string? endpoint { get; set; }
        public string? expirationTime { get; set; }    
        public Keys? keys { get; set; }
        
    }

    class Keys {
        public string? p256dh { get; set; }
        public string? auth { get; set; }
    }

}