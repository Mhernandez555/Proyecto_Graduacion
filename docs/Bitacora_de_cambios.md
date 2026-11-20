# Bitácora de cambios

Registro de cambios funcionales y de base de datos de CONTROLEX.

## 2026-11-20 — Tema nocturno y mejoras de tareas

**Módulos:** Interfaz general y Tareas
**Estado:** cambio implementado; no requiere cambios de base de datos.

### Cambios en la aplicación

- Se reemplaza el control textual del tema por un selector compacto con iconos de sol y luna, etiquetas «Claro» y «Oscuro», ubicado junto al usuario autenticado.
- La vista de detalle de tareas presenta en paralelo los formularios para actualizar estado/comentarios y registrar tiempo, apilándolos en pantallas estrechas.
- Los campos de texto de esos formularios conservan un tamaño fijo y ya no se pueden redimensionar.
- Se ocultan los registros informativos de comandos SQL de EF Core en la terminal; advertencias y errores siguen disponibles.

## 2026-11-16 — Participantes de proyectos y asignación de tareas

**Módulos:** Proyectos y Tareas  
**Estado:** cambio implementado en la aplicación; requiere ejecutar el script SQL en cada base de datos antes de desplegar esta versión.

### Motivo

Permitir asignar varios usuarios activos a un proyecto y limitar la selección de usuarios de cada tarea a los participantes de ese proyecto. La asignación de usuarios a proyectos no estaba persistida anteriormente.

### Cambios en la base de datos

- Se agrega `dbo.proyecto_usuarios`, relación entre proyectos y usuarios con clave primaria compuesta (`id_proyecto`, `id_usuario`) y fecha de asignación.
- Se agregan claves foráneas hacia `dbo.proyectos` y `dbo.usuarios`.
- Se inicializan las relaciones existentes con el responsable de cada proyecto y con los usuarios ya asignados a sus tareas. Así se conservan las asignaciones actuales al introducir la nueva restricción.
- El script es `database/sql/006_usuarios_proyecto.sql`; crea la tabla si no existe y evita duplicar relaciones existentes.

### Cambios en la aplicación

- Los formularios de proyecto permiten buscar y seleccionar varios participantes activos. Al crear un proyecto, el usuario actual aparece seleccionado inicialmente.
- El formulario de tarea carga los participantes del proyecto seleccionado y permite buscarlos y seleccionar varios.
- La aplicación valida en el servidor que los usuarios asignados a una tarea pertenezcan al proyecto.
- No se permite quitar de un proyecto a un participante que siga asignado a alguna de sus tareas; primero debe desasignarse de esas tareas.
- La participación en un proyecto permite a los usuarios con acceso parcial verlo, además de las reglas de visibilidad existentes.

### Despliegue

1. Respaldar la base de datos.
2. Ejecutar `database/sql/006_usuarios_proyecto.sql` en `CONTROLEX_DB`, después de los scripts de actualización anteriores (`003`, `004` y `005`).
3. Desplegar la versión de la aplicación que incluye este cambio.

No desplegar la nueva aplicación antes de ejecutar el script: la aplicación consulta `dbo.proyecto_usuarios` para cargar y validar participantes.
