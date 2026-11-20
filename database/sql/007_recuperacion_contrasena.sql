CREATE OR ALTER PROCEDURE dbo.sp_datos_recuperacion
    @nombre_usuario nvarchar(50),
    @cedula nvarchar(12)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT correo, nombre
    FROM dbo.usuarios
    WHERE nombre_usuario = @nombre_usuario
      AND cedula = @cedula
      AND estado = 1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_aplicar_contrasena_temporal
    @nombre_usuario nvarchar(50),
    @cedula nvarchar(12),
    @hash nvarchar(256),
    @ip nvarchar(45)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        DECLARE @usuarios_actualizados TABLE (id_usuario int NOT NULL);

        BEGIN TRANSACTION;

        UPDATE dbo.usuarios
        SET contrasena_hash = @hash,
            requiere_cambio_contrasena = 1
        OUTPUT inserted.id_usuario INTO @usuarios_actualizados
        WHERE nombre_usuario = @nombre_usuario
          AND cedula = @cedula
          AND estado = 1;

        IF NOT EXISTS (SELECT 1 FROM @usuarios_actualizados)
        BEGIN
            ROLLBACK TRANSACTION;
            RETURN 0;
        END;

        INSERT INTO dbo.bitacora_movimientos
            (id_usuario, direccion_ip, fecha_hora, modulo, tipo_movimiento, detalle)
        SELECT id_usuario, @ip, SYSDATETIME(), N'SEGURIDAD',
               N'RECUPERAR CONTRASEÑA',
               N'Se envió contraseña temporal al correo registrado'
        FROM @usuarios_actualizados;

        COMMIT TRANSACTION;
        RETURN 1;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
