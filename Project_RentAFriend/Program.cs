var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMvc(option => option.EnableEndpointRouting = true);
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Version = "v1",
        Title = "Инструментарий"
    });
    string PathFile = Path.Combine(AppContext.BaseDirectory, "APIRentAFriend.xml");
    option.IncludeXmlComments(PathFile);
});
var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Инструментарий");
});
app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});
app.Run();