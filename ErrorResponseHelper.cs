using CurseForge.APIClient.Models;

namespace ModpackDownloadAPI
{
    public static class ErrorResponseHelper
    {
        public static IResult CreateAPIResponse(this ErrorResponse response)
        {
            return Results.Problem(detail: response.ErrorMessage, statusCode: response.ErrorCode);
        }
    }
}
