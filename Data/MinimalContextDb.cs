﻿using Microsoft.EntityFrameworkCore;
using MinimalAPI.Models;

namespace MinimalAPI.Data
{
    public class MinimalContextDb : DbContext
    {
        public MinimalContextDb(DbContextOptions<MinimalContextDb> options) : base(options) { }

        public DbSet<Fornecedor> Fornecedores { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Fornecedor>()
                .HasKey(p => p.Id);

            modelBuilder.Entity<Fornecedor>()
                .Property(p => p.Nome)
                .IsRequired()
                .HasColumnType("varchar(45)");

            modelBuilder.Entity<Fornecedor>()
                .Property(p => p.Documento)
                .IsRequired()
                .HasColumnType("varchar(200)");

            modelBuilder.Entity<Fornecedor>()
                .ToTable("fornecedores");

            base.OnModelCreating(modelBuilder);
        }
    }
}
