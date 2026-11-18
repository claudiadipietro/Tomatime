# Tomatime

## Ejecutar con Docker

Esta configuración es para **uso local**, por ejemplo para probar el proyecto. Inicia el frontend, la API y una base de datos PostgreSQL independiente de cualquier base de datos de Aspire que ya exista.

### Requisitos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) instalado y abierto, con Docker Compose disponible.
- Conexión a Internet para descargar las imágenes y dependencias la primera vez.
- Una terminal situada en la raíz del proyecto, donde se encuentra `compose.yaml`.

No es necesario instalar Node.js, .NET ni PostgreSQL en el ordenador para ejecutar esta versión.

### Configuración inicial

1. Copia el archivo de ejemplo:

   ```bash
   cp .env.example .env
   ```

2. Se debe abrir `.env` con un editor de texto. Genera **dos valores diferentes** con el siguiente comando, ejecutándolo dos veces:

   ```bash
   openssl rand -hex 32
   ```

   Se debe copiar el primer resultado en `POSTGRES_PASSWORD` y el segundo en `JWT_KEY`, sin espacios ni comillas. Si no se tiene `openssl`, se pueden generar dos cadenas aleatorias hexadecimales seguras por otro medio.

3. Para que funcione la recuperación de contraseña, se debe rellenar también `GMAIL_ADDRESS` y `GMAIL_APP_PASSWORD` con la cuenta de Gmail de la persona que hará pruebas, y su contraseña de aplicación, sin espacios. Si quedan los valores vacíos, la aplicación arranca, pero **no enviará correos**. Los secretos de .NET usados fuera de Docker no se copian automáticamente a los contenedores.

4. Para habilitar notificaciones push, se debe rellenar `WEBPUSH_PUBLIC_KEY`, `WEBPUSH_PRIVATE_KEY` y `WEBPUSH_SUBJECT`. Se pueden generar un par con `npx web-push generate-vapid-keys` desde un entorno con Node.js; el asunto debe ser un contacto `mailto:` o una URL HTTPS. Se conserva el mismo par: cambiarlo requiere volver a registrar los dispositivos. Si los campos están vacíos, la aplicación arranca, pero no se podrán activar las notificaciones.

### Inicio y parada

En la raíz del proyecto:

```bash
docker compose up --build -d
```

Se debe abrir **http://localhost:8080** en el mismo ordenador. El primer arranque puede tardar varios minutos; la API aplica las migraciones a la base de datos de Docker al arrancar. Para ver el estado y los registros:

```bash
docker compose ps
docker compose logs -f api
```

Se debe pulsar `Ctrl+C` para dejar de ver los registros. Para detener la aplicación sin borrar los datos:

```bash
docker compose down
```

Para volver a iniciarla, se usa `docker compose up -d`. Si se cambia el código o la configuración, se debe ejecutar de nuevo `docker compose up --build -d`. Si el puerto 8080 está ocupado, se debe cambiar `APP_PORT` en `.env` y se debe abrir `http://localhost:PUERTO`; el enlace de recuperación se ajustará al puerto elegido.

PostgreSQL guarda los datos en un volumen Docker, que **no se elimina** con `docker compose down`. Se debe evitar `docker compose down -v`: también elimina ese volumen y los datos. Cambiar `POSTGRES_PASSWORD` en `.env` después de crear el volumen no actualiza la contraseña dentro de la base de datos; hay que cambiarla allí también.