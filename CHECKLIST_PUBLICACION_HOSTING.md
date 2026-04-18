📋 CHECKLIST PARA PUBLICAR EN HOSTING - Planificación y Gestión de Eventos

✅ PUBLISH ACTUALIZADO
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Fecha: 18/4/2026 12:05 a.m.
Ubicación: E:\Proyecto_gestion_eventos\publish\
Total de archivos: 409
DLL Principal: PlanificacionGestionEventos.dll (0.6 MB) ✅
.NET Version: 8.0
Base de datos: SQL Server
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📦 PASO 1: PREPARACIÓN EN EL HOSTING
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
□ Verificar que el hosting tenga .NET 8 Runtime instalado
  └─ Contactar a soporte si no lo tiene
□ Crear una base de datos SQL Server vacía
□ Crear usuario SQL Server con permisos db_owner
□ Obtener la connection string del hosting
  ├─ Formato: Server=<servidor>;Database=<base_datos>;User Id=<usuario>;Password=<contraseña>;
  └─ Ej: Server=192.168.1.100;Database=PlanificacionEventos;User Id=sa;Password=Secure@123;

🗂️ PASO 2: COPIAR ARCHIVOS DEL PUBLISH
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Copiar TODA la carpeta: E:\Proyecto_gestion_eventos\publish\
  ├─ PlanificacionGestionEventos.dll ✅
  ├─ appsettings.json ✅
  ├─ web.config ✅
  ├─ wwwroot/ ✅
  │  ├─ css/
  │  ├─ js/
  │  └─ uploads/ (crear si no existe)
  └─ todos los archivos .dll

Opciones de copia:
1. FTP/SFTP: Conectar con cliente FTP y subir la carpeta
2. Git: Push a repositorio y pull en el hosting
3. Panel de Control: Upload ZIP desde panel de hosting

⚙️ PASO 3: ACTUALIZAR APPSETTINGS.JSON
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Editar el archivo appsettings.json en el hosting:

{
  "ConnectionStrings": {
    "DefaultConnection": "Server=<tu_servidor>;Database=<tu_base_datos>;User Id=<tu_usuario>;Password=<tu_contraseña>;"
  },
  "Smtp": {
    "Host": "smtp.gmail.com",      // ← Cambiar por tu proveedor SMTP
    "Port": 587,
    "User": "tu_email@gmail.com",
    "Pass": "tu_contraseña_app",
    "From": "tu_email@gmail.com",
    "EnableSsl": true
  }
}

🗄️ PASO 4: APLICAR MIGRACIONES DE BD
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Opción A - Automático (recomendado):
  1. En local: dotnet-ef migrations bundle
  2. Copiar el archivo .exe generado al hosting
  3. Ejecutar el .exe para aplicar todas las migraciones

Opción B - Manual desde el hosting:
  1. SSH/RDP al servidor
  2. cd C:\inetpub\wwwroot\PlanificacionGestionEventos
  3. dotnet PlanificacionGestionEventos.dll --migrate

✅ Las migraciones incluidas son:
  • InitialCreate
  • AgregarImagenesEventos
  • AgregarEstadoEventos
  • InitialCategoriaMaximo
  • AddEventoHoraInicioyFin
  • UsuarioIdNullable ✅ (NUEVA - para invitaciones por correo)
  • MakeUsuarioIdNullableInvitaciones

🔧 PASO 5: CONFIGURAR EL SERVIDOR WEB
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Para IIS:
  1. Abrir IIS Manager
  2. Crear nuevo Site:
     └─ Nombre: PlanificacionGestionEventos
     └─ Ruta: C:\inetpub\wwwroot\PlanificacionGestionEventos
     └─ Pool de aplicaciones: .NET 8.0
  3. Habilitar HTTPS si es posible
  4. Configurar permisos de carpeta (IUSR, IIS_IUSRS)
  5. Crear carpeta "uploads" con permisos de escritura

Para Linux/Docker:
  1. Exponer puerto 80/443
  2. Configurar reverse proxy (nginx/Apache)
  3. SSL con Let's Encrypt

📁 PASO 6: VERIFICAR PERMISOS DE CARPETAS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
La carpeta wwwroot/uploads debe ser escribible:
  ├─ Windows: Permisos NTFS para IUSR/IIS_IUSRS
  ├─ Linux: chmod 755 uploads/
  └─ Docker: Volumen montado con permisos

🌐 PASO 7: VERIFICAR LA APLICACIÓN
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
□ Acceder a: https://tu_dominio.com
□ Ver página de inicio: ✅ debe cargar
□ Intentar login: ✅ debe funcionar
□ Registrarse como Participante: ✅
□ Registrarse como Organizador: ✅
□ Crear evento (como Organizador): ✅
□ Enviar invitación por correo: ✅ verificar que no da error NULL
□ Ver Dashboard: ✅ debe mostrar eventos

🔍 POSIBLES PROBLEMAS Y SOLUCIONES
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

❌ "Connection to the database failed"
  └─ ✅ Verificar connection string en appsettings.json
  └─ ✅ Verificar que la BD existe en SQL Server
  └─ ✅ Verificar credenciales SQL Server
  └─ ✅ Verificar firewall del hosting permite conexión a BD

❌ "No se puede insertar NULL en UsuarioId"
  └─ ✅ SOLUCIONADO: Migración aplicada correctamente
  └─ ✅ Verificar que las migraciones se ejecutaron sin error

❌ "Imágenes no se guardan"
  └─ ✅ Verificar permisos de wwwroot/uploads/
  └─ ✅ Verificar espacio disponible en disco

❌ "Error de SMTP al enviar correos"
  └─ ✅ Verificar host SMTP (Gmail, Outlook, etc.)
  └─ ✅ Verificar usuario y contraseña de aplicación
  └─ ✅ Si es Gmail: Activar contraseña de aplicación

❌ "Página en blanco o error 500"
  └─ ✅ Revisar logs en: C:\inetpub\logs\LogFiles\
  └─ ✅ Habilitar custom error page en web.config
  └─ ✅ Conectarse por SSH/RDP y revisar consola

📊 INFORMACIÓN IMPORTANTE
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✅ Base de datos:
  • UsuarioId en Invitaciones: NULLABLE (permite invitaciones por correo)
  • Todas las migraciones incluidas en el publish
  • Sin requiere ejecutar migraciones manualmente

✅ Authenticación:
  • Cookies ASP.NET (8 horas de sesión)
  • Roles: Admin, Organizador, Participante
  • HTTPS recomendado pero no requerido

✅ Características:
  • Dashboard con eventos invitados
  • Envío de invitaciones por correo
  • Galería de imágenes
  • Sistema de RSVP (Confirmado/Rechazado/Pendiente)

⚠️ Vulnerabilidad Conocida:
  • MailKit 3.4.0 tiene CVE: GHSA-9j88-vvj5-vhgr
  • Actualizar a MailKit 4.0+ cuando sea posible

📞 CONTACTAR A SOPORTE SI:
  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  □ No tienes .NET 8 Runtime disponible
  □ No puedes conectar a la BD después de verificar todo
  □ El hosting no tiene SQL Server disponible
  □ Necesitas SSL/HTTPS activado
  □ Necesitas configuración de DNS

✅ ESTADO FINAL
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
El publish está listo y contiene:
✅ Todas las vistas Razor compiladas
✅ Componentes Blazor
✅ Migraciones de base de datos
✅ Assets estáticos (CSS, JS)
✅ Configuración de autenticación
✅ Soporte para MailKit
✅ Corrección de NULL en invitaciones

🎉 ¡Listo para publicar en hosting!
