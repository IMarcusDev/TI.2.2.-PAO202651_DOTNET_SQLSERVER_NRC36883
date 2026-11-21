-- =============================================================
-- Sistema CountryAPI · TI.2.2 Consumo de APIs Públicas en .NET con SQL Server (MVC)
-- Script de creación de la base de datos ApiPaisesDB (SQL Server 2022, T-SQL)
-- Autor: Escobar Marcos · 20/11/2026
-- Ejecutar en SQL Server Management Studio.
-- =============================================================

USE master;
GO
IF DB_ID(N'ApiPaisesDB') IS NOT NULL
BEGIN
    ALTER DATABASE ApiPaisesDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE ApiPaisesDB;
END
GO
CREATE DATABASE ApiPaisesDB;
GO
USE ApiPaisesDB;
GO

-- -------------------------------------------------------------
-- Usuarios: personas que inician sesión en el sistema.
-- La clave se guarda como hash SHA-256 (64 caracteres hexadecimales).
-- -------------------------------------------------------------
CREATE TABLE Usuarios (
    IdUsuario     INT IDENTITY(1,1) NOT NULL,
    Username      NVARCHAR(50)      NOT NULL,
    PasswordHash  CHAR(64)          NOT NULL,
    Nombre        NVARCHAR(100)     NOT NULL,
    Estado        NVARCHAR(10)      NOT NULL CONSTRAINT DF_Usuarios_Estado DEFAULT N'ACTIVO',
    CONSTRAINT PK_Usuarios PRIMARY KEY (IdUsuario),
    CONSTRAINT UQ_Usuarios_Username UNIQUE (Username),
    CONSTRAINT CK_Usuarios_Estado CHECK (Estado IN (N'ACTIVO', N'INACTIVO'))
);
GO

-- -------------------------------------------------------------
-- Regiones: catálogo de regiones de la API REST Countries, en español.
-- -------------------------------------------------------------
CREATE TABLE Regiones (
    IdRegion  INT IDENTITY(1,1) NOT NULL,
    Nombre    NVARCHAR(50)      NOT NULL,
    CONSTRAINT PK_Regiones PRIMARY KEY (IdRegion),
    CONSTRAINT UQ_Regiones_Nombre UNIQUE (Nombre)
);
GO

-- -------------------------------------------------------------
-- Paises: tabla del taller, normalizada.
--   * Nombre único: importar varias veces no duplica países.
--   * La región pasa al catálogo Regiones (IdRegion).
--   * SubRegion y Area, que pide el taller .NET.
--   * Población y área no negativas.
--   * FechaActualizacion: la asigna el servidor (EF Core) en cada
--     importación o edición; SQL Server no tiene ON UPDATE.
-- -------------------------------------------------------------
CREATE TABLE Paises (
    Id                  INT IDENTITY(1,1) NOT NULL,
    Nombre              NVARCHAR(100)     NOT NULL,
    Capital             NVARCHAR(100)     NULL,
    IdRegion            INT               NOT NULL,
    SubRegion           NVARCHAR(100)     NULL,
    Poblacion           BIGINT            NOT NULL CONSTRAINT DF_Paises_Poblacion DEFAULT 0,
    Area                FLOAT             NOT NULL CONSTRAINT DF_Paises_Area DEFAULT 0,
    Moneda              NVARCHAR(100)     NULL,
    FechaActualizacion  DATETIME2(0)      NOT NULL CONSTRAINT DF_Paises_Fecha DEFAULT SYSDATETIME(),
    CONSTRAINT PK_Paises PRIMARY KEY (Id),
    CONSTRAINT UQ_Paises_Nombre UNIQUE (Nombre),
    CONSTRAINT CK_Paises_Poblacion CHECK (Poblacion >= 0),
    CONSTRAINT CK_Paises_Area CHECK (Area >= 0),
    CONSTRAINT FK_Paises_Regiones FOREIGN KEY (IdRegion)
        REFERENCES Regiones (IdRegion)
        ON UPDATE CASCADE ON DELETE NO ACTION
);
GO

-- -------------------------------------------------------------
-- Universidades: actividad avanzada del taller (API de Hipolabs).
--   * La pareja Nombre + Pais es única: reimportar no duplica.
--   * Pais se guarda como texto en inglés, como lo entrega la API.
-- -------------------------------------------------------------
CREATE TABLE Universidades (
    Id                  INT IDENTITY(1,1) NOT NULL,
    Nombre              NVARCHAR(200)     NOT NULL,
    Dominio             NVARCHAR(150)     NULL,
    SitioWeb            NVARCHAR(250)     NULL,
    Pais                NVARCHAR(100)     NOT NULL,
    FechaActualizacion  DATETIME2(0)      NOT NULL CONSTRAINT DF_Universidades_Fecha DEFAULT SYSDATETIME(),
    CONSTRAINT PK_Universidades PRIMARY KEY (Id),
    CONSTRAINT UQ_Universidades_Nombre_Pais UNIQUE (Nombre, Pais)
);
GO
