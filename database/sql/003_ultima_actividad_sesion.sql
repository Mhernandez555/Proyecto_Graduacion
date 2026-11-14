IF COL_LENGTH('dbo.bitacora_ingresos', 'fecha_hora_ultima_actividad') IS NULL
BEGIN
    ALTER TABLE dbo.bitacora_ingresos
        ADD fecha_hora_ultima_actividad datetime2(0) NULL;
END;
GO
