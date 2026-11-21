-- =============================================================
-- Sistema CountryAPI · Datos de prueba (SQL Server 2022, T-SQL)
-- Ejecutar después de init.sql.
-- Autor: Escobar Marcos · 20/11/2026
-- =============================================================
USE ApiPaisesDB;
GO

-- Usuarios de prueba (clave en SHA-256):
--   monster / monster9
--   admin   / admin123
INSERT INTO Usuarios (Username, PasswordHash, Nombre, Estado) VALUES
  (N'monster', '774e993500f4027acfd72b7a7ee564b76ae43cf7c4c943ed0e0f364cca16b6ec', N'Marcos Escobar', N'ACTIVO'),
  (N'admin',   '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', N'Administrador',  N'ACTIVO');
GO

-- Regiones de la API REST Countries traducidas al español
INSERT INTO Regiones (Nombre) VALUES
  (N'América'), (N'Europa'), (N'Asia'), (N'África'), (N'Oceanía'), (N'Antártida');
GO

-- Muestra de países para probar el sistema sin conexión a la API.
-- La opción «Importar países» actualiza estos registros y agrega el resto.
INSERT INTO Paises (Nombre, Capital, IdRegion, SubRegion, Poblacion, Area, Moneda)
SELECT p.Nombre, p.Capital, r.IdRegion, p.SubRegion, p.Poblacion, p.Area, p.Moneda
FROM (VALUES
  (N'Ecuador',   N'Quito',     N'América', N'South America',             17643060,   276841,  N'United States dollar (USD)'),
  (N'Colombia',  N'Bogotá',    N'América', N'South America',             50882884,   1141748, N'Colombian peso (COP)'),
  (N'Perú',      N'Lima',      N'América', N'South America',             32971846,   1285216, N'Peruvian sol (PEN)'),
  (N'Brasil',    N'Brasília',  N'América', N'South America',             212559409,  8515767, N'Brazilian real (BRL)'),
  (N'España',    N'Madrid',    N'Europa',  N'Southern Europe',           47351567,   505992,  N'Euro (EUR)'),
  (N'Alemania',  N'Berlin',    N'Europa',  N'Western Europe',            83240525,   357114,  N'Euro (EUR)'),
  (N'China',     N'Beijing',   N'Asia',    N'Eastern Asia',              1402112000, 9706961, N'Chinese yuan (CNY)'),
  (N'India',     N'New Delhi', N'Asia',    N'Southern Asia',             1380004385, 3287590, N'Indian rupee (INR)'),
  (N'Japón',     N'Tokyo',     N'Asia',    N'Eastern Asia',              125836021,  377930,  N'Japanese yen (JPY)'),
  (N'Nigeria',   N'Abuja',     N'África',  N'Western Africa',            206139587,  923768,  N'Nigerian naira (NGN)'),
  (N'Egipto',    N'Cairo',     N'África',  N'Northern Africa',           102334403,  1002450, N'Egyptian pound (EGP)'),
  (N'Australia', N'Canberra',  N'Oceanía', N'Australia and New Zealand', 25687041,   7692024, N'Australian dollar (AUD)')
) AS p (Nombre, Capital, Region, SubRegion, Poblacion, Area, Moneda)
JOIN Regiones r ON r.Nombre = p.Region;
GO

-- Muestra de universidades de Ecuador (actividad avanzada).
-- La opción «Universidades» → «Importar» actualiza estos registros y agrega el resto.
INSERT INTO Universidades (Nombre, Dominio, SitioWeb, Pais) VALUES
  (N'Escuela Politécnica del Ejército',          N'espe.edu.ec', N'http://www.espe.edu.ec/', N'Ecuador'),
  (N'Escuela Politécnica Nacional',              N'epn.edu.ec',  N'http://www.epn.edu.ec/',  N'Ecuador'),
  (N'Universidad Central del Ecuador',           N'uce.edu.ec',  N'http://www.uce.edu.ec/',  N'Ecuador'),
  (N'Pontificia Universidad Católica del Ecuador', N'puce.edu.ec', N'http://www.puce.edu.ec/', N'Ecuador'),
  (N'Universidad San Francisco de Quito',        N'usfq.edu.ec', N'http://www.usfq.edu.ec/', N'Ecuador');
GO
