IF COL_LENGTH('dbo.bitacora_ingresos', 'motivo_salida') IS NULL
BEGIN
    ALTER TABLE dbo.bitacora_ingresos
        ADD motivo_salida nvarchar(40) NULL;
END;
GO
