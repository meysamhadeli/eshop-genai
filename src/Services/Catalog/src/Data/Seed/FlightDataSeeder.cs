// using BuildingBlocks.EFCore;
// using Flight.Aircrafts.Models;
// using Flight.Airports.Models;
// using Flight.Flights.Models;
// using Flight.Seats.Models;
// using MapsterMapper;
// using Microsoft.EntityFrameworkCore;
// using MongoDB.Driver;
// using MongoDB.Driver.Linq;
//
// namespace Flight.Data.Seed;
//
// public class FlightDataSeeder(
//     CatalogDbContext catalogDbContext,
//     FlightReadDbContext flightReadDbContext,
//     IMapper mapper
// ) : IDataSeeder
// {
//     public async Task SeedAllAsync()
//     {
//         var pendingMigrations = await catalogDbContext.Database.GetPendingMigrationsAsync();
//
//         if (!pendingMigrations.Any())
//         {
//             await SeedAirportAsync();
//             await SeedAircraftAsync();
//             await SeedFlightAsync();
//             await SeedSeatAsync();
//         }
//     }
//
//     private async Task SeedAirportAsync()
//     {
//         if (!await EntityFrameworkQueryableExtensions.AnyAsync(catalogDbContext.Airports))
//         {
//             await catalogDbContext.Airports.AddRangeAsync(InitialData.Airports);
//             await catalogDbContext.SaveChangesAsync();
//
//             if (!await MongoQueryable.AnyAsync(flightReadDbContext.Airport.AsQueryable()))
//             {
//                 await flightReadDbContext.Airport.InsertManyAsync(mapper.Map<List<AirportReadModel>>(InitialData.Airports));
//             }
//         }
//     }
//
//     private async Task SeedAircraftAsync()
//     {
//         if (!await EntityFrameworkQueryableExtensions.AnyAsync(catalogDbContext.Aircraft))
//         {
//             await catalogDbContext.Aircraft.AddRangeAsync(InitialData.Aircrafts);
//             await catalogDbContext.SaveChangesAsync();
//
//             if (!await MongoQueryable.AnyAsync(flightReadDbContext.Aircraft.AsQueryable()))
//             {
//                 await flightReadDbContext.Aircraft.InsertManyAsync(mapper.Map<List<AircraftReadModel>>(InitialData.Aircrafts));
//             }
//         }
//     }
//
//
//     private async Task SeedSeatAsync()
//     {
//         if (!await EntityFrameworkQueryableExtensions.AnyAsync(catalogDbContext.Seats))
//         {
//             await catalogDbContext.Seats.AddRangeAsync(InitialData.Seats);
//             await catalogDbContext.SaveChangesAsync();
//
//             if (!await MongoQueryable.AnyAsync(flightReadDbContext.Seat.AsQueryable()))
//             {
//                 await flightReadDbContext.Seat.InsertManyAsync(mapper.Map<List<SeatReadModel>>(InitialData.Seats));
//             }
//         }
//     }
//
//     private async Task SeedFlightAsync()
//     {
//         if (!await EntityFrameworkQueryableExtensions.AnyAsync(catalogDbContext.Flights))
//         {
//             await catalogDbContext.Flights.AddRangeAsync(InitialData.Flights);
//             await catalogDbContext.SaveChangesAsync();
//
//             if (!await MongoQueryable.AnyAsync(flightReadDbContext.Flight.AsQueryable()))
//             {
//                 await flightReadDbContext.Flight.InsertManyAsync(mapper.Map<List<FlightReadModel>>(InitialData.Flights));
//             }
//         }
//     }
// }