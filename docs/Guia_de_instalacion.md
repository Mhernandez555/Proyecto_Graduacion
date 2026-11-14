# CONTROLEX – Guía de instalación en el equipo del cliente

Deja CONTROLEX funcionando en un equipo de WallRed, con HTTPS (RF-021) y como servicio de Windows que arranca solo.

## 1. Qué se necesita
- Un equipo **servidor de aplicaciones** con Windows 10/11 o Windows Server, y permisos de administrador.
- **ASP.NET Core Runtime 10 (x64)**, que se descarga de dotnet.microsoft.com. No hace falta el SDK.
- **SQL Server** (la máquina virtual de WallRed) con acceso por red desde el servidor de aplicaciones.
- Una cuenta de correo con acceso SMTP, para la recuperación de contraseña.

## 2. Base de datos
En SSMS, contra el servidor de WallRed y en este orden:
1. `CONTROLEX_DB.sql` (Entregable 4): tablas, vista de rentabilidad y datos iniciales.
2. `database/sql/003_ultima_actividad_sesion.sql`.
3. `database/sql/004_motivo_salida_sesion.sql`.
4. `database/sql/005_direccion_ip_ipv6.sql`.
5. **No** ejecutar `002_datos_prueba_reportes.sql` (son datos ficticios).
6. Crear el login que usará la aplicación (cambie la clave):
   ```sql
   CREATE LOGIN controlex_app WITH PASSWORD = 'UnaClaveLargaYUnica1!', CHECK_POLICY = OFF;
   GO
   USE CONTROLEX_DB;
   GO
   CREATE USER controlex_app FOR LOGIN controlex_app;
   ALTER ROLE db_datareader ADD MEMBER controlex_app;
   ALTER ROLE db_datawriter ADD MEMBER controlex_app;
   ```
   El servidor debe tener activado el modo "SQL Server and Windows Authentication" (y reiniciado el servicio).
7. Clave del administrador inicial: en el equipo donde está publicada la aplicación,
   ```
   C:\controlex\app\Controlex.Web.exe hash "ClaveTemporal1!"
   ```
   imprime un hash. Guárdelo así:
   ```sql
   UPDATE dbo.usuarios SET contrasena_hash = 'PEGUE_EL_HASH', requiere_cambio_contrasena = 1, estado = 1
   WHERE nombre_usuario = 'mhernandez';
   ```

## 3. Publicar la aplicación
En el equipo de desarrollo, desde la raíz del repositorio:
```
dotnet publish Controlex.Web.csproj -c Release -o C:\controlex\app
```
Copie la carpeta `C:\controlex\app` al servidor (misma ruta). Debe contener `Controlex.Web.exe`, `config\appsettings.json` y `public\assets\...`. El archivo `appsettings.Development.json` nunca se publica.

## 4. Configuración de producción
1. En el servidor, copie `config\appsettings.Production.example.json` como `config\appsettings.Production.json`.
2. Complete la cadena de conexión (servidor, base `CONTROLEX_DB`, usuario `controlex_app` y su clave), la sección `Correo` y la ruta del certificado.
3. Este archivo no se sube a Git. Si la clave de SQL cambia, se cambia solo ahí.
4. Si se publica detrás de un proxy inverso, agregue su IP exacta a `Red:ProxiesConfiables` en `appsettings.Production.json` y configure el proxy para enviar `X-Forwarded-For` y `X-Forwarded-Proto`. No agregue IPs de clientes ni rangos no confiables. En la instalación directa con Kestrel, deje la lista vacía.

## 5. Certificado HTTPS (red interna)
En PowerShell como administrador. Use como nombre el que los usuarios escribirán en el navegador (ejemplo `controlex.wallred.local`):
```powershell
New-Item -ItemType Directory -Force C:\controlex\certs | Out-Null
$cert = New-SelfSignedCertificate -DnsName "controlex.wallred.local","localhost" `
        -CertStoreLocation Cert:\LocalMachine\My -NotAfter (Get-Date).AddYears(2)
$clave = Read-Host -AsSecureString "Clave del certificado"
Export-PfxCertificate -Cert $cert -FilePath C:\controlex\certs\controlex.pfx -Password $clave
Export-Certificate    -Cert $cert -FilePath C:\controlex\certs\controlex.cer
```
- El `.pfx` se queda en el servidor. La clave del `.pfx` **no** se escribe en ningún archivo: se guarda como variable del sistema:
  ```
  setx /M Kestrel__Endpoints__Https__Certificate__Password "CLAVE_DEL_PFX"
  ```
- Para que los navegadores no muestren advertencia, instale `controlex.cer` en cada equipo cliente:
  ```powershell
  Import-Certificate -FilePath .\controlex.cer -CertStoreLocation Cert:\LocalMachine\Root
  ```
- Los usuarios deben abrir el sistema por ese **nombre**. Si ingresan por la IP, el certificado no coincide y el navegador avisará.
- Con un certificado comercial o de una autoridad propia, solo se cambia el `.pfx` y su clave.

## 6. Servicio de Windows
En PowerShell como administrador:
```powershell
sc.exe create CONTROLEX binPath= "C:\controlex\app\Controlex.Web.exe" start= auto DisplayName= "CONTROLEX"
sc.exe description CONTROLEX "Sistema de gestión de proyectos y tareas de WallRed"
New-NetFirewallRule -DisplayName "CONTROLEX HTTPS" -Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow
sc.exe start CONTROLEX
```
Si cambia la variable de la clave del certificado, reinicie el servicio: `sc.exe stop CONTROLEX` y `sc.exe start CONTROLEX`.

## 7. Verificación
1. Desde otro equipo, abra `https://controlex.wallred.local`: debe cargar con candado y pedir inicio de sesión.
2. Entre como `mhernandez`: el sistema pide cambiar la contraseña.
3. Abra `http://controlex.wallred.local`: debe redirigir a https. (Para esto, el puerto 80 debe estar atendido; si no, basta con que https funcione.)
4. En Bitácoras → Ingresos debe aparecer su ingreso con la IP del equipo.
5. Reinicie el servidor: el servicio debe arrancar solo.

## 8. Respaldo de la base de datos
Al menos semanal, y antes de cada actualización:
```sql
BACKUP DATABASE CONTROLEX_DB TO DISK = 'D:\respaldos\CONTROLEX_DB.bak' WITH INIT, COMPRESSION;
```
Guarde una copia fuera del servidor.

## 9. Instalar una versión nueva
1. Respaldar la base de datos.
2. Detener el servicio: `sc.exe stop CONTROLEX`.
3. Publicar y copiar la carpeta nueva, **conservando** `config\appsettings.Production.json`.
4. Ejecutar los scripts SQL nuevos, si los hay.
5. Iniciar el servicio: `sc.exe start CONTROLEX`.

## 10. Problemas frecuentes
| Síntoma | Causa probable |
|---|---|
| El servicio no inicia | Falta `config\appsettings.Production.json`, la cadena de conexión está vacía o el certificado/clave no coincide. Revise el Visor de eventos → Aplicación. |
| `Login failed for user 'controlex_app'` | Clave distinta a la del login de SQL, o el modo mixto no está activo. |
| El navegador avisa que el sitio no es seguro | El certificado no está instalado en ese equipo o se entró por IP y no por el nombre. |
| No carga desde otros equipos | Falta la regla de firewall del puerto 443 o el nombre no se resuelve (agregarlo al DNS o al archivo hosts). |
| No llegan los correos de recuperación | Revise la sección `Correo`. Con Gmail se necesita una "contraseña de aplicación". |
| Las sesiones no registran salida | Verifique que se ejecutaron `003_ultima_actividad_sesion.sql` y `004_motivo_salida_sesion.sql`, y revise el Visor de eventos → Aplicación. El cierre de una pestaña no notifica al servidor: se registra como inactividad después de 15 minutos. |
