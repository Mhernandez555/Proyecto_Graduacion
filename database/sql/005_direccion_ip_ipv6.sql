DECLARE @tabla sysname;
DECLARE @nulable bit;
DECLARE @longitud smallint;
DECLARE @tipo sysname;
DECLARE @sentencia nvarchar(max);
DECLARE @tablas TABLE (nombre sysname NOT NULL);

INSERT INTO @tablas (nombre)
VALUES (N'bitacora_ingresos'), (N'bitacora_movimientos');

DECLARE cursorTablas CURSOR LOCAL FAST_FORWARD FOR
    SELECT nombre FROM @tablas;

OPEN cursorTablas;
FETCH NEXT FROM cursorTablas INTO @tabla;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @nulable = NULL;
    SET @longitud = NULL;
    SET @tipo = NULL;

    SELECT @nulable = columna.is_nullable, @longitud = columna.max_length, @tipo = tipo.name
    FROM sys.columns AS columna
    JOIN sys.types AS tipo ON tipo.user_type_id = columna.user_type_id
    WHERE columna.object_id = OBJECT_ID(N'dbo.' + @tabla) AND columna.name = N'direccion_ip';

    IF @longitud IS NOT NULL AND @longitud <> -1
       AND ((@tipo IN (N'nvarchar', N'nchar') AND @longitud < 90)
            OR (@tipo IN (N'varchar', N'char') AND @longitud < 45))
    BEGIN
        SET @sentencia = N'ALTER TABLE dbo.' + QUOTENAME(@tabla) +
            N' ALTER COLUMN direccion_ip nvarchar(45) ' +
            CASE WHEN @nulable = 1 THEN N'NULL' ELSE N'NOT NULL' END + N';';
        EXEC sp_executesql @sentencia;
    END;

    FETCH NEXT FROM cursorTablas INTO @tabla;
END;

CLOSE cursorTablas;
DEALLOCATE cursorTablas;
GO
