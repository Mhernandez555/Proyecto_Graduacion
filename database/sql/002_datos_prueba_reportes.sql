/* ============================================================================
   CONTROLEX – Datos FICTICIOS para probar los reportes (RF-005 y RF-024).
   Todos los nombres empiezan con "PRUEBA -". Al final hay un bloque para borrarlos.
   Cambie el nombre de la base si la suya se llama distinto.
   ============================================================================ */
USE CONTROLEX_BD;
GO

SET NOCOUNT ON;

IF OBJECT_ID('dbo.vw_rentabilidad_proyectos', 'V') IS NULL
BEGIN
    RAISERROR(N'Falta la vista vw_rentabilidad_proyectos. Ejecute primero el script de la base de datos (Entregable 4).', 16, 1);
    RETURN;
END;

IF EXISTS (SELECT 1 FROM dbo.proyectos WHERE nombre LIKE N'PRUEBA -%')
BEGIN
    RAISERROR(N'Los datos de prueba ya fueron cargados. Use el bloque de limpieza del final si desea cargarlos de nuevo.', 16, 1);
    RETURN;
END;

DECLARE @resp int = (SELECT TOP 1 id_usuario FROM dbo.usuarios WHERE nombre_usuario = 'mhernandez');
IF @resp IS NULL
BEGIN
    RAISERROR(N'No existe el usuario mhernandez.', 16, 1);
    RETURN;
END;
DECLARE @otro int = (SELECT TOP 1 id_usuario FROM dbo.usuarios WHERE id_usuario <> @resp ORDER BY id_usuario);

DECLARE @cat int = (SELECT TOP 1 id_categoria FROM dbo.categorias ORDER BY id_categoria);
DECLARE @pPlan int = (SELECT id_estado FROM dbo.estados WHERE tipo_estado = 'PROYECTO' AND nombre = N'Planificado');
DECLARE @pEjec int = (SELECT id_estado FROM dbo.estados WHERE tipo_estado = 'PROYECTO' AND nombre = N'En ejecución');
DECLARE @pFin  int = (SELECT id_estado FROM dbo.estados WHERE tipo_estado = 'PROYECTO' AND nombre = N'Finalizado');
DECLARE @tPend int = (SELECT id_estado FROM dbo.estados WHERE tipo_estado = 'TAREA' AND nombre = N'Pendiente');
DECLARE @tProc int = (SELECT id_estado FROM dbo.estados WHERE tipo_estado = 'TAREA' AND nombre = N'En proceso');
DECLARE @tComp int = (SELECT id_estado FROM dbo.estados WHERE tipo_estado = 'TAREA' AND nombre = N'Completada');
IF @cat IS NULL OR @pPlan IS NULL OR @pEjec IS NULL OR @pFin IS NULL OR @tPend IS NULL OR @tProc IS NULL OR @tComp IS NULL
BEGIN
    RAISERROR(N'Faltan los datos iniciales (estados o categorías). Ejecute la sección de datos iniciales del script del Entregable 4.', 16, 1);
    RETURN;
END;

IF NOT EXISTS (SELECT 1 FROM dbo.clientes WHERE identificacion = '999000111')
    INSERT INTO dbo.clientes (id_tipo_cliente, identificacion, nombre_cliente, contacto, telefono1, estado)
    SELECT TOP 1 id_tipo_cliente, '999000111', N'PRUEBA - Cliente de ejemplo S.A.', N'Contacto de prueba', '22223333', 1
    FROM dbo.tipos_cliente ORDER BY id_tipo_cliente;
DECLARE @cli int = (SELECT id_cliente FROM dbo.clientes WHERE identificacion = '999000111');
IF @cli IS NULL
BEGIN
    RAISERROR(N'No se pudo crear el cliente de prueba: falta el catálogo tipos_cliente.', 16, 1);
    RETURN;
END;

-- Proyectos
INSERT dbo.proyectos (nombre, descripcion, id_cliente, id_categoria, id_estado, id_responsable, fecha_inicio, fecha_fin, precio_venta)
VALUES (N'PRUEBA - Análisis de licitaciones 2026', N'Datos ficticios para probar reportes', @cli, @cat, @pEjec, @resp, '2026-10-01', '2026-12-15', 1500000);
DECLARE @p1 int = SCOPE_IDENTITY();

INSERT dbo.proyectos (nombre, descripcion, id_cliente, id_categoria, id_estado, id_responsable, fecha_inicio, fecha_fin, precio_venta)
VALUES (N'PRUEBA - Informe de mercado', N'Datos ficticios para probar reportes', @cli, @cat, @pPlan, @resp, '2026-11-01', '2027-01-31', 900000);
DECLARE @p2 int = SCOPE_IDENTITY();

INSERT dbo.proyectos (nombre, descripcion, id_cliente, id_categoria, id_estado, id_responsable, fecha_inicio, fecha_fin, precio_venta)
VALUES (N'PRUEBA - Tablero de indicadores', N'Datos ficticios para probar reportes', @cli, @cat, @pFin, @resp, '2026-09-01', '2026-09-30', 120000);
DECLARE @p3 int = SCOPE_IDENTITY();

-- Tareas
INSERT dbo.tareas (nombre, id_proyecto, id_categoria, id_estado, fecha_inicio, fecha_fin, tiempo_estimado)
VALUES (N'PRUEBA - Limpieza de datos', @p1, @cat, @tComp, '2026-10-01', '2026-10-10', 16);
DECLARE @t1 int = SCOPE_IDENTITY();
INSERT dbo.tareas (nombre, id_proyecto, id_categoria, id_estado, fecha_inicio, fecha_fin, tiempo_estimado)
VALUES (N'PRUEBA - Modelo de análisis', @p1, @cat, @tProc, '2026-10-11', '2026-11-30', 24);
DECLARE @t2 int = SCOPE_IDENTITY();
INSERT dbo.tareas (nombre, id_proyecto, id_categoria, id_estado, fecha_inicio, fecha_fin, tiempo_estimado)
VALUES (N'PRUEBA - Informe final', @p1, @cat, @tPend, '2026-11-01', '2026-11-05', 10);
DECLARE @t3 int = SCOPE_IDENTITY();
INSERT dbo.tareas (nombre, id_proyecto, id_categoria, id_estado, fecha_inicio, fecha_fin, tiempo_estimado)
VALUES (N'PRUEBA - Recopilación de fuentes', @p2, @cat, @tComp, '2026-11-01', '2026-11-08', 8);
DECLARE @t4 int = SCOPE_IDENTITY();
INSERT dbo.tareas (nombre, id_proyecto, id_categoria, id_estado, fecha_inicio, fecha_fin, tiempo_estimado)
VALUES (N'PRUEBA - Redacción del informe', @p2, @cat, @tPend, '2026-11-09', '2026-12-20', 12);
DECLARE @t5 int = SCOPE_IDENTITY();
INSERT dbo.tareas (nombre, id_proyecto, id_categoria, id_estado, fecha_inicio, fecha_fin, tiempo_estimado)
VALUES (N'PRUEBA - Diseño del tablero', @p3, @cat, @tComp, '2026-09-01', '2026-09-25', 20);
DECLARE @t6 int = SCOPE_IDENTITY();

-- Asignaciones
INSERT dbo.tarea_usuarios (id_tarea, id_usuario)
VALUES (@t1, @resp), (@t2, @resp), (@t3, @resp), (@t4, @resp), (@t5, @resp), (@t6, @resp);
IF @otro IS NOT NULL
    INSERT dbo.tarea_usuarios (id_tarea, id_usuario) VALUES (@t2, @otro), (@t5, @otro);

-- Tiempo registrado (con el costo por hora de cada registro)
INSERT dbo.registros_tiempo (id_tarea, id_usuario, fecha_registro, horas, costo_hora, comentario)
VALUES (@t1, @resp, '2026-10-03', 10, 5000, N'Depuración inicial'),
       (@t1, @resp, '2026-10-08',  8, 5000, N'Normalización'),
       (@t2, @resp, '2026-10-20',  6, 5000, N'Modelo base'),
       (@t4, @resp, '2026-11-04',  6, 6000, N'Recopilación'),
       (@t6, @resp, '2026-09-20', 20, 5000, N'Diseño y pruebas');
IF @otro IS NOT NULL
    INSERT dbo.registros_tiempo (id_tarea, id_usuario, fecha_registro, horas, costo_hora, comentario)
    VALUES (@t2, @otro, '2026-11-02', 4, 5500, N'Revisión del modelo');

PRINT N'Datos de prueba cargados.';
SELECT * FROM dbo.vw_rentabilidad_proyectos WHERE proyecto LIKE N'PRUEBA -%' ORDER BY codigo;
GO

/* ---------------------------------------------------------------------------
   LIMPIEZA: para borrar los datos de prueba, seleccione y ejecute este bloque.

   DELETE r FROM dbo.registros_tiempo r JOIN dbo.tareas t ON t.id_tarea = r.id_tarea WHERE t.nombre LIKE N'PRUEBA -%';
   DELETE a FROM dbo.tarea_usuarios a JOIN dbo.tareas t ON t.id_tarea = a.id_tarea WHERE t.nombre LIKE N'PRUEBA -%';
   DELETE FROM dbo.tareas WHERE nombre LIKE N'PRUEBA -%';
   DELETE FROM dbo.proyectos WHERE nombre LIKE N'PRUEBA -%';
   DELETE FROM dbo.clientes WHERE identificacion = '999000111';
   --------------------------------------------------------------------------- */
