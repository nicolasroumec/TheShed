# The Shed — Product

## Qué es
Gestor de contraseñas multi-usuario. Los usuarios guardan sus credenciales cifradas en vaults,
organizadas como quieran, accesibles desde cualquier dispositivo.

## Para quién
Cualquier persona que quiera guardar sus contraseñas de forma segura en la nube,
con la posibilidad de compartir vaults con otros usuarios (pareja, equipo, familia).

---

## Funcionalidades

### Autenticación
- Registro e inicio de sesión con email + contraseña maestra
- Cierre automático de sesión por inactividad (timeout configurable)
- 2FA (autenticación de dos factores)

### Vaults
- Crear, editar y eliminar vaults (carpetas agrupadas)
- Compartir un vault con otro usuario
- Roles en vault compartido: solo lectura / lectura+escritura

### Entradas
- Campos: nombre, usuario, contraseña, URL, notas
- Mostrar/ocultar contraseña
- Copiar contraseña al clipboard (sin exponerla en pantalla)
- Favoritos para acceso rápido
- Tags para categorizar entradas
- Historial de versiones (ver contraseñas anteriores de una entrada)
- Papelera: las entradas eliminadas se pueden recuperar antes de borrarse definitivamente

### Notas seguras
- Entradas de solo texto, sin usuario/contraseña
- Para guardar: números de serie, respuestas de seguridad, PINs, etc.

### Adjuntos
- Archivos pequeños adjuntos a una entrada (ej: PDF de licencia, imagen de tarjeta)

### Generador de contraseñas
- Longitud configurable
- Opciones: mayúsculas, minúsculas, números, símbolos
- Usable al crear/editar una entrada

### Seguridad
- Todas las contraseñas cifradas con AES-256
- Detector de contraseñas débiles
- Detector de contraseñas repetidas entre entradas

### Búsqueda
- Buscar entradas por nombre, URL o usuario

### Importar / Exportar
- Importar desde LastPass, Bitwarden, 1Password (formato CSV)
- Exportar todas las entradas propias

---

## Fuera de scope (por ahora)
- App móvil nativa
- Extensión de navegador
- Acceso offline
