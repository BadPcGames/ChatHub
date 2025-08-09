using ChatHub.Logic;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddSingleton<ChatMessageStore>(); 
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowed(_ => true)); 
});

builder.Services.AddSingleton<ChatMessageStore>();

var app = builder.Build();
app.UseCors();

app.MapGet("/", () => "Chat SignalR server running");

app.MapGet("/messages", (ChatMessageStore store) => Results.Ok(store.GetAllMessages()));

app.MapHub<ChatHub.Logic.ChatHub>("/chathub");

app.Run();
