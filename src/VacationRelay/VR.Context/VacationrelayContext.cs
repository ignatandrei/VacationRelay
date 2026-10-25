using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace VR.Context;

public partial class VacationrelayContext : DbContext
{
    public VacationrelayContext(DbContextOptions<VacationrelayContext> options)
        : base(options)
    {
    }

    public virtual DbSet<VrData> VrData { get; set; }

    public virtual DbSet<VrDataHistory> VrDataHistory { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VrData>(entity =>
        {
            entity.ToTable("VR_Data");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newsequentialid())", "DF_VR_Data_ID")
                .HasColumnName("ID");
            entity.Property(e => e.IdpersonInVacation)
                .HasMaxLength(50)
                .HasColumnName("IDPersonInVacation");
            entity.Property(e => e.IdpersonReplace)
                .HasMaxLength(50)
                .HasColumnName("IDPersonReplace");
        });

        modelBuilder.Entity<VrDataHistory>(entity =>
        {
            entity.ToTable("VR_Data_History");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.IdpersonInVacation)
                .HasMaxLength(50)
                .HasColumnName("IDPersonInVacation");
            entity.Property(e => e.IdpersonReplace)
                .HasMaxLength(50)
                .HasColumnName("IDPersonReplace");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
