# CONTROLEX – Plan de pruebas

Versión del sistema: 1.0.0 · Fecha de ejecución: ____ · Probadores: ____

**Cómo usarlo:** en la columna *Resultado* anote ✔ (aprobado) o ✘ (falló, con una nota breve). Las pruebas que fallen se corrigen y se repiten.

## 1. Preparación
Cree estos usuarios desde Catálogos → Usuarios (cada uno con correo propio):

| Usuario | Rol | Costo por hora | Para qué sirve |
|---|---|---|---|
| mhernandez | Administrador | ₡5.000 | Pruebas de acceso total |
| soporte1 | Soporte TI | ₡5.500 | Pruebas del rol intermedio |
| empleado1 | Empleado | ₡4.000 | Pruebas de acceso parcial |
| empleado2 | Empleado | ₡4.500 | Verificar que no ve lo ajeno |

Datos: ejecute `database/sql/002_datos_prueba_reportes.sql` **solo en el ambiente de pruebas**, o cree a mano 2 clientes, 3 proyectos y 6 tareas. Asigne tareas a `empleado1` y a `empleado2`.

## 2. Ingreso (RF-001)
| ID | Caso | Pasos | Resultado esperado | Resultado |
|---|---|---|---|---|
| LOG-01 | Ingreso correcto | Usuario y clave válidos | Entra a Inicio; arriba aparecen su nombre y rol | |
| LOG-02 | Clave incorrecta | Usuario válido, clave errónea | Alerta "Usuario o contraseña incorrectos"; queda LOGIN FALLIDO en la bitácora | |
| LOG-03 | Usuario inexistente | Usuario que no existe | La misma alerta | |
| LOG-04 | Usuario inhabilitado | Inhabilitar un usuario y tratar de entrar | No entra | |
| LOG-05 | Campos vacíos | Presionar Iniciar sesión sin datos | Mensajes de campos obligatorios | |
| LOG-06 | Cancelar | Escribir datos y presionar Cancelar | Los campos se limpian | |
| LOG-07 | Clave oculta | Escribir la contraseña | Los caracteres no se ven | |

## 3. Contraseñas y sesión (RF-006, RF-015, RF-022, RF-023)
| ID | Caso | Pasos | Resultado esperado | Resultado |
|---|---|---|---|---|
| SEG-01 | Clave temporal | Entrar con un usuario recién creado | Lo lleva a Cambiar contraseña y no puede abrir otras opciones | |
| SEG-02 | Clave actual incorrecta | Cambiar contraseña con la actual errónea | Rechaza con mensaje | |
| SEG-03 | Complejidad | Nueva clave de menos de 8 caracteres, o sin números o sin especiales | Rechaza con el mensaje correspondiente | |
| SEG-04 | Confirmación distinta | Confirmar con otro texto | Rechaza | |
| SEG-05 | Cambio correcto | Cambiar con datos válidos | Mensaje de éxito; ya navega; queda CAMBIO DE CONTRASEÑA en la bitácora | |
| SEG-06 | Recuperar contraseña | Usuario y cédula correctos | Llega un correo con clave temporal; al entrar con ella obliga a cambiarla | |
| SEG-07 | Recuperar con datos erróneos | Cédula que no coincide | Alerta de que los datos no coinciden | |
| SEG-08 | Inactividad | Dejar la sesión sin usar 15 minutos (para probar rápido, poner `MinutosInactividad = 1` y volver a 15) | Pide iniciar sesión; en Bitácoras → Ingresos la salida es la última actividad + el tiempo de inactividad | |
| SEG-09 | Cerrar sesión | Cerrar sesión | Vuelve al login; la salida queda registrada | |

## 4. Menú y roles (RF-002, RF-010)
| ID | Caso | Pasos | Resultado esperado | Resultado |
|---|---|---|---|---|
| MEN-01 | Administrador | Entrar como mhernandez | Ve todas las opciones | |
| MEN-02 | Soporte TI | Entrar como soporte1 | No ve Roles y permisos; en Usuarios puede crear, editar e inhabilitar | |
| MEN-03 | Empleado | Entrar como empleado1 | No ve Rentabilidad, Bitácoras, Catálogos, Crear proyecto ni Crear tarea | |
| MEN-04 | Acceso por dirección | Como empleado1, escribir `/Usuarios` | "Acceso denegado" | |
| MEN-05 | Acceso por dirección (2) | Como empleado1, escribir `/Proyectos/Crear` y `/Tareas/Crear` | "Acceso denegado" en ambas | |
| MEN-06 | Opciones comunes | Cualquier rol | Acerca de, Ayuda y Cerrar sesión visibles | |

## 5. Usuarios (RF-009)
| ID | Caso | Pasos | Resultado esperado | Resultado |
|---|---|---|---|---|
| USU-01 | Crear válido | Completar todos los campos | Se crea; mensaje de éxito | |
| USU-02 | Cédula con letras | Escribir letras en cédula | Rechaza | |
| USU-03 | Nombre con números | Escribir números en nombre | Rechaza | |
| USU-04 | Correo inválido | Escribir un correo sin formato | Rechaza | |
| USU-05 | Datos repetidos | Repetir cédula, correo y usuario (uno por uno) | Rechaza cada caso | |
| USU-06 | Editar sin cambiar clave | Editar y dejar la clave vacía | Se actualiza; la clave anterior sigue valiendo | |
| USU-07 | Inhabilitar y habilitar | Inhabilitar y luego habilitar | El inhabilitado no entra; al habilitarlo sí | |
| USU-08 | Autoinhabilitar | Inhabilitar el propio usuario | Rechaza | |

## 6. Roles y permisos (RF-010)
| ID | Caso | Pasos | Resultado esperado | Resultado |
|---|---|---|---|---|
| ROL-01 | Rol nuevo | Crear un rol con Tareas = Parcial y asignarlo a un usuario | Tras un nuevo ingreso, solo ve lo permitido | |
| ROL-02 | Nombre repetido | Crear un rol con un nombre existente | Rechaza | |
| ROL-03 | Único administrador | Quitar el acceso administrativo al único rol administrador | Rechaza | |
| ROL-04 | Aplicación de cambios | Cambiar permisos de un rol con un usuario conectado | El cambio se aplica en su siguiente inicio de sesión | |

## 7. Clientes (RF-008)
| ID | Caso | Pasos | Resultado esperado | Resultado |
|---|---|---|---|---|
| CLI-01 | Crear válido | Completar los datos | Se registra | |
| CLI-02 | Identificación repetida | Repetir la identificación | Rechaza | |
| CLI-03 | Teléfono con letras | Escribir letras | Rechaza | |
| CLI-04 | Correo inválido | Formato incorrecto | Rechaza | |
| CLI-05 | Inhabilitar | Inhabilitar un cliente | No aparece para nuevos proyectos | |

## 8. Proyectos (RF-003)
| ID | Caso | Pasos | Resultado esperado | Resultado |
|---|---|---|---|---|
| PRY-01 | Crear | Completar y guardar | Se genera el código PRY-000X | |
| PRY-02 | Fechas | Fecha de fin anterior a la de inicio | Rechaza | |
| PRY-03 | Obligatorios | Dejar vacío cliente, categoría o responsable | Mensajes de campo obligatorio | |
| PRY-04 | Editar | Cambiar datos y actualizar | Se guardan los cambios | |
| PRY-05 | Consultar (acceso total) | Abrir un proyecto | Ve precio, costo, rentabilidad y margen | |
| PRY-06 | Finalizar con pendientes | Pasar a Finalizado con tareas sin completar | Rechaza e indica cuántas faltan | |
| PRY-07 | Finalizar completo | Completar todas las tareas y finalizar | Lo permite | |
| PRY-08 | Empleado | Entrar como empleado1 | Solo ve proyectos donde participa, sin montos y solo sus tareas | |
| PRY-09 | Tarea desde el proyecto | "Nueva tarea en este proyecto" | El formulario abre con ese proyecto ya seleccionado | |
| PRY-10 | Proyecto cerrado | Abrir un proyecto finalizado o cancelado | No ofrece crear tareas ni lo permite | |

## 9. Tareas y tiempo (RF-004)
| ID | Caso | Pasos | Resultado esperado | Resultado |
|---|---|---|---|---|
| TAR-01 | Crear | Crear una tarea con 2 usuarios | Se genera el código TAR-000X | |
| TAR-02 | Sin asignados | Guardar sin usuarios | Rechaza | |
| TAR-03 | Fechas | Fin anterior al inicio | Rechaza | |
| TAR-04 | Alcance del empleado | empleado1 abre Ver tareas y una tarea ajena por dirección | Solo ve las suyas; la ajena da "acceso denegado" | |
| TAR-05 | Registrar tiempo | empleado1 registra 2 h | Aparece en el historial y suma en "Registrado" | |
| TAR-06 | Fecha futura | Registrar con fecha de mañana | Rechaza | |
| TAR-07 | Tope diario | Registrar más de 24 h en un día entre tareas | Rechaza | |
| TAR-08 | Horas inválidas | 0 o negativas | Rechaza | |
| TAR-09 | No asignado | Un usuario no asignado intenta registrar | No puede | |
| TAR-10 | A nombre de otro | El administrador registra por un asignado | Se registra a nombre de ese usuario | |
| TAR-11 | Estados | Pasar a En proceso y luego a Completada | Se llenan las fechas de inicio y fin vacías | |
| TAR-12 | Comentarios | Escribir y actualizar | Se guardan | |
| TAR-13 | Eliminar registro | empleado1 elimina uno suyo de hoy, y otro de otro día | El de hoy sí; el otro no | |
| TAR-14 | Quitar asignado | Quitar a quien ya registró tiempo | Rechaza | |
| TAR-15 | Costo en cero | Registrar tiempo de un usuario con costo ₡0 | Muestra el aviso | |
| TAR-16 | Vencidas | Tarea con fecha de fin pasada y sin completar | Aparece "Vencida" | |

## 10. Reportes (RF-005, RF-024)
| ID | Caso | Pasos | Resultado esperado | Resultado |
|---|---|---|---|---|
| REP-01 | Rentabilidad total | Consultar sin filtros | Los totales cuadran con la suma de los proyectos | |
| REP-02 | Costo a mano | Para un proyecto, sumar horas × costo por hora | Coincide con el costo del reporte | |
| REP-03 | Filtros | Por proyecto y por fechas | Muestra solo lo filtrado | |
| REP-04 | PDF de rentabilidad | Exportar PDF | Se abre en otra pestaña con los mismos datos, página y "generado por" | |
| REP-05 | Eficiencia | Consultar eficiencia | Estimado ÷ registrado correcto | |
| REP-06 | Pendientes | Consultar pendientes | No incluye completadas; marca las vencidas | |
| REP-07 | Filtros de tareas | Proyecto, usuario, estado y período | Muestra solo lo filtrado | |
| REP-08 | Empleado | empleado1 abre los reportes | Solo sus tareas; sin acceso a rentabilidad | |
| REP-09 | PDF de tareas | Exportar PDF | Igual que la pantalla | |
| REP-10 | Costo histórico | Cambiar el costo por hora de un usuario | Los registros ya hechos conservan su costo | |

## 11. Bitácoras (RF-007, RF-011, RF-012)
| ID | Caso | Pasos | Resultado esperado | Resultado |
|---|---|---|---|---|
| BIT-01 | Ingresos | Iniciar sesión | Aparece con IP y hora de entrada | |
| BIT-02 | Sesión activa | Ver la bitácora con la sesión abierta y luego de cerrarla | "Sesión activa" y luego la hora de salida | |
| BIT-03 | Filtros de ingresos | Usuario y fechas | Muestra solo lo filtrado | |
| BIT-04 | Filtros de movimientos | Tipo y módulo | Muestra solo lo filtrado | |
| BIT-05 | Paginación | Más de 50 registros | Anterior y Siguiente conservan los filtros | |
| BIT-06 | Empleado | empleado1 intenta abrir Bitácoras | "Acceso denegado" | |
| BIT-07 | Trazabilidad | Crear usuario, proyecto y tarea, registrar tiempo | Cada acción aparece en Movimientos | |

## 12. Inicio, Acerca de y Ayuda (RF-013, RF-014)
| ID | Caso | Pasos | Resultado esperado | Resultado |
|---|---|---|---|---|
| INF-01 | Acerca de | Abrir la opción | Muestra los siete datos del requerimiento | |
| INF-02 | Ayuda | Abrir el manual y usar el índice | Cada enlace lleva a su sección; imprimir funciona | |
| INF-03 | Inicio | Revisar las tarjetas | Coinciden con los datos; el administrador ve proyectos en ejecución | |

## 13. Requerimientos no funcionales y validaciones
| ID | Caso | Pasos | Resultado esperado | Resultado |
|---|---|---|---|---|
| NF-01 | HTTPS (RF-021) | Abrir por http | Redirige a https; con el certificado instalado no hay advertencia | |
| NF-02 | Cookie protegida | En las herramientas del navegador, revisar la cookie de sesión | Marcada HttpOnly y Secure | |
| NF-03 | Contraseñas cifradas | En SQL: `SELECT TOP 5 contrasena_hash FROM dbo.usuarios` | Todas empiezan con `v1.` | |
| NF-04 | Concurrencia (RNF-005) | 5 usuarios navegan y guardan a la vez | Sin errores ni lentitud notoria | |
| NF-05 | Disponibilidad (RNF-004) | Reiniciar el servidor | El servicio arranca solo | |
| NF-06 | Usabilidad (RNF-003) | Un empleado nuevo registra tiempo usando el manual | Lo logra en menos de 5 minutos | |
| NF-07 | Celular | En un celular, abrir el sistema | Menú desplegable, tablas desplazables y formularios cómodos | |
| NF-08 | Validaciones (RF-016) | Probar campos numéricos, de solo letras, correos y fechas en cada pantalla | Rechazan datos inválidos con mensaje claro | |

## 14. Resumen
Casos ejecutados: ____ · Aprobados: ____ · Fallidos: ____ · Fecha de cierre: ____
