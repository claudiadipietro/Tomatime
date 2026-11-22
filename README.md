# Tomatime

## Utilizar la aplicación desplegada

Se puede utilizar la aplicación desplegada en el [siguiente enlace](https://tomatime-5x2.pages.dev/).

Sin embargo, para compilar la aplicación en local, se deben seguir los siguientes pasos.

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

   Se debe copiar el primer resultado en `POSTGRES_PASSWORD` y el segundo en `JWT_KEY`, sin espacios ni comillas. Si no se tiene `openssl`, se pueden generar dos cadenas aleatorias hexadecimales seguras por otro medio (por ejemplo, [en el siguiente enlace](https://randomkeygen.com/openssl-rand-hex).

3. Opcional - Para que funcione la recuperación de contraseña, se debe rellenar también `GMAIL_ADDRESS` y `GMAIL_APP_PASSWORD` con una cuenta de Gmail y su contraseña de aplicación (no la contraseña del Gmail, sino la contraseña de aplicación, que se puede obtener en [el siguiente enlace](https://myaccount.google.com/u/1/apppasswords)). La contraseña de aplicación se debe poner sin espacios. Si quedan los valores vacíos, la aplicación arranca, pero **no enviará correos**.

4. Opcional - Para habilitar notificaciones push, se debe rellenar `WEBPUSH_PUBLIC_KEY`, `WEBPUSH_PRIVATE_KEY` y `WEBPUSH_SUBJECT`. Se pueden generar un par de llaves con `npx web-push generate-vapid-keys` desde un entorno con Node.js; el asunto debe ser un contacto `mailto:` o una URL HTTPS (por ejemplo, mailto:ejemplo@gmail.com). Si los campos están vacíos, la aplicación arranca, pero no se podrán activar las notificaciones.

### Inicio y parada

La primera vez que se inicie la aplicación, se debe ejecutar el siguiente comando en la raíz del proyecto:

```bash
docker compose up --build -d
```

Se debe abrir **http://localhost:8080** en el mismo ordenador. El primer arranque puede tardar varios minutos; la API aplica las migraciones a la base de datos de Docker al arrancar. Para ver el estado y los registros:

```bash
docker compose ps
docker compose logs -f api
```

Para iniciar la aplicación en modo test, con avance del tiempo en la sesión de concentración (sin tener que esperar a que transcurra el tiempo), se deben ejecutar los siguientes comandos en la raíz del proyecto:

```bash
docker compose build --build-arg BUILD_CONFIGURATION=production,development-tools web
docker compose up -d --no-build
```

En caso de querer iniciarla sin avance del tiempo, con la configuración de producción, se ejecutan los siguentes comandos:

```bash
docker compose build --build-arg BUILD_CONFIGURATION=production web
docker compose up -d --no-build
```

> **Nota:** Si se recompila el proyecto con otra configuración, se debe recargar la página con `Ctrl + F5`.

En la consola, se debe pulsar `Ctrl+C` para dejar de ver los registros. Para detener la aplicación sin borrar los datos:

```bash
docker compose down
```

Para volver a iniciarla, se usa `docker compose up -d`. Si el puerto 8080 está ocupado, se debe cambiar `APP_PORT` en `.env` y se debe abrir `http://localhost:PUERTO`.

PostgreSQL guarda los datos en un volumen Docker, que **no se elimina** con `docker compose down`. Se debe evitar `docker compose down -v`: también elimina ese volumen y los datos.
