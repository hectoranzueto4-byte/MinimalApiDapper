using Dapper;
using Npgsql;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar la conexión a PostgreSQL
builder.Services.AddScoped<IDbConnection>(sp =>
    new NpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Configurar Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3. Construir la aplicación (esta es la línea que faltaba)
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 4. Endpoints CRUD
app.MapGet("/clientes", async (IDbConnection db) =>
{
    var sql = "SELECT Id, Nombre, Correo FROM Clientes";
    var clientes = await db.QueryAsync<Cliente>(sql);
    return Results.Ok(clientes);
});

app.MapGet("/clientes/{id}", async (int id, IDbConnection db) =>
{
    var sql = "SELECT Id, Nombre, Correo FROM Clientes WHERE Id = @Id";
    var cliente = await db.QueryFirstOrDefaultAsync<Cliente>(sql, new { Id = id });
    return cliente is not null ? Results.Ok(cliente) : Results.NotFound();
});

app.MapPost("/clientes", async (Cliente cliente, IDbConnection db) =>
{
    var sql = @"INSERT INTO Clientes (Nombre, Correo) VALUES (@Nombre, @Correo)
                RETURNING Id;";
    var id = await db.QuerySingleAsync<int>(sql, cliente);
    cliente.Id = id;
    return Results.Created($"/clientes/{id}", cliente);
});

app.MapPut("/clientes/{id}", async (int id, Cliente cliente, IDbConnection db) =>
{
    var sql = "UPDATE Clientes SET Nombre = @Nombre, Correo = @Correo WHERE Id = @Id";
    cliente.Id = id;
    var filasAfectadas = await db.ExecuteAsync(sql, cliente);
    return filasAfectadas > 0 ? Results.NoContent() : Results.NotFound();
});

app.MapDelete("/clientes/{id}", async (int id, IDbConnection db) =>
{
    var sql = "DELETE FROM Clientes WHERE Id = @Id";
    var filasAfectadas = await db.ExecuteAsync(sql, new { Id = id });
    return filasAfectadas > 0 ? Results.NoContent() : Results.NotFound();
});

app.Run();
