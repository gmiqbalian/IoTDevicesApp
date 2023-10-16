using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace AzureFunctions.Models;

public class MethodResponseMessage
{
    public static HttpResponseData CreateReponseMessage(HttpRequestData req, HttpStatusCode statusCode, string msg)
    {
        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
        response.WriteString(msg);

        return response;
    }
}
