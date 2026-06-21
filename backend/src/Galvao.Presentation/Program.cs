using Galvao.Presentation.Configurations;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddPresentationServices(builder.Configuration);

var app = builder.Build();
app.Configure();
app.Run();

#pragma warning disable ASP0027 
public partial class Program;
#pragma warning restore ASP0027 
