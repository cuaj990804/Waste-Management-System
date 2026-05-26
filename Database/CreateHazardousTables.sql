-- =============================================
-- Script para crear las tablas de Residuos Peligrosos (Hazardous)
-- Sistema de Gestión Ambiental (SGA)
-- =============================================

USE [SGA]; -- Cambiar al nombre de tu base de datos
GO

-- =============================================
-- 1. Tabla: Cretib (Códigos CRETIB)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Cretib]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Cretib](
        [CretibId] INT IDENTITY(1,1) NOT NULL,
        [CretibKey] NVARCHAR(10) NOT NULL,
        [CretibName] NVARCHAR(100) NOT NULL,
        [CretibDescription] NVARCHAR(500) NULL,
        CONSTRAINT [PK_Cretib] PRIMARY KEY CLUSTERED ([CretibId] ASC)
    );

    PRINT 'Tabla Cretib creada exitosamente.';
END
ELSE
BEGIN
    PRINT 'La tabla Cretib ya existe.';
END
GO

-- =============================================
-- 2. Tabla: HazardousWastes (Catálogo de Residuos Peligrosos)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[HazardousWastes]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[HazardousWastes](
        [HazardousWasteId] INT IDENTITY(1,1) NOT NULL,
        [WasteKey] NVARCHAR(50) NOT NULL,
        [WasteName] NVARCHAR(200) NOT NULL,
        [WasteDescription] NVARCHAR(500) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
        [ModifiedDate] DATETIME NULL,
        CONSTRAINT [PK_HazardousWastes] PRIMARY KEY CLUSTERED ([HazardousWasteId] ASC),
        CONSTRAINT [UK_HazardousWastes_WasteKey] UNIQUE ([WasteKey])
    );

    PRINT 'Tabla HazardousWastes creada exitosamente.';
END
ELSE
BEGIN
    PRINT 'La tabla HazardousWastes ya existe.';
END
GO

-- =============================================
-- 3. Tabla: HazardousWasteCretibs (Relación Residuos-CRETIB)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[HazardousWasteCretibs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[HazardousWasteCretibs](
        [HazardousWasteId] INT NOT NULL,
        [CretibId] INT NOT NULL,
        [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_HazardousWasteCretibs] PRIMARY KEY CLUSTERED ([HazardousWasteId], [CretibId]),
        CONSTRAINT [FK_HazardousWasteCretibs_HazardousWastes]
            FOREIGN KEY ([HazardousWasteId]) REFERENCES [dbo].[HazardousWastes]([HazardousWasteId]) ON DELETE CASCADE,
        CONSTRAINT [FK_HazardousWasteCretibs_Cretib]
            FOREIGN KEY ([CretibId]) REFERENCES [dbo].[Cretib]([CretibId]) ON DELETE CASCADE
    );

    PRINT 'Tabla HazardousWasteCretibs creada exitosamente.';
END
ELSE
BEGIN
    PRINT 'La tabla HazardousWasteCretibs ya existe.';
END
GO

-- =============================================
-- 4. Tabla: HazardousAreas (Áreas que generan residuos peligrosos)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[HazardousAreas]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[HazardousAreas](
        [HazardousAreaId] INT IDENTITY(1,1) NOT NULL,
        [AreaKey] NVARCHAR(50) NULL,
        [AreaName] NVARCHAR(200) NOT NULL,
        [AreaDescription] NVARCHAR(500) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
        [ModifiedDate] DATETIME NULL,
        CONSTRAINT [PK_HazardousAreas] PRIMARY KEY CLUSTERED ([HazardousAreaId] ASC)
    );

    PRINT 'Tabla HazardousAreas creada exitosamente.';
END
ELSE
BEGIN
    PRINT 'La tabla HazardousAreas ya existe.';
END
GO

-- =============================================
-- 5. Tabla: HazardousWasteManifests (Manifiestos/Registros de Residuos Peligrosos)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[HazardousWasteManifests]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[HazardousWasteManifests](
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Folio] NVARCHAR(50) NULL,
        [WasteName] NVARCHAR(200) NOT NULL,
        [Quantity] DECIMAL(18, 2) NULL,
        [WeightKG] DECIMAL(18, 4) NULL,
        [Corrosive] BIT NULL,
        [Reactive] BIT NULL,
        [Explosive] BIT NULL,
        [Toxic] BIT NULL,
        [Flammable] BIT NULL,
        [Biological] BIT NULL,
        [GenerationArea] NVARCHAR(200) NULL,
        [GenerationManagerName] NVARCHAR(200) NULL,
        [WarehouseEntryDate] DATE NULL,
        [WarehouseExitDate] DATE NULL,
        [ManifestNumber] NVARCHAR(100) NULL,
        [ManifestDeliveredBy] NVARCHAR(200) NULL,
        [ManifestReceivedBy] NVARCHAR(200) NULL,
        [CollectionTransportName] NVARCHAR(200) NULL,
        [CollectionTransportAuthNumber] NVARCHAR(100) NULL,
        [FinalDisposalName] NVARCHAR(200) NULL,
        [FinalDisposalAuthNumber] NVARCHAR(100) NULL,
        [ManifestSealed] BIT NULL,
        [Comments] NVARCHAR(MAX) NULL,
        [CreatedDate] DATETIME NULL DEFAULT GETDATE(),
        [ModifiedDate] DATETIME NULL,
        CONSTRAINT [PK_HazardousWasteManifests] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    PRINT 'Tabla HazardousWasteManifests creada exitosamente.';
END
ELSE
BEGIN
    PRINT 'La tabla HazardousWasteManifests ya existe.';
END
GO

-- =============================================
-- ÍNDICES ADICIONALES
-- =============================================

-- Índice para búsqueda rápida por Folio
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_HazardousWasteManifests_Folio' AND object_id = OBJECT_ID('HazardousWasteManifests'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_HazardousWasteManifests_Folio]
    ON [dbo].[HazardousWasteManifests] ([Folio] ASC);
    PRINT 'Índice IX_HazardousWasteManifests_Folio creado.';
END
GO

-- Índice para búsqueda por fecha de ingreso
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_HazardousWasteManifests_WarehouseEntryDate' AND object_id = OBJECT_ID('HazardousWasteManifests'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_HazardousWasteManifests_WarehouseEntryDate]
    ON [dbo].[HazardousWasteManifests] ([WarehouseEntryDate] DESC);
    PRINT 'Índice IX_HazardousWasteManifests_WarehouseEntryDate creado.';
END
GO

-- Índice para búsqueda por área de generación
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_HazardousWasteManifests_GenerationArea' AND object_id = OBJECT_ID('HazardousWasteManifests'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_HazardousWasteManifests_GenerationArea]
    ON [dbo].[HazardousWasteManifests] ([GenerationArea] ASC);
    PRINT 'Índice IX_HazardousWasteManifests_GenerationArea creado.';
END
GO

-- =============================================
-- DATOS INICIALES: Códigos CRETIB
-- =============================================
IF NOT EXISTS (SELECT * FROM [dbo].[Cretib])
BEGIN
    INSERT INTO [dbo].[Cretib] ([CretibKey], [CretibName], [CretibDescription])
    VALUES
        ('C', 'Corrosivo', 'Residuos que pueden causar corrosión en materiales y tejidos'),
        ('R', 'Reactivo', 'Residuos que pueden reaccionar violentamente con agua u otras sustancias'),
        ('E', 'Explosivo', 'Residuos que pueden causar explosión bajo ciertas condiciones'),
        ('T', 'Tóxico', 'Residuos que pueden causar daños a la salud humana o al medio ambiente'),
        ('I', 'Inflamable', 'Residuos que pueden incendiarse fácilmente'),
        ('B', 'Biológico-Infeccioso', 'Residuos que contienen microorganismos patógenos');

    PRINT 'Datos iniciales de Cretib insertados exitosamente.';
END
ELSE
BEGIN
    PRINT 'Los datos de Cretib ya existen.';
END
GO

-- =============================================
-- DATOS INICIALES: Áreas de Residuos Peligrosos (Ejemplos)
-- =============================================
IF NOT EXISTS (SELECT * FROM [dbo].[HazardousAreas])
BEGIN
    INSERT INTO [dbo].[HazardousAreas] ([AreaKey], [AreaName], [AreaDescription], [IsActive])
    VALUES
        ('ENFER', 'Enfermeria', 'Área de enfermería y atención médica', 1),
        ('LAB', 'Laboratorio', 'Laboratorio de análisis químicos', 1),
        ('MANT', 'Mantenimiento', 'Área de mantenimiento e instalaciones', 1),
        ('PROD', 'Producción', 'Área de producción industrial', 1),
        ('ALMA', 'Almacén', 'Almacén de químicos y materiales', 1);

    PRINT 'Datos iniciales de HazardousAreas insertados exitosamente.';
END
ELSE
BEGIN
    PRINT 'Los datos de HazardousAreas ya existen.';
END
GO

-- =============================================
-- DATOS DE EJEMPLO: Residuos Peligrosos (Opcional)
-- =============================================
IF NOT EXISTS (SELECT * FROM [dbo].[HazardousWastes])
BEGIN
    -- Insertar algunos residuos de ejemplo
    INSERT INTO [dbo].[HazardousWastes] ([WasteKey], [WasteName], [WasteDescription], [IsActive])
    VALUES
        ('RP-001', 'Aceite usado contaminado', 'Aceite lubricante usado contaminado con metales pesados', 1),
        ('RP-002', 'Solventes contaminados', 'Mezcla de solventes orgánicos contaminados', 1),
        ('RP-003', 'Residuos sólidos contaminados con (sustancias químicas)', 'Material sólido contaminado con sustancias químicas peligrosas', 1),
        ('RP-004', 'Baterías de plomo-ácido', 'Baterías usadas de plomo-ácido', 1),
        ('RP-005', 'Material biológico infeccioso', 'Residuos médicos con riesgo biológico', 1);

    -- Obtener IDs insertados
    DECLARE @AceiteId INT = (SELECT HazardousWasteId FROM HazardousWastes WHERE WasteKey = 'RP-001');
    DECLARE @SolventesId INT = (SELECT HazardousWasteId FROM HazardousWastes WHERE WasteKey = 'RP-002');
    DECLARE @SolidosId INT = (SELECT HazardousWasteId FROM HazardousWastes WHERE WasteKey = 'RP-003');
    DECLARE @BateriasId INT = (SELECT HazardousWasteId FROM HazardousWastes WHERE WasteKey = 'RP-004');
    DECLARE @BiologicoId INT = (SELECT HazardousWasteId FROM HazardousWastes WHERE WasteKey = 'RP-005');

    -- Obtener IDs de CRETIB
    DECLARE @CorrosiveId INT = (SELECT CretibId FROM Cretib WHERE CretibKey = 'C');
    DECLARE @ReactiveId INT = (SELECT CretibId FROM Cretib WHERE CretibKey = 'R');
    DECLARE @ExplosiveId INT = (SELECT CretibId FROM Cretib WHERE CretibKey = 'E');
    DECLARE @ToxicId INT = (SELECT CretibId FROM Cretib WHERE CretibKey = 'T');
    DECLARE @FlammableId INT = (SELECT CretibId FROM Cretib WHERE CretibKey = 'I');
    DECLARE @BiologicalId INT = (SELECT CretibId FROM Cretib WHERE CretibKey = 'B');

    -- Asignar códigos CRETIB a los residuos
    INSERT INTO [dbo].[HazardousWasteCretibs] ([HazardousWasteId], [CretibId])
    VALUES
        -- Aceite usado: Tóxico, Inflamable
        (@AceiteId, @ToxicId),
        (@AceiteId, @FlammableId),

        -- Solventes: Tóxico, Inflamable
        (@SolventesId, @ToxicId),
        (@SolventesId, @FlammableId),

        -- Residuos sólidos: Tóxico
        (@SolidosId, @ToxicId),

        -- Baterías: Corrosivo, Tóxico
        (@BateriasId, @CorrosiveId),
        (@BateriasId, @ToxicId),

        -- Biológico: Biológico-Infeccioso
        (@BiologicoId, @BiologicalId);

    PRINT 'Datos de ejemplo de HazardousWastes y sus códigos CRETIB insertados exitosamente.';
END
ELSE
BEGIN
    PRINT 'Los datos de ejemplo de HazardousWastes ya existen.';
END
GO

-- =============================================
-- RESUMEN
-- =============================================
PRINT '';
PRINT '========================================';
PRINT 'RESUMEN DE TABLAS CREADAS:';
PRINT '========================================';
PRINT '1. Cretib - Códigos CRETIB (6 registros)';
PRINT '2. HazardousWastes - Catálogo de residuos peligrosos';
PRINT '3. HazardousWasteCretibs - Relación Residuos-CRETIB';
PRINT '4. HazardousAreas - Áreas generadoras (5 registros)';
PRINT '5. HazardousWasteManifests - Manifiestos de residuos';
PRINT '';
PRINT 'Script ejecutado exitosamente.';
PRINT '========================================';
GO
