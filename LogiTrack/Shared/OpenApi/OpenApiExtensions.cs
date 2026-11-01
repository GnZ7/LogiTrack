namespace LogiTrack.Shared.OpenApi
{
    public static class OpenApiExtensions
    {
        public static WebApplication ConfigureSwaggerUI(this WebApplication app)
        {
            app.MapOpenApi();            

            app.UseSwaggerUI(options =>
            {
                options.DocumentTitle = "LogiTrack API Documentation";
                options.SwaggerEndpoint("/openapi/v1.json", "LogiTrack API V1");
                options.RoutePrefix = string.Empty;
            });

            return app;
        }
    }
}
