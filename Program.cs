using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Эндпоинт /health
app.MapGet("/health", () => new {
    status = "ok",
    currentTime = DateTime.Now
});

// GET /db/ping — проверка связи с БД
app.MapGet("/db/ping", async (IConfiguration conf) =>
{
    var connectionString = conf.GetConnectionString("Mssql");

    using var connection = new SqlConnection(connectionString);
    try
    {
        await connection.OpenAsync();
        return Results.Ok(new { status = "ok", message = "Соединение с БД установлено" });
    }
    catch (Exception ex)
    {
        // На данном этапе это ожидаемый результат, так как БД еще не создана
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500,
            title: "Ошибка подключения к БД"
        );
    }
});


// Хранилище заметок в оперативной памяти
var notes = new List<Note>();
var nextId = 1;

// GET /api/notes — Получить все заметки
app.MapGet("/api/notes", () => notes);

// GET /api/notes/{id} — Получить одну заметку
app.MapGet("/api/notes/{id:int}", (int id) =>
    notes.FirstOrDefault(n => n.Id == id) is Note note
        ? Results.Ok(note)
        : Results.NotFound(new { message = "Заметка не найдена" }));

// POST /api/notes — Создать заметку
app.MapPost("/api/notes", (CreateNoteDto dto) =>
{
    // Минимальная валидация
    if (string.IsNullOrWhiteSpace(dto.Title))
        return Results.BadRequest(new { error = "Заголовок не может быть пустым" });

    var newNote = new Note(nextId++, dto.Title, dto.Text, DateTime.Now);
    notes.Add(newNote);
    return Results.Created($"/api/notes/{newNote.Id}", newNote);
});

// DELETE /api/notes/{id} — Удалить заметку
app.MapDelete("/api/notes/{id:int}", (int id) =>
{
    var note = notes.FirstOrDefault(n => n.Id == id);
    if (note == null) return Results.NotFound();

    notes.Remove(note);
    return Results.NoContent();
});



// Эндпоинт /version
app.MapGet("/version", (IConfiguration conf) => new {
    name = conf["App:Name"],
    version = conf["App:Version"]
});


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}




// четвертое задание я схожу с ума
// Модель заметки
public record Note(int Id, string Title, string Text, DateTime CreatedAt);

// Класс для создания заметки (валидация входных данных)
public record CreateNoteDto(string Title, string Text);
