# Ajustes pendientes a los Entregables 2, 3 y 4

Cambios surgidos durante el diseño y el desarrollo que deben reflejarse en los documentos antes de la entrega final. Las páginas de Word las edita el equipo; aquí está qué cambiar y con qué texto.

---
## Entregable 2 – Requerimientos

### Requerimientos nuevos
Agregar a la sección 2.3.1 y al cuadro resumen (2.4.1), con el texto ya redactado en el Entregable 4 (sección 4.3.1):
- **RF-022 Cambiar contraseña**
- **RF-023 Recuperar contraseña**
- **RF-024 Reporte de eficiencia de tareas y pendientes**

### Requerimientos que cambian
| Requerimiento | Ajuste |
|---|---|
| **RF-002 Menú principal** | Opciones finales: Inicio; Proyectos (Crear proyecto, Ver proyectos); Tareas (Crear tarea, Ver tareas); Reportes (Rentabilidad de proyectos, Eficiencia y pendientes); Seguridad (Roles y permisos, Cambiar contraseña); Bitácoras (Ingresos de usuario, Movimientos de usuario); Catálogos (Usuarios, Clientes); Acerca de; Ayuda; Cerrar sesión. Se quitan **Estados** y **Categorías** del menú: son tablas de apoyo con valores predefinidos. |
| **RF-003 Gestión de proyectos** | Agregar el campo **Categoría**. *Costo acumulado* y *Rentabilidad* pasan a ser campos de solo lectura, calculados. Agregar los botones Consultar y Cambiar estado. Regla: un proyecto solo se finaliza si todas sus tareas están completadas. |
| **RF-004 Gestión de tareas** | Agregar **Tiempo estimado**. El campo único *Tiempo registrado* se reemplaza por el **registro de tiempo por usuario y fecha** (horas, costo por hora vigente y comentario). Reglas: solo los asignados registran tiempo; la fecha no puede ser futura; máximo 24 horas por día por persona; no se quita a un asignado que ya registró tiempo. |
| **RF-006 Gestión de seguridad** | Contraseña de mínimo 8 caracteres con letras, números y caracteres especiales. La sesión se cierra tras **15 minutos de inactividad**. |
| **RF-007 / RF-011 Bitácoras** | Una sesión cerrada por inactividad registra como salida su última actividad más 15 minutos. Las bitácoras se consultan con filtros y paginación de 50 registros. |
| **RF-009 Mantenimiento de usuarios** | Agregar **Correo electrónico** (necesario para recuperar la contraseña) y **Costo por hora** (necesario para calcular el costo de las tareas). |
| **RF-010 Roles y permisos** | Los permisos se otorgan **por módulo con tres niveles**: Sin acceso, Parcial y Total. Módulos: Proyectos, Tareas, Reporte de rentabilidad, Reporte de tareas, Bitácoras, Usuarios, Clientes y Roles y permisos. Se quitan los permisos de *estados* y *categorías*. Los cambios aplican en el siguiente inicio de sesión. |
| **RF-005 Reporte de rentabilidad** | Sin cambios de fondo. Aclarar: el costo es horas registradas × costo por hora de cada registro, y el período filtra por la fecha de inicio del proyecto. |

### Inconsistencias que conviene corregir
- **RNF-001** dice Windows 11, mientras el Entregable 3 toma Windows 10 como mínimo requerido. Unificar.
- En el índice y los títulos de 2.3.1, **"RNF-020" y "RNF-021"** son requerimientos funcionales (RF-020 y RF-021).
- RF-019 y su resumen mencionan "ASP.NET MVC" en el cuadro: usar "ASP.NET Core MVC", como en el resto.

### Glosario
Agregar: **Hash**, **HTTPS**, **SMTP**, **Servicio de Windows**, **Bitácora de movimientos**.

---
## Entregable 3 – Factibilidad

### Factibilidad técnica – software
Agregar a la tabla de software:
| Tipo | Software actual | Software requerido | Análisis |
|---|---|---|---|
| Librería de exportación a PDF | QuestPDF 2026.6.1 (licencia Community) | QuestPDF o equivalente | Gratuita para organizaciones con ingresos menores a 1 millón de dólares; WallRed cumple. Sin costo. |
| Acceso a datos | Entity Framework Core 10 | Entity Framework Core 10 | Libre distribución. Sin costo. |
| Correo saliente | Cuenta de correo con SMTP | Cuenta SMTP | Se usa una cuenta existente de la empresa (con Gmail, "contraseña de aplicación"). Sin costo adicional. |
| Certificado HTTPS | Certificado autofirmado | Certificado válido | Suficiente para la red interna; instalado en los equipos. Un certificado comercial es opcional. |

### Factibilidad operacional
- Agregar **RF-022, RF-023 y RF-024** con factibilidad 100 % (se desarrollan conforme a lo requerido) en la sección 3.2.2 y en el cuadro 3.3.1.
- La factibilidad operacional general sigue en 100 %.

### Factibilidad económica
- Agregar una línea de aclaración: la librería de PDF, el acceso a datos, el correo y el certificado HTTPS no generan costos. El resumen de costos no cambia: hardware ₡0, software ₡0.
- Revisar la cifra de desarrollo si el tiempo real de los estudiantes fue distinto al mes efectivo estimado.

---
## Entregable 4 – Diseño

### 4.2 Aprobado por el usuario
- **Anexo 1:** agregar las filas RF-022, RF-023 y RF-024 con factibilidad 100 % ("Se cumple en su totalidad con el requerimiento establecido").
- Antes de firmar, volver a revisar las fechas del acuerdo.

### 4.3.1 Diseño conceptual
- La matriz ya incluye RF-022 a RF-024. Confirmar que coincide con los permisos iniciales de la base de datos (tabla `permisos`).

### 4.3.2.4 Estructura de base de datos
- **Tabla 12 `bitacora_ingresos`:** agregar el campo
  | Campo | Tipo de dato | Nulo | Clave | Restricciones |
  |---|---|---|---|---|
  | fecha_hora_ultima_actividad | datetime2(0) | Sí | - | - |

  Script: `ALTER TABLE dbo.bitacora_ingresos ADD fecha_hora_ultima_actividad datetime2(0) NULL;` (archivo `003_ultima_actividad_sesion.sql`).
- **Diccionario de datos:** agregar el campo con la descripción: "Última vez que el usuario usó el sistema en esa sesión; permite calcular la hora de salida cuando la sesión se cierra por inactividad".
- Indicar en el texto que el número de tablas sigue siendo 13 y la vista `vw_rentabilidad_proyectos`.

### 4.3.2.1 y 4.3.2.3 Diagramas
- El diagrama de contexto ya incluye el servicio de correo. Mencionar que la aplicación accede directamente a SQL Server (no hay capa de servicios intermedia).
- Los flujos 1 a 10 siguen vigentes. El flujo de **Login** debe incluir la redirección a Cambiar contraseña cuando la clave es temporal.

### 4.3.3 Diseño físico
- Reemplazar los prototipos por **capturas del sistema terminado**.
- Pantallas adicionales a documentar: Detalle de proyecto, Detalle de tarea (registro de tiempo y estado), Bitácora de ingresos, Bitácora de movimientos y la vista del sistema en celular.
- Aclarar que el nombre de la base es el de la instalación real (`CONTROLEX_DB`).

### 4.6 Anexos
- Agregar: `CONTROLEX_DB.sql`, `002_datos_prueba_reportes.sql` (solo pruebas), `003_ultima_actividad_sesion.sql`, la **Guía de instalación** y el **Plan de pruebas** con los resultados.

---
## Entregable final / defensa
- Adjuntar el **Plan de pruebas** completado con resultados.
- Dejar el sistema instalado según la **Guía de instalación** y verificar los pasos del punto 7 de esa guía antes de la presentación.
