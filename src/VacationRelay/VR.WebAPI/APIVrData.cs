using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VR.Context;

namespace VR.WebAPI;

public static class APIVrData
{
    public static void MapVrData(this WebApplication app)
    {
        var vrDataGroup = app.MapGroup("/api/vrdata");

        vrDataGroup.MapGet("/", async (
            VacationrelayContext db,
            [FromQuery] string? idPersonInVacation,
            [FromQuery] string? idPersonReplacing,
            [FromQuery] string? idPersonReplace,
            CancellationToken ct) =>
        {
            var query = db.VrData.AsQueryable();

            if (!string.IsNullOrWhiteSpace(idPersonInVacation))
            {
                query = query.Where(data => data.IdpersonInVacation.Contains(idPersonInVacation));
            }

            var replaceFilter = string.IsNullOrWhiteSpace(idPersonReplacing) ? idPersonReplace : idPersonReplacing;
            if (!string.IsNullOrWhiteSpace(replaceFilter))
            {
                query = query.Where(data => data.IdpersonReplace.Contains(replaceFilter));
            }

            var result = await query
                .AsNoTracking()
                .ToArrayAsync(ct);

            return Results.Ok(result);
        });

        vrDataGroup.MapGet("/{id:guid}", async (VacationrelayContext db, Guid id, CancellationToken ct) =>
        {
            var entity = await db.VrData
                .AsNoTracking()
                .FirstOrDefaultAsync(data => data.Id == id, ct);

            return entity is null ? Results.NotFound($"Entity with ID {id} not found.") : Results.Ok(entity);
        });

        vrDataGroup.MapPost("/", async (VacationrelayContext db, [FromBody] VrData data, CancellationToken ct) =>
        {
            var errors = new Dictionary<string, string[]>();
            if (string.IsNullOrWhiteSpace(data.IdpersonInVacation))
            {
                errors[nameof(data.IdpersonInVacation)] = ["The person in vacation is required."];
            }
            if (string.IsNullOrWhiteSpace(data.IdpersonReplace))
            {
                errors[nameof(data.IdpersonReplace)] = ["The replacement person is required."];
            }
            if (data.FromDate == default)
            {
                errors[nameof(data.FromDate)] = ["A valid from date is required."];
            }
            if (data.ToDate is not null && (data.ToDate == default(DateTimeOffset) || data.ToDate < data.FromDate))
            {
                errors[nameof(data.ToDate)] = ["The to date must be on or after the from date."];
            }
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            db.VrData.Add(data);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/vrdata/{data.Id}", data);
        });

        //vrDataGroup.MapPut("/{id:guid}", async (VacationrelayContext db, Guid id, [FromBody] VrData input, CancellationToken ct) =>
        //{
        //    var entity = await db.VrData.FirstOrDefaultAsync(data => data.Id == id, ct);
        //    if (entity is null)
        //    {
        //        return Results.NotFound($"Entity with ID {id} not found.");
        //    }

        //    entity.IdpersonInVacation = input.IdpersonInVacation;
        //    entity.IdpersonReplace = input.IdpersonReplace;
        //    entity.FromDate = input.FromDate;
        //    entity.ToDate = input.ToDate;

        //    await db.SaveChangesAsync(ct);
        //    return Results.Ok(entity);
        //});

        vrDataGroup.MapDelete("/{id:guid}", async (VacationrelayContext db, Guid id, CancellationToken ct) =>
        {
            var entity = await db.VrData.FirstOrDefaultAsync(data => data.Id == id, ct);
            if (entity is null)
            {
                return Results.NotFound($"Entity with ID {id} not found.");
            }
             
            db.VrData.Remove(entity);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }
}
