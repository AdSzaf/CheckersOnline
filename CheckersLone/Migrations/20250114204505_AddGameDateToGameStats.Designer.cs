﻿// <auto-generated />
using System;
using CheckersLone.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace CheckersLone.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20250114204505_AddGameDateToGameStats")]
    partial class AddGameDateToGameStats
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("CheckersLone.Models.GameStats", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Color")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("Date")
                        .HasColumnType("datetime2");

                    b.Property<int>("Wins")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("GameStats");
                });

            modelBuilder.Entity("CheckersLone.Models.Player", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Players");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            Name = "Player1",
                            Password = "123"
                        },
                        new
                        {
                            Id = 2,
                            Name = "Player2",
                            Password = "231"
                        },
                        new
                        {
                            Id = 3,
                            Name = "TestPlayer",
                            Password = "TestPassword"
                        });
                });

            modelBuilder.Entity("Game", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("Player1Id")
                        .HasColumnType("int");

                    b.Property<int>("Player2Id")
                        .HasColumnType("int");

                    b.Property<int?>("PlayerId")
                        .HasColumnType("int");

                    b.Property<string>("Wynik")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("PlayerId");

                    b.ToTable("Games");
                });

            modelBuilder.Entity("Game", b =>
                {
                    b.HasOne("CheckersLone.Models.Player", null)
                        .WithMany("GameHistory")
                        .HasForeignKey("PlayerId");
                });

            modelBuilder.Entity("CheckersLone.Models.Player", b =>
                {
                    b.Navigation("GameHistory");
                });
#pragma warning restore 612, 618
        }
    }
}
