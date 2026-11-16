IF OBJECT_ID('dbo.proyecto_usuarios', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.proyecto_usuarios
    (
        id_proyecto int NOT NULL,
        id_usuario int NOT NULL,
        fecha_asignacion datetime2(0) NOT NULL
            CONSTRAINT DF_proyecto_usuarios_fecha_asignacion DEFAULT (SYSDATETIME()),
        CONSTRAINT PK_proyecto_usuarios PRIMARY KEY (id_proyecto, id_usuario),
        CONSTRAINT FK_proyecto_usuarios_proyectos FOREIGN KEY (id_proyecto)
            REFERENCES dbo.proyectos (id_proyecto) ON DELETE CASCADE,
        CONSTRAINT FK_proyecto_usuarios_usuarios FOREIGN KEY (id_usuario)
            REFERENCES dbo.usuarios (id_usuario)
    );
END;
GO

INSERT INTO dbo.proyecto_usuarios (id_proyecto, id_usuario)
SELECT proyecto.id_proyecto, proyecto.id_responsable
FROM dbo.proyectos AS proyecto
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.proyecto_usuarios AS asignacion
    WHERE asignacion.id_proyecto = proyecto.id_proyecto
      AND asignacion.id_usuario = proyecto.id_responsable
);
GO

INSERT INTO dbo.proyecto_usuarios (id_proyecto, id_usuario)
SELECT DISTINCT tarea.id_proyecto, tarea_usuario.id_usuario
FROM dbo.tareas AS tarea
INNER JOIN dbo.tarea_usuarios AS tarea_usuario
    ON tarea_usuario.id_tarea = tarea.id_tarea
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.proyecto_usuarios AS asignacion
    WHERE asignacion.id_proyecto = tarea.id_proyecto
      AND asignacion.id_usuario = tarea_usuario.id_usuario
);
GO
