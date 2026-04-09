using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using HowsMyMoney.Models;

namespace HowsMyMoney.Data;

/// <summary>
/// Contexto de base de datos para la aplicación
/// </summary>
public class InvestmentDbContext : DbContext
{
    public DbSet<Investment> Investments { get; set; } = null!;
    public DbSet<PriceHistory> PriceHistories { get; set; } = null!;
    public DbSet<ImageCache> ImageCaches { get; set; } = null!;
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HowsMyMoney",
            "investments.db"
        );
        
        // Crear directorio si no existe
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }
    
    /// <summary>
    /// Migra la base de datos para agregar campos Day y Week si no existen, y crear tabla ImageCaches
    /// </summary>
    public void MigrateDatabaseSchema()
    {
        try
        {
            using var connection = Database.GetDbConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            
            // Verificar si la tabla ImageCaches existe
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='ImageCaches'";
            var reader = command.ExecuteReader();
            bool hasImageCachesTable = reader.Read();
            reader.Close();
            
            if (!hasImageCachesTable)
            {
                Console.WriteLine("🔄 Creando tabla ImageCaches...");
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ImageCaches (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Url TEXT NOT NULL UNIQUE,
                        ImageData BLOB NOT NULL,
                        LastUpdated TEXT NOT NULL,
                        ContentType TEXT NOT NULL
                    )";
                command.ExecuteNonQuery();
                
                // Crear índice en URL para búsquedas rápidas
                command.CommandText = "CREATE UNIQUE INDEX IF NOT EXISTS IX_ImageCaches_Url ON ImageCaches (Url)";
                command.ExecuteNonQuery();
                
                Console.WriteLine("✓ Tabla ImageCaches creada");
            }
            
            // Verificar si la columna Day existe
            command.CommandText = "PRAGMA table_info(PriceHistories)";
            var columnReader = command.ExecuteReader();
            bool hasDayColumn = false;
            bool hasWeekColumn = false;
            
            while (columnReader.Read())
            {
                var columnName = columnReader.GetString(1);
                if (columnName == "Day") hasDayColumn = true;
                if (columnName == "Week") hasWeekColumn = true;
            }
            columnReader.Close();
            
            // Agregar columnas si no existen
            if (!hasDayColumn)
            {
                Console.WriteLine("🔄 Agregando columna 'Day' a PriceHistories...");
                command.CommandText = "ALTER TABLE PriceHistories ADD COLUMN Day TEXT NOT NULL DEFAULT ''";
                command.ExecuteNonQuery();
                Console.WriteLine("✓ Columna 'Day' agregada");
            }
            
            if (!hasWeekColumn)
            {
                Console.WriteLine("🔄 Agregando columna 'Week' a PriceHistories...");
                command.CommandText = "ALTER TABLE PriceHistories ADD COLUMN Week TEXT NOT NULL DEFAULT ''";
                command.ExecuteNonQuery();
                Console.WriteLine("✓ Columna 'Week' agregada");
            }
            
            // Actualizar registros existentes con datos de Day y Week basados en RecordedAt
            if (!hasDayColumn || !hasWeekColumn)
            {
                Console.WriteLine("🔄 Actualizando registros existentes...");
                var histories = PriceHistories.ToList();
                foreach (var history in histories)
                {
                    history.Day = history.RecordedAt.ToString("yyyy-MM-dd");
                    history.Week = GetIso8601WeekOfYear(history.RecordedAt);
                }
                SaveChanges();
                Console.WriteLine($"✓ {histories.Count} registros actualizados");
            }
            
            connection.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error en migración: {ex.Message}");
        }
    }
    
    private string GetIso8601WeekOfYear(DateTime date)
    {
        var day = (int)System.Globalization.CultureInfo.CurrentCulture.Calendar.GetDayOfWeek(date);
        var weekNum = System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
            date, 
            System.Globalization.CalendarWeekRule.FirstFourDayWeek, 
            DayOfWeek.Monday);
        return $"{date.Year}-W{weekNum:D2}";
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configurar Investment
        modelBuilder.Entity<Investment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PurchasePrice).HasPrecision(18, 8);
            entity.Property(e => e.CurrentPrice).HasPrecision(18, 8);
            entity.Property(e => e.Quantity).HasPrecision(18, 8);
        });
        
        // Configurar PriceHistory
        modelBuilder.Entity<PriceHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Price).HasPrecision(18, 8);
            entity.Property(e => e.Month).HasMaxLength(7); // YYYY-MM
            
            entity.HasOne(e => e.Investment)
                .WithMany()
                .HasForeignKey(e => e.InvestmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Configurar ImageCache
        modelBuilder.Entity<ImageCache>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Url).IsRequired().HasMaxLength(500);
            entity.HasIndex(e => e.Url).IsUnique(); // Índice único en URL
            entity.Property(e => e.ImageData).IsRequired();
            entity.Property(e => e.LastUpdated).IsRequired();
            entity.Property(e => e.ContentType).HasMaxLength(50);
        });
    }
}
