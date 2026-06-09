using Galvao.Presentation.Configurations;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddPresentationServices(builder.Configuration);

var app = builder.Build();
app.Configure();
app.Run();

public partial class Program { }
