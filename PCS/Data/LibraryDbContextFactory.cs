using Microsoft.EntityFrameworkCore;

namespace PCS.Data;

public static class LibraryDbContextFactory
{
    public static LibraryDbContext Create()
    {
        var optionsBuilder = new DbContextOptionsBuilder<LibraryDbContext>();
        optionsBuilder.UseSqlite(DatabaseSettings.ConnectionString);
        return new LibraryDbContext(optionsBuilder.Options);
    }

    public static void InitializeDatabase()
    {
        using var dbContext = Create();
        dbContext.Database.EnsureCreated();
    }
}
