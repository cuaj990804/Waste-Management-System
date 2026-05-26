using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SGA.Models;

namespace SGA.Data;

public partial class SgaContext : DbContext
{
    public SgaContext()
    {
    }

    public SgaContext(DbContextOptions<SgaContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Area> Areas { get; set; }

    public virtual DbSet<Cretib> Cretibs { get; set; }

    public virtual DbSet<HazardousArea> HazardousAreas { get; set; }

    public virtual DbSet<HazardousWaste> HazardousWastes { get; set; }

    public virtual DbSet<HazardousWasteCretib> HazardousWasteCretibs { get; set; }

    public virtual DbSet<HazardousWasteManifest> HazardousWasteManifests { get; set; }

    public virtual DbSet<NonHazardou> NonHazardous { get; set; }

    public virtual DbSet<PartNumber> PartNumbers { get; set; }

    public virtual DbSet<StorageType> StorageTypes { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Area>(entity =>
        {
            entity.HasKey(e => e.AreaId).HasName("PK__Area__70B82048A5B439FB");

            entity.ToTable("Area");

            entity.Property(e => e.AreaDescription)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AreaKey)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AreaName)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Cretib>(entity =>
        {
            entity.ToTable("Cretib");

            entity.Property(e => e.CretibDescription).HasMaxLength(500);
            entity.Property(e => e.CretibKey).HasMaxLength(10);
            entity.Property(e => e.CretibName).HasMaxLength(100);
        });

        modelBuilder.Entity<HazardousArea>(entity =>
        {
            entity.HasKey(e => e.HazardousAreaId).HasName("PK_HazardousAreas");

            entity.ToTable("HazardousArea");

            entity.Property(e => e.AreaDescription).HasMaxLength(500);
            entity.Property(e => e.AreaKey).HasMaxLength(50);
            entity.Property(e => e.AreaName).HasMaxLength(200);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<HazardousWaste>(entity =>
        {
            entity.HasKey(e => e.HazardousWasteId).HasName("PK_HazardousWastes");

            entity.ToTable("HazardousWaste");

            entity.HasIndex(e => e.WasteKey, "UK_HazardousWastes_WasteKey").IsUnique();

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.WasteDescription).HasMaxLength(500);
            entity.Property(e => e.WasteKey).HasMaxLength(50);
            entity.Property(e => e.WasteName).HasMaxLength(200);
        });

        modelBuilder.Entity<HazardousWasteCretib>(entity =>
        {
            entity.HasKey(e => new { e.HazardousWasteId, e.CretibId }).HasName("PK_HazardousWasteCretibs");

            entity.ToTable("HazardousWaste_Cretib");

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Cretib).WithMany(p => p.HazardousWasteCretibs)
                .HasForeignKey(d => d.CretibId)
                .HasConstraintName("FK_HazardousWasteCretibs_Cretib");

            entity.HasOne(d => d.HazardousWaste).WithMany(p => p.HazardousWasteCretibs)
                .HasForeignKey(d => d.HazardousWasteId)
                .HasConstraintName("FK_HazardousWasteCretibs_HazardousWastes");
        });

        modelBuilder.Entity<HazardousWasteManifest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_HazardousWasteManifests");

            entity.ToTable("HazardousWasteManifest");

            entity.HasIndex(e => e.Folio, "IX_HazardousWasteManifests_Folio");

            entity.HasIndex(e => e.GenerationArea, "IX_HazardousWasteManifests_GenerationArea");

            entity.HasIndex(e => e.WarehouseEntryDate, "IX_HazardousWasteManifests_WarehouseEntryDate").IsDescending();

            entity.Property(e => e.CollectionTransportAuthNumber).HasMaxLength(100);
            entity.Property(e => e.CollectionTransportName).HasMaxLength(200);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FinalDisposalAuthNumber).HasMaxLength(100);
            entity.Property(e => e.FinalDisposalName).HasMaxLength(200);
            entity.Property(e => e.Folio).HasMaxLength(50);
            entity.Property(e => e.GenerationArea).HasMaxLength(200);
            entity.Property(e => e.GenerationManagerName).HasMaxLength(200);
            entity.Property(e => e.ManifestDeliveredBy).HasMaxLength(200);
            entity.Property(e => e.ManifestNumber).HasMaxLength(100);
            entity.Property(e => e.ManifestReceivedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.WasteName).HasMaxLength(200);
            entity.Property(e => e.WeightKg)
                .HasColumnType("decimal(18, 4)")
                .HasColumnName("WeightKG");
        });

        modelBuilder.Entity<NonHazardou>(entity =>
        {
            entity.HasKey(e => e.NonHazardousId).HasName("PK__NonHazar__00899058ACC278C2");

            entity.Property(e => e.AreaGdi)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("AreaGDI");
            entity.Property(e => e.AreaKey)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.CollectionAuthorizationNumber)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.CollectionCenterAuthorizationNumber)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.CollectionCenterName)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.CollectorName)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Comments).IsUnicode(false);
            entity.Property(e => e.DateIntoWarehouse).HasColumnType("smalldatetime");
            entity.Property(e => e.DateOutoWarehouse).HasColumnType("smalldatetime");
            entity.Property(e => e.FinalDisposalAuthorizationNumber)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FinalDisposalCompanyName)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.ManifestNumber)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.PartNumber)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Program)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.ReturnToClient)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.ReuseCompanyAuthorizationNumber)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ReuseCompanyName)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.SealedManifests)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.StorageType)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.StorageTypeKey)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.WasteDestination)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.WasteGeneratorNumber)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.WasteKey)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.WasteName)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.WasteNameGdi)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("WasteNameGDI");
            entity.Property(e => e.WasteQuantity)
                .HasMaxLength(255)
                .IsUnicode(false);
        });

        modelBuilder.Entity<PartNumber>(entity =>
        {
            entity.HasKey(e => e.PartNumberId).HasName("PK__PartNumb__FD9D7FB2A0BF6227");

            entity.ToTable("PartNumber");

            entity.Property(e => e.PartNumber1)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("PartNumber");
            entity.Property(e => e.PartNumberKey)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.PartNumberName)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.PartNumberNameGdi)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("PartNumberNameGDI");
            entity.Property(e => e.PartNumberProgram)
                .HasMaxLength(255)
                .IsUnicode(false);
        });

        modelBuilder.Entity<StorageType>(entity =>
        {
            entity.HasKey(e => e.StorageId).HasName("PK__StorageT__8A247E57066A88B4");

            entity.ToTable("StorageType");

            entity.Property(e => e.StorageKey)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.StorageName)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C2FB0FFBD");

            entity.Property(e => e.Area)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.EmployeeNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UserRole)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
